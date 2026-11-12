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
    public class MegamixNoteJumpParityTest : TestBase
    {
        private bool previousAnimations;
        private bool previousClockEnabled;
        private float previousFov;
        private float previousOffset;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousFov = Settings.Instance.PlayerCameraFOV;
            previousOffset = Settings.Instance.PlayerCameraOffsetZ;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator JumpRotationMatchesNativeStagesAcrossSeeks()
        {
            yield return LoadFixture();
            var failures = new List<string>();
            var directory = PathUtils.Combine(Application.dataPath, "..", "TestResults", "MegamixNoteJump",
                System.DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff"));
            Directory.CreateDirectory(directory);
            foreach (var beat in new[] { 414.375f, 414.2f, 414.5f, 420f, 414.375f })
            {
                clock.MoveToJsonTime(beat);
                if (beat == 420f)
                {
                    yield return null;
                    continue;
                }
                yield return CheckRenderedRotations(failures, beat,
                    clock.CurrentSeconds - 1f / TimeHelper.PreviewCallbackRate,
                    beat == 414.375f ? PathUtils.Combine(directory, "actual.png") : null);
            }
            File.WriteAllText(PathUtils.Combine(directory, "errors.txt"), string.Join("\n", failures));
            Debug.Log("Megamix note jump capture: " + directory);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator JumpRotationMatchesNativeStagesDuringPlayback([Values(60, 90, 120)] int frameRate)
        {
            yield return LoadFixture();
            var map = BeatSaberSongContainer.Instance.Map;
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock,
                (float)map.JsonTimeToSongBpmTime(413f));
            var seconds = clock.CurrentSeconds;
            var failures = new List<string>();
            foreach (var beat in new[] { 414.2f, 414.375f, 414.5f })
            {
                var targetSeconds = clock.GetSecondsFromBeat((float)map.JsonTimeToSongBpmTime(beat));
                // Noodle's facing uses the preceding position, so playback must advance one frame at a time.
                while (seconds < targetSeconds)
                {
                    var previousSeconds = clock.CurrentSeconds;
                    seconds = Mathf.Min(seconds + 1f / frameRate, targetSeconds);
                    TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(seconds));
                    if (seconds == targetSeconds)
                        yield return CheckRenderedRotations(failures, beat, previousSeconds);
                    else
                        yield return null;
                }
            }
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        [Explicit("Requires the local original Megamix Lawless difficulty.")]
        public IEnumerator FullMapJumpRotationMatchesNativeStages()
        {
            yield return LoadFixture(true);
            clock.MoveToJsonTime(414.375f);
            var failures = new List<string>();
            var path = PathUtils.Combine(Application.dataPath, "..", "TestResults", "MegamixNoteJump", "full-map-after.png");
            yield return CheckRenderedRotations(failures, 414.375f,
                clock.CurrentSeconds - 1f / TimeHelper.PreviewCallbackRate, path);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator LoadFixture(bool fullMap = false)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            var difficulty = new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
            {
                Difficulty = "ExpertPlus",
                NoteJumpSpeed = 18f,
                NoteStartBeatOffset = -0.75f,
                CustomRequirements = fullMap
                    ? new List<string> { "Chroma", "Noodle Extensions" }
                    : new List<string> { "Noodle Extensions" },
                LightshowFileName = "MissingTestLightshow.dat"
            };
            var path = fullMap
                ? PathUtils.Combine(Application.dataPath, "..", "TestResults", "Megamix", "FullMap.json")
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "MegamixNoteJumpFixture.json");
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(path)), beatsPerMinute: 160f,
                environmentName: fullMap ? "BigMirrorEnvironment" : null,
                songLengthSeconds: fullMap ? 500 : 160, difficultyInfo: difficulty);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
        }

        [UnityTest]
        public IEnumerator StackEditsRefreshTheCachedMiddleRotation()
        {
            yield return LoadFixture();
            clock.MoveToJsonTime(414.375f);
            yield return null;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var note = grid.LoadedContainers.Values.OfType<NoteContainer>()
                .Single(item => item.NoteData.JsonTime == 415f && item.NoteData.PosX == 1);
            var lower = new BaseNote(note.NoteData) { PosY = 0 };
            var initial = new Vector3(-0.6780157f, 0.306681f, -0.6680131f) * 20f;
            var stacked = new Vector3(0.365105f, -0.3664974f, -0.8557909f) * 20f;
            Assert.That(Vector3.Distance(note.JumpRotationOffset, initial), Is.LessThan(0.0001f));
            grid.SpawnObject(lower);
            try
            {
                Assert.That(Vector3.Distance(note.JumpRotationOffset, stacked), Is.LessThan(0.0001f),
                    "Adding a lower note must change the existing note's floor-layer seed.");
            }
            finally
            {
                grid.DeleteObject(lower, triggersAction: false);
            }
            Assert.That(Vector3.Distance(note.JumpRotationOffset, initial), Is.LessThan(0.0001f),
                "Deleting the lower note must restore the original floor-layer seed.");
        }

        private struct RenderedNote
        {
            public NoteContainer Note;
            public Quaternion Rotation;
            public Quaternion BaseRotation;
        }

        private IEnumerator CheckRenderedRotations(List<string> failures, float beat, float previousSeconds,
            string capturePath = null)
        {
            var renderObject = new GameObject("Megamix rendered note poses");
            RenderedNote[] poses = null;
            var head = Vector3.zero;
            try
            {
                var renderer = renderObject.AddComponent<CameraRenderAfterLateUpdate>();
                renderer.TargetCamera = cameras.PlayerCameraController.Camera;
                renderer.BeforeRender = () =>
                {
                    poses = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                        .OfType<NoteContainer>().Where(note => !note.NoteData.CustomFake
                            && (note.NoteData.JsonTime == 415f || note.NoteData.JsonTime == 415.5f))
                        .Select(note => new RenderedNote
                        {
                            Note = note,
                            Rotation = note.DirectionTarget.rotation,
                            BaseRotation = note.DirectionTarget.parent.rotation
                        }).ToArray();
                    head = cameras.PlayerCameraController.transform.position;
                    if (capturePath != null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(capturePath));
                        LinearRenderCapture.Capture(renderer.TargetCamera, capturePath);
                    }
                };
                yield return null;
                Assert.That(poses, Is.Not.Null, "Facing assertions must observe the production LateUpdate pass.");
                CheckRotations(failures, beat, previousSeconds, poses, head);
            }
            finally
            {
                Object.DestroyImmediate(renderObject);
            }
        }

        private void CheckRotations(List<string> failures, float beat, float previousSeconds,
            RenderedNote[] poses, Vector3 headPosition)
        {
            Assert.That(poses.Length, Is.EqualTo(4));
            foreach (var pose in poses)
            {
                var note = pose.Note;
                var data = note.NoteData;
                Assert.That(data.HalfJumpDuration, Is.EqualTo(1.25f).Within(0.0001f));
                var progress = Mathf.Clamp01((clock.CurrentSongBpmTime - data.SpawnSongBpmTime) / (2f * data.HalfJumpDuration));
                // NoteJump.Init's table indices are 1,2,5,6 for these four original notes.
                // All four start alone in their columns, so their floor Y is .25, including the top-layer blue.
                var tilt = data.PosX switch
                {
                    0 => new Vector3(0.7680854f, -0.08805521f, 0.6342642f),
                    1 => new Vector3(-0.6780157f, 0.306681f, -0.6680131f),
                    2 => new Vector3(-0.8790653f, -0.06244748f, -0.4725934f),
                    _ => new Vector3(0.01886305f, -0.8065798f, 0.5908241f)
                };
                // The native time-row processor aligns the blue pair to its (1,2) lane/layer line.
                var end = Quaternion.Euler(0f, 0f, data.Color == 0 ? -90f : 153.43495f);
                Assert.That(Quaternion.Angle(Quaternion.Euler(note.DirectionTargetEuler), end), Is.LessThan(0.05f));
                var middle = Quaternion.Euler(end.eulerAngles + tilt * 20f);
                var expected = progress < 0.125f
                    ? Quaternion.Slerp(Quaternion.identity, middle, Mathf.Sin(progress * Mathf.PI * 4f))
                    : Quaternion.Slerp(middle, end, Mathf.Sin((progress - 0.125f) * Mathf.PI * 2f));
                var actual = Track.GetNoteSpawnRotation(note, progress);
                var error = Quaternion.Angle(actual, expected);
                if (error > 0.05f)
                {
                    failures.Add($"b{beat} note b{data.JsonTime} x{data.PosX}: progress={progress:F6}, jump turn differs by {error:F3} degrees.");
                }
                var authored = pose.BaseRotation * expected;
                // Noodle's DoNoteLook reads the preceding frame's transform before NoteJump writes its new position.
                var previousProgress = Mathf.Clamp01((clock.GetBeatFromSeconds(previousSeconds) - data.SpawnSongBpmTime)
                    / (2f * data.HalfJumpDuration));
                var previousJump = Track.GetNoteJumpPosition(note, previousProgress);
                var previousPosition = new Vector3(Track.GetNoteJumpHorizontalPosition(note, previousProgress),
                    previousJump.x, previousJump.y);
                var head = headPosition;
                head.y = Mathf.Lerp(head.y, previousPosition.y, 0.8f);
                var look = Quaternion.LookRotation((previousPosition - head).normalized, authored * Vector3.up);
                var expectedFacing = Quaternion.Lerp(authored, look, progress * 2f);
                var facingError = Quaternion.Angle(pose.Rotation, expectedFacing);
                if (facingError > 0.1f)
                {
                    failures.Add($"b{beat} note b{data.JsonTime} x{data.PosX}: rendered facing differs by {facingError:F3} degrees.");
                }
            }
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
            }
            if (cameras != null)
            {
                cameras.SelectCamera(CameraType.Editing);
            }
            if (clock != null)
            {
                clock.enabled = previousClockEnabled;
            }
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraFOV = previousFov;
            Settings.Instance.PlayerCameraOffsetZ = previousOffset;
            yield break;
        }
    }
}
