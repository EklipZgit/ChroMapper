using System.Collections;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class WorldCavesParentBoundaryTest : TestBase
    {
        private bool previousAnimations;
        private float previousOffset;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PausedZeroShowsOpeningParentsWithoutChangingPlaybackTiming(
            [Values(UIModeType.Playing, UIModeType.Preview)] UIModeType previewMode,
            [Values(false, true)] bool playback)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var fixturePath = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "WorldCavesParentBoundaryFixture.json");
            yield return TestUtils.ReloadMap(2, JSON.Parse(File.ReadAllText(fixturePath)),
                beatsPerMinute: 124f, environmentName: "TimbalandEnvironment", songLengthSeconds: 20);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            var construction = Target("TrackConstruction1");
            var mirror = Target("TrackMirror1");
            var constructionStart = construction.position;
            var mirrorStart = mirror.position;
            mode.SetUIMode(previewMode, false);
            cameras.SelectCamera(CameraType.Playing);
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 0f);
            var samples = new JSONArray();
            var firstPlaybackBeat = clock.GetBeatFromSeconds(1f / TimeHelper.PreviewCallbackRate);
            foreach (var beat in new[] { 0f, 0.0005f, 0.001f, 0.01f, 0.0219f, 0.022f, firstPlaybackBeat, 6f, 0f, 20f, 0f })
            {
                if (playback)
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                else
                    clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var openingPose = !playback && beat == 0f;
                var expectedConstruction = constructionStart + (openingPose || beat >= 0.001f ? Vector3.forward * 802.2f : Vector3.zero);
                var expectedMirror = mirrorStart + (openingPose || beat >= 0.022f ? Vector3.forward * 802.2f : Vector3.zero);
                samples.Add(new JSONObject
                {
                    ["beat"] = beat,
                    ["constructionZ"] = construction.position.z,
                    ["mirrorZ"] = mirror.position.z,
                    ["playerZ"] = cameras.PlayerCameraController.transform.position.z
                });
                Assert.That(Vector3.Distance(construction.position, expectedConstruction), Is.LessThan(0.001f),
                    $"Beat {beat:R}, playback {playback}: paused zero previews the opening, otherwise the runway callback remains at 0.001.");
                Assert.That(Vector3.Distance(mirror.position, expectedMirror), Is.LessThan(0.001f),
                    $"Beat {beat:R}, playback {playback}: paused zero previews the opening, otherwise the mirror callback remains at 0.022.");
                Assert.That(cameras.PlayerCameraController.transform.position.z, Is.EqualTo(802.2f).Within(0.001f));
                Assert.That(clock.CurrentJsonTime, Is.EqualTo(beat).Within(0.000001f),
                    "The opening hierarchy preview must not advance the map's clock.");
            }
            if (playback)
                TestUtils.PauseDeterministicPlayback(clock);
            clock.MoveToJsonTime(0f);
            yield return null;
            Assert.That(construction.position.z, Is.EqualTo(constructionStart.z + 802.2f).Within(0.001f));
            Assert.That(mirror.position.z, Is.EqualTo(mirrorStart.z + 802.2f).Within(0.001f));
            mode.SetUIMode(UIModeType.Normal, false);
            yield return null;
            Assert.That(Vector3.Distance(construction.position, constructionStart), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(mirror.position, mirrorStart), Is.LessThan(0.001f));
            mode.SetUIMode(previewMode, false);
            yield return null;
            yield return null;
            Assert.That(construction.position.z, Is.EqualTo(constructionStart.z + 802.2f).Within(0.001f));
            Assert.That(mirror.position.z, Is.EqualTo(mirrorStart.z + 802.2f).Within(0.001f));
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "WorldCavesParentBoundary");
            Directory.CreateDirectory(directory);
            File.WriteAllText(PathUtils.Combine(directory, $"{previewMode}-playback-{playback}.json"), samples.ToString(2));
        }

        private static Transform Target(string trackName) => Object
            .FindObjectsByType<GeometryContainer>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(container => container.EnvironmentEnhancement?.Track == trackName)
            .SelectMany(container => container.GetComponents<ObjectAnimator>())
            .Select(animator => animator.LocalTarget)
            .Where(target => target != null).Single();

        [UnityTest]
        public IEnumerator PausedZeroEditsUpdateOpeningPoseAcrossUndoRedo(
            [Values(UIModeType.Playing, UIModeType.Preview)] UIModeType previewMode,
            [Values(false, true)] bool editAssignmentTime)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var path = PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "WorldCavesParentBoundaryFixture.json");
            var original = JSON.Parse(File.ReadAllText(path));
            var edited = original.Clone();
            var eventData = edited["_customData"]["_customEvents"].Children.Single(ev => editAssignmentTime
                ? ev["_type"].Value == "AssignTrackParent" && ev["_data"]["_parentTrack"].Value == "TrackConstructionParent1"
                : ev["_type"].Value == "AnimateTrack" && ev["_data"]["_track"].Value == "TrackConstructionParent1"
                    && !ev["_data"].HasKey("_duration"));
            if (editAssignmentTime)
                eventData["_time"] = 1f;
            else
            {
                eventData["_data"]["_position"] = JSON.Parse("[5,3,1400]");
                eventData["_data"]["_rotation"] = JSON.Parse("[0,30,15]");
                eventData["_data"]["_scale"] = JSON.Parse("[1.5,0.75,2]");
            }
            yield return LoadForEditing(edited, previewMode);
            var editedPose = CaptureRunwayPose();
            yield return LoadForEditing(original, previewMode);
            var originalPose = CaptureRunwayPose();
            Assert.That(Vector3.Distance(originalPose[0].GetColumn(3), editedPose[0].GetColumn(3)), Is.GreaterThan(1f),
                "The edited reference map must visibly move the runway before testing live updates.");
            clock.enabled = false;
            var source = BeatSaberSongContainer.Instance.Map.CustomEvents.Single(ev => editAssignmentTime
                ? ev.Type == "AssignTrackParent" && ev.DataParentTrack == "TrackConstructionParent1"
                : ev.Type == "AnimateTrack" && ev.CustomTrack?.Value == "TrackConstructionParent1" && ev.DataDuration == null);
            var replacement = new BaseCustomEvent(eventData.Clone());
            BeatmapActionContainer.AddAction(
                new BeatmapObjectUpdatedAction(replacement, source, "Edit paused-zero opening"), true);
            yield return null;
            yield return null;
            AssertRunwayPose(editedPose, "edit without seeking");
            var actions = Object.FindAnyObjectByType<BeatmapActionContainer>();
            actions.Undo();
            yield return null;
            yield return null;
            AssertRunwayPose(originalPose, "undo without seeking");
            actions.Redo();
            yield return null;
            yield return null;
            AssertRunwayPose(editedPose, "redo without seeking");
            Assert.That(clock.CurrentJsonTime, Is.EqualTo(0f));
            Assert.That(clock.IsPlaying, Is.False);
        }

        private IEnumerator LoadForEditing(JSONNode map, UIModeType previewMode)
        {
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            yield return TestUtils.ReloadMap(2, map.Clone(), beatsPerMinute: 124f,
                environmentName: "TimbalandEnvironment", songLengthSeconds: 20);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(previewMode, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;
        }

        private static Matrix4x4[] CaptureRunwayPose() => new[]
        {
            Target("TrackConstruction1").localToWorldMatrix,
            Target("TrackMirror1").localToWorldMatrix
        };

        private static void AssertRunwayPose(Matrix4x4[] expected, string operation)
        {
            var actual = CaptureRunwayPose();
            for (var target = 0; target < actual.Length; target++)
            {
                for (var row = 0; row < 4; row++)
                {
                    for (var column = 0; column < 4; column++)
                    {
                        Assert.That(actual[target][row, column], Is.EqualTo(expected[target][row, column]).Within(0.001f),
                            $"{operation}: runway {target} matrix [{row},{column}] must match a cold load of the edited map.");
                    }
                }
            }
        }

        [UnityTearDown]
        public IEnumerator Restore()
        {
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
