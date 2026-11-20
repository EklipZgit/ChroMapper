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
    // The Sledgehammer LAGGY WALLS report: walls flying past the player must keep traveling and pick up
    // native ObstacleController's cubic end-distance throw (z -= 500 * t^3) starting 0.15 seconds after
    // the trailing edge clears the player, completing at the native despawn. A linear jump-out at a
    // fixed 2-beat slope makes the walls suddenly accelerate instead of easing away.
    public class ObstacleDespawnThrowParityTest : TestBase
    {
        private const float Bpm = 200f;
        private const float HalfJumpBeats = 2f;
        private const float HalfJumpDistance = 10.8f;
        private const float TravelPerBeat = HalfJumpDistance / HalfJumpBeats;
        private const float AvoidMarkBeats = Bpm / 60f * 0.15f;
        private const float ThrowDistance = 500f;

        private bool previousAnimations;
        private float previousFov;
        private float previousCameraOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousCameraOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator RotatedSlabsAccelerateAwayCubicallyOnceTailClearsPlayer()
        {
            yield return LoadFixture();
            foreach (var beat in new[] { 1.9f, 2.0f, 2.3f, 2.6f, 2.9f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                CheckWallZ(1.0f, 0.062f, 180, beat);
                CheckWallZ(1.4375f, 0.062f, 180, beat);
            }
        }

        [UnityTest]
        public IEnumerator ForwardAndRotatedWallsShareTheSameThrowInTrackSpace()
        {
            yield return LoadFixture();
            foreach (var beat in new[] { 4.8f, 5.3f, 5.9f, 6.8f, 7.3f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                CheckWallZ(3.0f, 1.0f, 0, beat);
                CheckWallZ(5.0f, 1.0f, 180, beat);
            }
        }

        private IEnumerator LoadFixture()
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "ObstacleDespawnThrowFixture.json");
            yield return TestUtils.ReloadMap(4, JSON.Parse(File.ReadAllText(fixturePath)), beatsPerMinute: Bpm,
                songLengthSeconds: 20,
                difficultyInfo: new InfoDifficulty(
                    new InfoDifficultySet { Characteristic = "360Degree" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 18f,
                    NoteStartBeatOffset = 0f,
                    LightshowFileName = "MissingTestLightshow.dat"
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        private void CheckWallZ(float wallBeat, float duration, int rotation, float beat)
        {
            var grid = Object.FindAnyObjectByType<ObstacleGridContainer>();
            var wallData = grid.MapObjects.Single(value => Mathf.Abs(value.JsonTime - wallBeat) < 0.0001f
                && value.Rotation == rotation);
            // Beyond the native despawn (end + halfJumpDuration) the container is already recycled.
            if (beat >= wallBeat + duration + HalfJumpBeats)
            {
                return;
            }

            Assert.That(
                grid.LoadedContainers.TryGetValue(wallData, out var container),
                Is.True,
                $"Wall b{wallBeat} must be loaded at beat {beat}.");
            var z = ((ObstacleContainer)container).AssignedTrack.ObjectParentTransform.localPosition.z;
            Assert.That(z, Is.EqualTo(ExpectedWallZ(wallBeat, duration, beat)).Within(0.01f),
                $"Wall b{wallBeat} track z at beat {beat}.");
        }

        // Native ObstacleController.GetPosForTime in track space: constant-speed travel plus the cubic
        // 500m throw that starts at end + 0.15s and runs for halfJumpDuration - 0.15s.
        private static float ExpectedWallZ(float wallBeat, float duration, float beat)
        {
            var end = wallBeat + duration;
            var travel = HalfJumpDistance + BeatmapConstant.ZOffset
                - TravelPerBeat * (beat - (wallBeat - HalfJumpBeats));
            var throwT = Mathf.Max(0f, (beat - end - AvoidMarkBeats) / (HalfJumpBeats - AvoidMarkBeats));
            return travel - ThrowDistance * throwT * throwT * throwT;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditor()
        {
            if (mode != null)
            {
                mode.SetUIMode(UIModeType.Normal, false);
                cameras.SelectCamera(CameraType.Editing);
            }
            Settings.Instance.Animations = previousAnimations;
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
