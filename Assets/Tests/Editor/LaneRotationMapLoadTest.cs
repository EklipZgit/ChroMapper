using System.Collections;
using System.IO;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class LaneRotationMapLoadTest : TestBase
    {
        private static readonly int[] NextMapRotations = { 0, 30 };
        private static readonly float[] PlaybackSeekBeats = { 12.5f, 26f };
        private bool previousAnimations;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator LoadingAnotherMapClearsPreviousRotationMotion(
            [ValueSource(nameof(NextMapRotations))] int nextRotation)
        {
            yield return LoadRotatingMap();
            var provider = Object.FindAnyObjectByType<LaneRotationProvider>();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.MoveToJsonTime(26f);
            yield return null;
            Assert.That(provider.PlaybackRotation, Is.EqualTo(-30f).Within(0.0001f));
            Assert.That(Mathf.Abs(provider.SmoothRotation), Is.GreaterThan(0.001f));

            var rotations = new JSONArray();
            if (nextRotation != 0)
                rotations.Add(new JSONObject { ["b"] = 0f, ["e"] = 0, ["r"] = nextRotation });

            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0", ["rotationEvents"] = rotations },
                environmentName: "DefaultEnvironment");
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            Assert.That(provider.PlaybackRotation, Is.EqualTo(nextRotation).Within(0.0001f));
            for (var frame = 0; frame < 4; frame++)
            {
                Assert.That(
                    Mathf.DeltaAngle(nextRotation, provider.SmoothRotation),
                    Is.EqualTo(0f).Within(0.0001f),
                    $"The new map inherited rotation smoothing on frame {frame}.");
                var camera = Object.FindAnyObjectByType<CameraManager>().PlayerCameraController.Camera;
                Assert.That(
                    Quaternion.Angle(camera.transform.rotation, Quaternion.Euler(0f, nextRotation, 0f)),
                    Is.EqualTo(0f).Within(0.01f),
                    $"The player camera retained the previous map's rotation on frame {frame}.");
                yield return null;
            }

            SetUpEditorMode();
            Assert.That(provider.PlaybackRotation, Is.EqualTo(nextRotation).Within(0.0001f));
            Assert.That(provider.SmoothRotation, Is.EqualTo(nextRotation).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator HarnessSetupClearsRotationMotionFromPreviousTest(
            [ValueSource(nameof(PlaybackSeekBeats))] float seekBeat)
        {
            yield return LoadRotatingMap();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var provider = Object.FindAnyObjectByType<LaneRotationProvider>();
            clock.MoveToJsonTime(seekBeat);
            yield return null;
            yield return null;
            Assert.That(Mathf.Abs(provider.SmoothRotation), Is.GreaterThan(0.001f));

            SetUpEditorMode();
            Assert.That(provider.PlaybackRotation, Is.EqualTo(0f).Within(0.0001f));
            for (var frame = 0; frame < 4; frame++)
            {
                Assert.That(
                    provider.SmoothRotation,
                    Is.EqualTo(0f).Within(0.0001f),
                    $"The test harness retained rotation motion on frame {frame}.");
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator StartingPlaybackDiscardsPreviousSmoothingVelocity(
            [ValueSource(nameof(PlaybackSeekBeats))] float seekBeat)
        {
            yield return LoadRotatingMap();
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var provider = Object.FindAnyObjectByType<LaneRotationProvider>();
            clock.MoveToJsonTime(seekBeat);
            yield return null;
            yield return null;
            var expectedRotation = seekBeat == 12.5f
                ? 30f
                : -30f;
            Assert.That(provider.PlaybackRotation, Is.EqualTo(expectedRotation).Within(0.0001f));
            Assert.That(
                Mathf.Abs(Mathf.DeltaAngle(provider.PlaybackRotation, provider.SmoothRotation)),
                Is.GreaterThan(0.001f),
                "The seek must still be smoothing before playback snaps it.");
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, seekBeat);
            for (var frame = 0; frame < 4; frame++)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, seekBeat);
                yield return null;
                Assert.That(
                    Mathf.DeltaAngle(provider.PlaybackRotation, provider.SmoothRotation),
                    Is.EqualTo(0f).Within(0.0001f),
                    $"The camera moved away from its snapped rotation on playback frame {frame}.");
            }
        }

        private static IEnumerator LoadRotatingMap()
        {
            Settings.Instance.Animations = true;
            var path = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "Issue628RotationFixture.json");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(path)),
                beatsPerMinute: 170f,
                environmentName: "GlassDesertEnvironment",
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }

        [UnityTearDown]
        public IEnumerator RestoreSharedMap()
        {
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (clock.IsPlaying)
                TestUtils.PauseDeterministicPlayback(clock);

            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(
                3,
                new JSONObject { ["version"] = "3.3.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
