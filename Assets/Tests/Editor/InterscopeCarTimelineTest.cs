using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class InterscopeCarTimelineTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private bool capturedClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            capturedClockEnabled = false;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ImportedCarsResolveEveryNativeBodyAndJoint()
        {
            yield return LoadInterscope();
            AssertCarBindings();
        }

        [UnityTest]
        public IEnumerator BounceAndHydraulicsAreStableAtThePausedSongTime()
        {
            yield return LoadInterscope();
            var bodies = AssertCarBindings();
            clock.MoveToJsonTime(4.125f);
            yield return null;
            yield return null;
            var positions = bodies.Select(body => body.transform.position).ToArray();
            var rotations = bodies.Select(body => body.transform.rotation).ToArray();
            for (var frame = 0; frame < 10; frame++)
                yield return new WaitForFixedUpdate();

            for (var index = 0; index < bodies.Length; index++)
            {
                Assert.That(Vector3.Distance(bodies[index].transform.position, positions[index]), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(bodies[index].transform.rotation, rotations[index]), Is.LessThan(0.01f));
            }

        }

        [UnityTest]
        public IEnumerator RewindingRestoresBounceAndHydraulicsWithoutChangingCarRoots()
        {
            yield return LoadInterscope();
            var bodies = AssertCarBindings();
            var roots = bodies.Select(body => body.transform.parent).Distinct().ToArray();
            var rootPositions = roots.Select(root => root.position).ToArray();
            clock.MoveToJsonTime(3.9f);
            yield return null;
            var beforeBounce = bodies.Select(body => body.transform.localPosition).ToArray();
            clock.MoveToJsonTime(4.125f);
            yield return null;
            var bounce = bodies.Select(body => body.transform.localPosition).ToArray();
            var rotations = bodies.Select(body => body.transform.localRotation).ToArray();
            Assert.That(bodies.Select((body, index) => Vector3.Distance(bounce[index], beforeBounce[index])).Max(),
                Is.GreaterThan(0.001f), "A frozen car must not satisfy the paused/scrub regression.");
            clock.MoveToJsonTime(8f);
            yield return null;
            clock.MoveToJsonTime(4.125f);
            yield return null;
            for (var index = 0; index < bodies.Length; index++)
            {
                Assert.That(Vector3.Distance(bodies[index].transform.localPosition, bounce[index]), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(bodies[index].transform.localRotation, rotations[index]), Is.LessThan(0.01f));
            }

            for (var index = 0; index < roots.Length; index++)
                Assert.That(Vector3.Distance(roots[index].position, rootPositions[index]), Is.LessThan(0.0001f));

        }

        [UnityTest]
        public IEnumerator HydraulicsFollowTheCapturedNativeMotion()
        {
            var fixture = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "InterscopeCarHydraulics.json")));
            yield return LoadInterscope(fixture);
            var body = AssertCarBindings().Single(item => item.name == "Body" && item.transform.parent.name == "Car1");
            var failures = new List<string>();
            foreach (var sample in fixture["nativeSamples"].Children)
            {
                var seconds = sample["seconds"].AsFloat;
                clock.MoveToJsonTime(seconds * 2f);
                yield return null;
                var position = sample["position"];
                var rotation = sample["rotation"];
                var expectedPosition = new Vector3(position[0].AsFloat, position[1].AsFloat, position[2].AsFloat);
                var expectedRotation = new Quaternion(
                    rotation[0].AsFloat,
                    rotation[1].AsFloat,
                    rotation[2].AsFloat,
                    rotation[3].AsFloat);
                var distance = Vector3.Distance(body.transform.position, expectedPosition);
                var angle = Quaternion.Angle(body.transform.rotation, expectedRotation);
                if (distance > 0.025f || angle > 2f)
                    failures.Add($"{seconds:F3}s: position error {distance:F5}m, rotation error {angle:F3} degrees");

            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PlaybackEditsAndMapReloadRebuildTheCarMotion()
        {
            yield return LoadInterscope();
            var context = Object.FindAnyObjectByType<BeatmapRuntimeContext>();
            var timeline = context.Descriptor.GetComponentInChildren<HydraulicCarTimeline>();
            var bodies = AssertCarBindings();
            clock.MoveToJsonTime(4.125f);
            var expected = bodies.Select(body => body.transform.localPosition).ToArray();
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 3.9f);
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 4.125f);
            for (var index = 0; index < bodies.Length; index++)
                Assert.That(Vector3.Distance(bodies[index].transform.localPosition, expected[index]), Is.LessThan(0.0001f));

            TestUtils.PauseDeterministicPlayback(clock);
            // Pausing snaps the editor cursor, so restore the sample before checking edits.
            clock.MoveToJsonTime(4.125f);
            var extraBounce = new BaseEvent { Type = 8, Value = 0, songBpmTime = 3.9f };
            timeline.InsertData(extraBounce);
            timeline.Refresh();
            Assert.That(bodies.Select((body, index) => Vector3.Distance(body.transform.localPosition, expected[index])).Max(),
                Is.GreaterThan(0.001f), "Changing an earlier event must invalidate cached motion.");
            timeline.RemoveData(extraBounce, extraBounce);
            timeline.Refresh();
            for (var index = 0; index < bodies.Length; index++)
                Assert.That(Vector3.Distance(bodies[index].transform.localPosition, expected[index]), Is.LessThan(0.0001f));

            yield return LoadInterscope();
            bodies = AssertCarBindings();
            clock.MoveToJsonTime(4.125f);
            for (var index = 0; index < bodies.Length; index++)
                Assert.That(Vector3.Distance(bodies[index].transform.localPosition, expected[index]), Is.LessThan(0.0001f));

        }

        [UnityTest]
        public IEnumerator BounceLaneHasItsNativeLabelAndValueKind()
        {
            yield return LoadInterscope();
            var definitions = Object.FindAnyObjectByType<BeatmapRuntimeContext>().TrackDefinitions;
            foreach (var (type, name) in new[]
                     {
                         (6, "Left Lasers"), (7, "Right Lasers"), (8, "Car Bounce"),
                         (9, "Laser Spread"), (16, "Hydraulics Down"), (17, "Hydraulics Up")
                     })
            {
                Assert.That(definitions.GetBasicOrDefault(type).Name, Is.EqualTo(name));
            }

            foreach (var type in new[] { 8, 16, 17 })
                Assert.That(definitions.GetBasicOrDefault(type).Kind, Is.EqualTo(BasicEventKind.IntValue));
        }

        private IEnumerator LoadInterscope(JSONNode overrideFixture = null)
        {
            Settings.Instance.Animations = true;
            var fixture = JSON.Parse(@"{
                ""version"":""3.3.0"",
                ""basicBeatmapEvents"":[
                    {""b"":4,""et"":8,""i"":1,""f"":1},
                    {""b"":6,""et"":16,""i"":1,""f"":1},
                    {""b"":7,""et"":17,""i"":1,""f"":1}
                ]
            }");
            yield return TestUtils.ReloadMap(
                3,
                overrideFixture ?? fixture,
                beatsPerMinute: 120f,
                environmentName: "InterscopeEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (!capturedClockEnabled)
            {
                previousClockEnabled = clock.enabled;
                capturedClockEnabled = true;
            }

            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(0f);
            yield return null;
        }

        private static Rigidbody[] AssertCarBindings()
        {
            var descriptor = Object.FindAnyObjectByType<BeatmapRuntimeContext>().Descriptor;
            var jumps = descriptor.GetComponentsInChildren<HydraulicCarJump>(true);
            var suspension = descriptor.GetComponentsInChildren<HydraulicCarSuspension>(true);
            Assert.That(jumps.Length, Is.EqualTo(8));
            Assert.That(suspension.Length, Is.EqualTo(8));
            foreach (var jump in jumps)
                Assert.That(jump.Rigidbody, Is.Not.Null, jump.name + " has no native jump rigidbody.");

            foreach (var car in suspension)
            {
                Assert.That(car.Rigidbody, Is.Not.Null, car.name + " has no native suspension rigidbody.");
                Assert.That(car.SpringJoint, Is.Not.Null);
            }

            var joints = jumps.SelectMany(jump => jump.Rigidbody.GetComponents<SpringJoint>()).ToArray();
            Assert.That(joints.Length, Is.EqualTo(16));
            foreach (var joint in joints)
                Assert.That(joint.connectedBody, Is.Not.Null, joint.name + " is incorrectly attached to the world origin.");

            return jumps.Select(jump => jump.Rigidbody).Concat(joints.Select(joint => joint.connectedBody)).Distinct().ToArray();
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
            }

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
