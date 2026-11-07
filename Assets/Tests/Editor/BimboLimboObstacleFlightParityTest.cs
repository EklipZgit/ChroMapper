using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class BimboLimboObstacleFlightParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private bool previousChroma;
        private float previousFov;
        private float previousCameraOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousChroma = Settings.Instance.EmulateChromaLite;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator ObstacleMatchesNativeTravelAndSimultaneousNote()
        {
            yield return LoadFixture(false);
            foreach (var beat in new[] { 20.328f, 20.312f, 20.344f, 20.328f, 20.6f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                CheckAlignment();
            }
        }

        [UnityTest]
        public IEnumerator NativeApproachRemainsVisibleAfterModeSwitch()
        {
            yield return LoadFixture(false);
            clock.MoveToJsonTime(20.328f);
            yield return null;
            yield return null;
            mode.SetUIMode(UIModeType.Normal, false);
            cameras.SelectCamera(CameraType.Editing);
            yield return null;
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            yield return null;
            yield return null;
            CheckAlignment();
        }

        [UnityTest]
        public IEnumerator ObstacleMatchesNativeTravelDuringPlayback()
        {
            yield return LoadFixture(false);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock,
                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(20.2f));
            foreach (var beat in new[] { 20.2f, 20.328f, 20.344f, 20.6f })
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock,
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(beat));
                yield return null;
                yield return null;
                CheckAlignment();
            }
        }

        [UnityTest]
        [Explicit("Requires the local original Bimbo Limbo difficulty.")]
        public IEnumerator OriginalMapObstacleMatchesNativeTravel()
        {
            yield return LoadFixture(true);
            clock.MoveToJsonTime(20.328f);
            yield return null;
            yield return null;
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "BimboLimboObstacleFlight",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(directory);
            LinearRenderCapture.Capture(cameras.PlayerCameraController.Camera, PathUtils.Combine(directory, "actual.png"));
            Debug.Log("Bimbo obstacle capture: " + directory);
            CheckAlignment();
        }

        private IEnumerator LoadFixture(bool fullMap)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.EmulateChromaLite = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var fixturePath = fullMap
                ? PathUtils.Combine(Application.dataPath, "..", "TestResults", "BimboLimboObstacleFlight", "OriginalMap.json")
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "BimboLimboObstacleFlightFixture.json");
            yield return TestUtils.ReloadMap(2, JSON.Parse(File.ReadAllText(fixturePath)), beatsPerMinute: 202f,
                environmentName: fullMap ? "BigMirrorEnvironment" : null,
                songLengthSeconds: fullMap ? 300 : 12,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 19f,
                    NoteStartBeatOffset = -0.4f,
                    CustomSuggestions = new List<string> { "Chroma" },
                    LightshowFileName = "MissingTestLightshow.dat"
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        private void CheckAlignment()
        {
            var grid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            // V2 import can normalize JSON beats. SongBpmTime identifies the original file's b21.203 objects.
            var wallData = grid.MapObjects.Single(value => Mathf.Abs(value.SongBpmTime - 21.203f) < 0.001f);
            Assert.That(grid.LoadedContainers.TryGetValue(wallData, out var container), Is.True,
                "The original b21.203 wall must already be loaded during its native half-second approach.");
            var wall = (ObstacleContainer)container;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single(value => Mathf.Abs(value.NoteData.SongBpmTime - 21.203f) < 0.001f
                    && value.NoteData.Color == 0);
            var wallZ = wall.AssignedTrack.ObjectParentTransform.position.z;
            var noteZ = note.DirectionTarget.position.z;
            var noteSeconds = clock.GetSecondsFromBeat(note.NoteData.SongBpmTime);
            var halfSeconds = clock.GetSecondsFromBeat(note.NoteData.HalfJumpDuration);
            var jumpStart = noteSeconds - halfSeconds;
            var expectedZ = (clock.CurrentSeconds < jumpStart
                ? 19f * halfSeconds + 100f * (jumpStart - clock.CurrentSeconds) / 0.5f
                : (noteSeconds - clock.CurrentSeconds) * 19f) + BeatmapConstant.ZOffset;
            Assert.That(noteZ, Is.EqualTo(expectedZ).Within(0.001f));
            Assert.That(wallZ, Is.EqualTo(expectedZ).Within(0.001f),
                "A native wall and note at b21.203 must align during their incoming travel.");
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (clock != null && clock.IsPlaying)
            {
                TestUtils.PauseDeterministicPlayback(clock);
            }
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.EmulateChromaLite = previousChroma;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
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
