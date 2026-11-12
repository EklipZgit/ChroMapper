using System.Collections;
using System.Collections.Generic;
using System.IO;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class HeliovPlayerCameraTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousCameraOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator PreviewScrubsKeepTheCameraAtTheSameHeightRelativeToNotes() => CheckPlayerCamera(false);

        [UnityTest]
        public IEnumerator PlaybackKeepsTheCameraAtTheSameHeightRelativeToNotes() => CheckPlayerCamera(true);

        private IEnumerator CheckPlayerCamera(bool playback)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 14f,
                NoteStartBeatOffset = -0.5f,
                CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            var fullMapPath = System.Environment.GetEnvironmentVariable("CHROMAPPER_HELIOV_CAMERA_FULL_MAP_PATH");
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "HeliovPlayerCameraFixture.json");
            yield return TestUtils.ReloadMap(
                2,
                JSON.Parse(File.ReadAllText(string.IsNullOrEmpty(fullMapPath) ? fixturePath : fullMapPath)),
                beatsPerMinute: 150f,
                songLengthSeconds: 145,
                difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            var camera = cameras.PlayerCameraController.transform;
            var restingPosition = camera.position;
            mode.SetUIMode(playback ? UIModeType.Playing : UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            var tracks = Object.FindAnyObjectByType<TracksManager>();
            var noteParent = tracks.GetAnimationTrack("note").Track.ObjectParentTransform;
            clock.MoveToJsonTime(293f);
            yield return null;
            yield return null;
            var home = noteParent.InverseTransformPoint(camera.position);
            Assert.That(Vector3.Distance(home, new Vector3(0f, 1.65f, 0f)), Is.LessThan(0.01f));
            if (playback)
            {
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 293f);
            }
            var failures = new List<string>();
            var beats = playback
                ? new[] { 294f, 294.937f, 295f, 310f, 325.9f, 326f }
                : new[] { 294f, 294.937f, 295f, 310f, 325.9f, 326f, 295f, 295f - 1f / 64f, 295f + 1f / 64f, 293f, 326f };
            foreach (var beat in beats)
            {
                if (playback)
                {
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                }
                else
                {
                    clock.MoveToJsonTime(beat);
                }
                yield return null;
                yield return null;
                var actual = noteParent.InverseTransformPoint(camera.position);
                if (Vector3.Distance(actual, home) > 0.01f)
                {
                    failures.Add($"b{beat}: camera offset in the shared note frame was {actual:F4}, expected {home:F4}.");
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
            if (playback)
            {
                TestUtils.PauseDeterministicPlayback(clock);
            }
            mode.SetUIMode(UIModeType.Normal, false);
            cameras.SelectCamera(CameraType.Editing);
            Assert.That(
                Vector3.Distance(camera.position, restingPosition),
                Is.LessThan(0.001f),
                "Disconnecting the player track must restore the editor camera height.");
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(clock);
                }
                clock.enabled = previousClockEnabled;
            }
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
            }
            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
