using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class BimboLimboCloneParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousChroma;
        private bool previousClockEnabled;
        private float previousFov;
        private float previousOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the ignored local original Bimbo Limbo difficulty.")]
        public IEnumerator FullMapSquaresResolveAllAuthoredCloneIds()
        {
            yield return CheckMap(
                PathUtils.Combine(Application.dataPath, "..", "TestResults", "BimboLimbo", "FullMap.json"),
                "full",
                32);
        }

        [UnityTest]
        public IEnumerator ReducedSquareResolvesAuthoredCloneId()
        {
            yield return CheckMap(
                PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "BimboLimboCloneFixture.json"),
                "reduced",
                1);
        }

        [UnityTest]
        [Explicit("Requires the ignored local original Bimbo Limbo difficulty.")]
        public IEnumerator FullMapSquaresRemainSquareAfterReverseScrub()
        {
            yield return CheckMap(
                PathUtils.Combine(Application.dataPath, "..", "TestResults", "BimboLimbo", "FullMap.json"),
                "full-square-scrub",
                32,
                true);
        }

        [UnityTest]
        public IEnumerator ReducedSquareRestoresEdgesAfterScrubbingPastBeat267()
        {
            // The four edges gain their first rotation animation at 246.203, after earlier position animations.
            // Rewinding must recover their enhancement rotations while continuing to sample those positions.
            yield return CheckMap(
                PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "BimboLimboSquareScrubFixture.json"),
                "reduced-square-scrub",
                1,
                true);
        }

        private IEnumerator CheckMap(
            string path,
            string label,
            int squareCount,
            bool checkScrub = false)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 19f,
                NoteStartBeatOffset = -0.4f,
                CustomSuggestions = new List<string> { "Chroma" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 202f,
                environmentName: "BigMirrorEnvironment",
                songLengthSeconds: 300,
                forceSceneReload: !checkScrub,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            var beat = checkScrub
                ? 184.703f
                : 49.578f;
            yield return Seek(beat);
            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "..",
                "Scratch",
                "BimboLimboSquares",
                DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + label);
            Directory.CreateDirectory(directory);
            LinearRenderCapture.Capture(cameras.CameraControllers[1].Camera, PathUtils.Combine(directory, "initial.png"));
            var edges = CheckBindings(squareCount);
            if (!checkScrub)
                yield break;

            var initialPoses = edges.Select(edge => (
                Target: edge.transform,
                Position: edge.transform.localPosition,
                Rotation: edge.transform.localRotation,
                Scale: edge.transform.localScale)).ToArray();
            AssertSquareEdges(edges);
            foreach (var farBeat in new[] { 250f, 267f })
            {
                for (var scrubBeat = beat; scrubBeat < farBeat; scrubBeat += 4f)
                {
                    clock.MoveToJsonTime(scrubBeat);
                    yield return null;
                }

                yield return Seek(farBeat);
                Assert.That(
                    Quaternion.Angle(initialPoses[0].Target.localRotation, initialPoses[0].Rotation),
                    Is.GreaterThan(20f),
                    "The forward scrub must reach the later rotation animation before checking the return.");
                yield return Seek(beat);
                LinearRenderCapture.Capture(
                    cameras.CameraControllers[1].Camera,
                    PathUtils.Combine(directory, "returned-from-" + farBeat + ".png"));
                foreach (var pose in initialPoses)
                {
                    Assert.That(
                        Quaternion.Angle(pose.Target.localRotation, pose.Rotation),
                        Is.LessThan(0.001f),
                        pose.Target.name + " retained its future rotation after returning from " + farBeat);
                    Assert.That(Vector3.Distance(pose.Target.localPosition, pose.Position), Is.LessThan(0.001f));
                    Assert.That(Vector3.Distance(pose.Target.localScale, pose.Scale), Is.LessThan(0.001f));
                }

                AssertSquareEdges(edges);
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, beat);
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat + 0.2f);
                yield return null;
                yield return null;
                AssertSquareEdges(edges);
                TestUtils.PauseDeterministicPlayback(clock);
                yield return Seek(beat);
            }
        }

        private ParametricBloomFogLightController[] CheckBindings(int squareCount)
        {
            var manager = Object.FindAnyObjectByType<TracksManager>();
            var descriptor = Object.FindAnyObjectByType<EnvironmentDescriptor>();
            var effect = descriptor.BasicEventEffectManager.GetEffect<BasicLightEffect>(4);
            var boundRoots = new HashSet<Transform>();
            var allEdges = new List<ParametricBloomFogLightController>();
            for (var index = 0; index < squareCount; index++)
            {
                var track = manager.GetAnimationTrack("rg" + index);
                var targets = track.Children.Where(child => child.LocalTarget != null)
                    .Select(child => child.LocalTarget).Distinct().ToArray();
                Assert.That(targets.Length, Is.EqualTo(1), "rg" + index + " needs one cloned FrontLights target.");
                boundRoots.Add(targets[0]);
                var edges = targets[0].GetComponentsInChildren<ParametricBloomFogLightController>(true);
                Assert.That(edges.Length, Is.EqualTo(4), "Each square needs four independently addressed edges.");
                allEdges.AddRange(edges);
                var edgeIndices = edges.Select(edge => edge.ID).ToHashSet();
                var actualIds = effect.LightIdRemapEntries.Where(entry => edgeIndices.Contains((int)entry.y))
                    .Select(entry => (int)entry.x).OrderBy(id => id).ToArray();
                Assert.That(actualIds, Is.EqualTo(Enumerable.Range(100 + index * 10, 4).ToArray()));
            }

            foreach (var marker in descriptor.ChromaIDMarkers.Where(item => item.name == "FrontLights(Clone)"))
                Assert.That(boundRoots.Contains(marker.transform), Is.True, "Unconfigured FrontLights clone: " + marker.ChromaID);

            return allEdges.ToArray();
        }

        private static void AssertSquareEdges(ParametricBloomFogLightController[] edges)
        {
            foreach (var edge in edges)
            {
                var axis = edge.transform.localRotation * Vector3.up;
                Assert.That(
                    Mathf.Min(Mathf.Abs(axis.x), Mathf.Abs(axis.y)),
                    Is.LessThan(0.001f),
                    edge.name + " is diagonal inside its square: " + axis);
            }
        }

        private IEnumerator Seek(float beat)
        {
            clock.MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            if (clock != null)
                clock.enabled = previousClockEnabled;

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield break;
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreSharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" }, forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
