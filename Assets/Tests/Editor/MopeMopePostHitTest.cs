using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class MopeMopePostHitTest : TestBase
    {
        private const float TargetBeat = 266f + 51f / 64f;
        private static readonly int animationSpawnedId = Shader.PropertyToID("_AnimationSpawned");
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousCameraOffset;
        private int previousGridSnapping;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private NoteGridContainer grid;
        private readonly Dictionary<BaseNote, Vector3> playingPositions = new();
        private readonly Dictionary<BaseNote, Quaternion> stoppedRotations = new();

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
        public IEnumerator StoppedSeekCanFinishFakeBeforeApplyingItsPose()
        {
            yield return LoadFixture(false, "MopeMopeSeekRecycleFixture.json");
            clock.MoveToJsonTime(8f);
            yield return null;
            yield return null;
            Assert.That(grid.LoadedContainers.Count, Is.EqualTo(1));
            clock.MoveToJsonTime(12f);
            yield return null;
            yield return null;
            Assert.That(grid.LoadedContainers, Is.Empty,
                "Track time one must recycle the fake without applying a pose to the recycled container.");
        }

        [UnityTest]
        public IEnumerator StaggeredRowLoadingKeepsNativeCutDirectionAngles()
        {
            yield return LoadAndPlay(noteRow: true);
            CheckRowAngles("playback");
            TestUtils.PauseDeterministicPlayback(clock);
            yield return null;
            CheckRowAngles("pause");
            grid.RefreshPool(true);
            yield return null;
            CheckRowAngles("forced refresh");
            clock.MoveToJsonTime(400f);
            yield return null;
            clock.MoveToJsonTime(TargetBeat);
            yield return null;
            CheckRowAngles("backward seek");
        }

        private void CheckRowAngles(string phase)
        {
            Assert.That(grid.LoadedContainers.Count, Is.EqualTo(3), phase);
            foreach (var data in grid.MapObjects)
            {
                var note = (NoteContainer)grid.LoadedContainers[data];
                Assert.That(Quaternion.Angle(Quaternion.Euler(note.DirectionTargetEuler), Quaternion.identity),
                    Is.LessThan(0.01f), $"{phase}: b272 x{data.PosX} must use the native Base layer in its time row.");
            }
        }

        [UnityTest]
        public IEnumerator ViewingDistanceDoesNotChangeNativeNoteFacing()
        {
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            yield return LoadAndPlay();
            var data = grid.MapObjects.Single(note => note.JsonTime == 268f);
            var cameraHome = cameras.PlayerCameraController.transform.position;
            var rotation = ((NoteContainer)grid.LoadedContainers[data]).DirectionTarget.rotation;
            foreach (var offset in new[] { 3.6f, 6f, 0f })
            {
                Settings.Instance.PlayerCameraOffsetZ = offset;
                Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.PlayerCameraOffsetZ), offset);
                yield return null;
                yield return null;
                var note = (NoteContainer)grid.LoadedContainers[data];
                Assert.That(Vector3.Distance(cameras.PlayerCameraController.transform.position, cameraHome),
                    Is.EqualTo(offset).Within(0.001f), "The viewing camera must actually move in this regression.");
                note.Animator.ApplyAnimation();
                Assert.That(Quaternion.Angle(note.DirectionTarget.rotation, rotation), Is.LessThan(0.05f),
                    "Noodle faces the player head. Moving CM's viewing camera backward must not move that head.");
            }
        }

        [UnityTest]
        public IEnumerator PostHitFakesSurvivePauseModeSwitchAndBidirectionalSeeks()
        {
            yield return LoadAndPlay();
            CheckPresent("playback");
            foreach (var data in grid.MapObjects)
            {
                var note = (NoteContainer)grid.LoadedContainers[data];
                playingPositions.Add(data, note.DirectionTarget.position);
            }

            TestUtils.PauseDeterministicPlayback(clock);
            yield return null;
            yield return null;
            CheckPresent("pause");
            cameras.SelectCamera(CameraType.Editing);
            mode.SetUIMode(UIModeType.Normal, false);
            yield return null;
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Playing, false);
            yield return null;
            yield return null;
            CheckPresent("Normal to Playing");
            foreach (var from in new[] { TargetBeat - 1f / 64f, TargetBeat + 1f / 64f, 0f, 400f })
            {
                clock.MoveToJsonTime(from);
                yield return null;
                clock.MoveToJsonTime(TargetBeat);
                yield return null;
                yield return null;
                CheckPresent($"seek from {from}");
            }

            grid.RefreshPool(true);
            yield return null;
            yield return null;
            CheckPresent("forced pool refresh");
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, TargetBeat);
            yield return null;
            yield return null;
            CheckPresent("resume");
            TestUtils.PauseDeterministicPlayback(clock);
            clock.MoveToJsonTime(280f);
            yield return null;
            yield return null;
            Assert.That(grid.LoadedContainers, Is.Empty, "Expired fake notes must be recycled.");
        }

        [UnityTest]
        [Explicit("Requires the original map at TestResults/MopeMope/FullMap.json.")]
        public IEnumerator FullMapKeepsTheSameObjectsAndPosesAcrossPreviewRoutes()
        {
            yield return LoadAndPlay(true);
            var poses = GetActiveFakePoses();

            var directory = PathUtils.Combine(
                Application.dataPath,
                "..",
                "TestResults",
                "MopeMope",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(directory);
            LinearRenderCapture.Capture(cameras.PlayerCameraController.Camera, PathUtils.Combine(directory, "playing.png"));
            TestUtils.PauseDeterministicPlayback(clock);
            yield return null;
            yield return null;
            CheckFullMapPoses("pause", poses);
            LinearRenderCapture.Capture(cameras.PlayerCameraController.Camera, PathUtils.Combine(directory, "paused.png"));
            cameras.SelectCamera(CameraType.Editing);
            mode.SetUIMode(UIModeType.Normal, false);
            yield return null;
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Playing, false);
            yield return null;
            yield return null;
            CheckFullMapPoses("Normal to Playing", poses);
            LinearRenderCapture.Capture(cameras.PlayerCameraController.Camera, PathUtils.Combine(directory, "mode-switch.png"));
            foreach (var from in new[] { TargetBeat - 1f / 64f, TargetBeat + 1f / 64f, 0f, 400f })
            {
                clock.MoveToJsonTime(from);
                yield return null;
                clock.MoveToJsonTime(TargetBeat);
                yield return null;
                yield return null;
                CheckFullMapPoses($"seek from {from}", poses);
            }

            LinearRenderCapture.Capture(cameras.PlayerCameraController.Camera, PathUtils.Combine(directory, "seek.png"));
            Debug.Log("Mope Mope post-hit captures: " + directory);
        }

        private void CheckFullMapPoses(
            string phase,
            Dictionary<BaseNote, (Vector3 Position, Quaternion Rotation)> poses)
        {
            var actual = GetActiveFakePoses();
            var missing = string.Join(
                ", ",
                poses.Keys.Where(note => !actual.ContainsKey(note))
                    .Select(note => $"b{note.JsonTime} ({note.PosX},{note.PosY}) {note.CustomTrack}"));
            Assert.That(actual.Count, Is.EqualTo(poses.Count),
                $"{phase} at b{clock.CurrentJsonTime}: active fake note count. Missing: {missing}");
            foreach (var pair in poses)
            {
                Assert.That(actual.TryGetValue(pair.Key, out var pose), Is.True,
                    $"{phase}: missing b{pair.Key.JsonTime} at ({pair.Key.PosX},{pair.Key.PosY})");
                Assert.That(Vector3.Distance(pose.Position, pair.Value.Position), Is.LessThan(0.001f),
                    $"{phase}: b{pair.Key.JsonTime} world position");
                CheckStoppedRotation(pair.Key, pose.Rotation, phase);
            }
        }

        private Dictionary<BaseNote, (Vector3 Position, Quaternion Rotation)> GetActiveFakePoses()
        {
            var poses = new Dictionary<BaseNote, (Vector3 Position, Quaternion Rotation)>();
            foreach (var pair in grid.LoadedContainers)
            {
                var data = (BaseNote)pair.Key;
                var note = (NoteContainer)pair.Value;
                note.Animator.ApplyAnimation();
                if (!data.CustomFake || data.SpawnSongBpmTime > TargetBeat
                    || note.MpbController.Mpb.GetFloat(animationSpawnedId) < 0f)
                    continue;

                poses.Add(data, (note.DirectionTarget.position, note.DirectionTarget.rotation));
            }

            return poses;
        }

        [UnityTest]
        public IEnumerator PostHitLookKeepsTheLocalRotationReachedAtTheHit()
        {
            yield return LoadFixture(false, "MopeMopePostHitFixture.json");
            var data = grid.MapObjects.Single(note => note.JsonTime == 264.75f);
            var lastFrame = TimeHelper.GetPreviousPreviewSeconds(clock.GetSecondsFromBeat(data.SongBpmTime));
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock,
                clock.GetBeatFromSeconds(lastFrame - 1f / TimeHelper.PreviewCallbackRate));
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(lastFrame));
            yield return null;
            yield return null;
            var hitNote = (NoteContainer)grid.LoadedContainers[data];
            hitNote.Animator.ApplyAnimation();
            var hitRotation = hitNote.DirectionTarget.localRotation;
            // A multi-beat clock jump is a seek, not the previous rendered frame consumed by NoteJump.
            var endSeconds = clock.GetSecondsFromBeat(TargetBeat);
            for (var frame = 1; lastFrame + (frame - 1) / TimeHelper.PreviewCallbackRate < endSeconds; frame++)
            {
                var seconds = Mathf.Min(endSeconds, lastFrame + frame / TimeHelper.PreviewCallbackRate);
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(seconds));
                yield return null;
            }
            var note = (NoteContainer)grid.LoadedContainers[data];
            note.Animator.ApplyAnimation();
            Assert.That(Quaternion.Angle(note.DirectionTarget.localRotation, hitRotation), Is.LessThan(0.1f),
                "Native NoteJump stops updating the rotated object at half jump, while the path's world rotation continues.");
        }

        [UnityTest]
        public IEnumerator PostHitJumpMatchesNativeParabolaAndFinalQuarterTravel()
        {
            yield return LoadAndPlay();
            foreach (var data in grid.MapObjects)
            {
                var note = (NoteContainer)grid.LoadedContainers[data];
                var elapsed = (TargetBeat - data.SpawnSongBpmTime) / data.HalfJumpDuration;
                // NoteJump.ManualUpdate integrates gravity for the entire jump, including after the hit.
                var expectedY = note.JumpStartHeight
                    + (note.JumpApexHeight - note.JumpStartHeight) * (2f * elapsed - elapsed * elapsed);
                var progress = elapsed * 0.5f;
                var quarter = Mathf.Max(0f, (progress - 0.75f) / 0.25f);
                var expectedZ = data.HalfJumpDistance * (1f - 2f * progress)
                    + BeatmapConstant.NoteZOffset - 500f * quarter * quarter * quarter;
                var actual = note.Animator.AnimationTrack.ObjectParentTransform.localPosition;
                Assert.That(actual.y, Is.EqualTo(expectedY).Within(0.0001f), $"b{data.JsonTime}: descending jump height");
                Assert.That(actual.z, Is.EqualTo(expectedZ).Within(0.001f), $"b{data.JsonTime}: native final-quarter travel");
            }
        }

        private void CheckPresent(string phase)
        {
            foreach (var data in grid.MapObjects)
            {
                Assert.That(grid.LoadedContainers.ContainsKey(data), Is.True,
                    $"{phase}: fake b{data.JsonTime} is still inside its full jump lifetime at b{TargetBeat}.");
                var container = (NoteContainer)grid.LoadedContainers[data];
                container.Animator.ApplyAnimation();
                Assert.That(container.gameObject.activeInHierarchy, Is.True, phase);
                Assert.That(container.MpbController.Mpb.GetFloat(cutoutId), Is.EqualTo(0.15f).Within(0.001f), phase);
                Assert.That(container.ArrowMpbController.Mpb.GetFloat(cutoutId), Is.EqualTo(0.4f).Within(0.001f), phase);
                if (playingPositions.TryGetValue(data, out var position))
                {
                    var note = (NoteContainer)grid.LoadedContainers[data];
                    Assert.That(Vector3.Distance(note.DirectionTarget.position, position), Is.LessThan(0.001f),
                        $"{phase}: b{data.JsonTime} world position");
                    CheckStoppedRotation(data, note.DirectionTarget.rotation, phase);
                }
            }
        }

        private void CheckStoppedRotation(BaseNote data, Quaternion rotation, string phase)
        {
            if (clock.IsPlaying)
                return;

            // Playback uses its preceding rendered frame. Compare stopped routes with the same stopped sample.
            if (stoppedRotations.TryGetValue(data, out var stoppedRotation))
                Assert.That(Quaternion.Angle(rotation, stoppedRotation), Is.LessThan(0.05f),
                    $"{phase}: b{data.JsonTime} stopped world rotation");
            else
                stoppedRotations.Add(data, rotation);
        }

        private IEnumerator LoadAndPlay(bool fullMap = false, bool noteRow = false)
        {
            yield return LoadFixture(fullMap, noteRow ? "MopeMopeNoteRowFixture.json" : "MopeMopePostHitFixture.json");
            var start = fullMap ? 200f : 256f;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, start);
            for (var beat = start; beat < TargetBeat; beat += 0.25f)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                yield return null;
            }

            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, TargetBeat);
            yield return null;
            yield return null;
            foreach (var container in grid.LoadedContainers.Values)
                container.Animator.ApplyAnimation();

            Assert.That(grid.MapObjects.Count, Is.EqualTo(fullMap ? 5060 : 3));
        }

        private IEnumerator LoadFixture(bool fullMap, string fixtureName)
        {
            playingPositions.Clear();
            stoppedRotations.Clear();
            Settings.Instance.Animations = true;
            var path = fullMap
                ? PathUtils.Combine(Application.dataPath, "..", "TestResults", "MopeMope", "FullMap.json")
                : PathUtils.Combine(
                    Application.dataPath,
                    "Tests",
                    "Fixtures",
                    fixtureName);
            var map = JSON.Parse(File.ReadAllText(path));
            yield return TestUtils.ReloadMap(
                2,
                map,
                beatsPerMinute: 200f,
                songLengthSeconds: 150,
                environmentName: "TriangleEnvironment",
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 16f,
                    NoteStartBeatOffset = 2f,
                    CustomRequirements = new List<string> { "Chroma", "Noodle Extensions" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            previousGridSnapping = clock.GridMeasureSnapping;
            clock.GridMeasureSnapping = 64;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            grid = Object.FindAnyObjectByType<NoteGridContainer>();
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Playing, false);
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (clock != null)
            {
                if (clock.IsPlaying)
                    TestUtils.PauseDeterministicPlayback(clock);

                clock.enabled = previousClockEnabled;
                clock.GridMeasureSnapping = previousGridSnapping;
            }

            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.PlayerCameraOffsetZ), previousCameraOffset);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
