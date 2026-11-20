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
    public class AetherBeat77NoteLookTest : TestBase
    {
        private const float BeatsPerSecond = 145f / 60f;
        private const int ReproducedHistoryLength = 32;
        private bool previousAnimations;
        private float previousCameraOffset;
        private RenderedPoseProbe probe;

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
        public IEnumerator Beat77PairCarriesNativeNoteLookThroughTheLocalRotationFlourish()
        {
            Settings.Instance.Animations = true;
            var fixturePath = PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "AetherBeat77NoteLookFixture.json");
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(fixturePath)),
                beatsPerMinute: 145f,
                environmentName: "BillieEnvironment",
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 16f,
                    NoteStartBeatOffset = -0.5f,
                    CustomRequirements = new List<string> { "Noodle Extensions", "Chroma" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.PlayerCameraOffsetZ), 0f);
            var clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var cameras = Object.FindAnyObjectByType<CameraManager>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            cameras.SelectCamera(CameraType.Playing);
            using var playback = new DeterministicPlaybackClock(clock);
            playback.StartAtJsonTime(68.95f);
            yield return null;
            yield return null;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var notes = grid.LoadedContainers.Values.OfType<NoteContainer>().OrderBy(n => n.NoteData.PosX).ToArray();
            Assert.That(notes.Length, Is.EqualTo(2));
            Assert.That(notes[0].NoteData.SpawnSongBpmTime, Is.EqualTo(69f).Within(0.0001f));
            Assert.That(notes[1].NoteData.SpawnSongBpmTime, Is.EqualTo(69f).Within(0.0001f));
            probe = new GameObject("Beat 77 rendered pose probe").AddComponent<RenderedPoseProbe>();
            probe.Notes = notes;
            yield return null;

            var history = new[] { new List<LookFrame>(), new List<LookFrame>() };
            var local = new[] { Quaternion.identity, Quaternion.identity };
            var previousPosition = notes.Select(n => n.DirectionTarget.position).ToArray();
            var maximumError = 0f;
            var samples = new List<string>();
            var seekSamples = new List<(float Beat, Quaternion[] Facing)>();
            // Compute the reference from jump start using Noodle's previous visual up and root position.
            // Feeding it the same motion, spawn turns, and head as CM isolates note look from camera and travel.
            for (var frame = 0; ; frame++)
            {
                var beat = 69f + frame * (BeatsPerSecond / TimeHelper.PreviewCallbackRate);
                if (beat >= 77f)
                    break;

                playback.AdvanceToJsonTime(beat);
                yield return null;
                yield return null;
                var head = cameras.PlayerCameraController.GetPlayerHeadPosition();
                var lifetime = (clock.CurrentSongBpmTime - 69f) / 16f;
                var inspect = Mathf.Abs(beat - 76.28125f) < 0.014f || beat > 76.97f;
                if (inspect)
                    seekSamples.Add((clock.CurrentJsonTime, new Quaternion[notes.Length]));

                for (var i = 0; i < notes.Length; i++)
                {
                    var note = notes[i];
                    var visual = probe.Poses[i];
                    var root = visual.Root;
                    var inverseWorld = Quaternion.Inverse(visual.World);
                    var headInTravelSpace = inverseWorld * head;
                    var targetInTravelSpace = inverseWorld * previousPosition[i];
                    headInTravelSpace.y = Mathf.Lerp(headInTravelSpace.y, targetInTravelSpace.y, 0.8f);
                    var forward = root * (targetInTravelSpace - headInTravelSpace).normalized;
                    var spawn = root * Track.GetNoteSpawnRotation(note, lifetime);
                    var lookFrame = new LookFrame(root, forward, spawn, lifetime * 2f);
                    history[i].Add(lookFrame);
                    local[i] = lookFrame.Advance(local[i]);
                    previousPosition[i] = visual.Position;
                    if (inspect)
                    {
                        var expected = root * local[i];
                        seekSamples[^1].Facing[i] = expected;
                        var recentLocal = Quaternion.identity;
                        var oldest = Mathf.Max(0, history[i].Count - 1 - ReproducedHistoryLength);
                        for (var j = oldest; j < history[i].Count; j++)
                            recentLocal = history[i][j].Advance(recentLocal);

                        var error = Quaternion.Angle(visual.Rotation, expected);
                        maximumError = Mathf.Max(maximumError, error);
                        var camera = cameras.PlayerCameraController.Camera;
                        samples.Add(
                            $"b{beat:F5} lane {note.NoteData.PosX}: CM {ScreenArrowRoll(camera, visual.Position, visual.Rotation):F2}°, "
                            + $"Noodle recurrence {ScreenArrowRoll(camera, visual.Position, expected):F2}°, pose error {error:F2}°. "
                            + $"Reset 32 frames back {ScreenArrowRoll(camera, visual.Position, root * recentLocal):F2}°, "
                            + $"pose error vs CM {Quaternion.Angle(visual.Rotation, root * recentLocal):F2}°. "
                            + $"Forward error {Vector3.Angle(visual.Rotation * Vector3.forward, expected * Vector3.forward):F2}°.");
                    }
                }
            }

            playback.Pause();
            // Force a cold seek after both notes leave the pool, then scrub back and forward.
            probe.Notes = System.Array.Empty<NoteContainer>();
            clock.MoveToJsonTime(0f);
            yield return null;
            Assert.That(grid.LoadedContainers.Values.OfType<NoteContainer>(), Is.Empty);
            foreach (var sample in new[] { seekSamples[^1], seekSamples[0], seekSamples[^1] })
            {
                clock.MoveToJsonTime(sample.Beat);
                notes = grid.LoadedContainers.Values.OfType<NoteContainer>().OrderBy(n => n.NoteData.PosX).ToArray();
                Assert.That(notes.Length, Is.EqualTo(2));
                probe.Notes = notes;
                yield return null;
                yield return null;
                for (var i = 0; i < notes.Length; i++)
                {
                    var v = probe.Poses[i];
                    var cam = cameras.PlayerCameraController.Camera;
                    var error = Quaternion.Angle(v.Rotation, sample.Facing[i]);
                    maximumError = Mathf.Max(maximumError, error);
                    samples.Add(
                        $"Seek b{clock.CurrentJsonTime:F5} lane {notes[i].NoteData.PosX}: "
                        + $"CM {ScreenArrowRoll(cam, v.Position, v.Rotation):F2}°, pose error {error:F2}°.");
                }
            }
            Assert.That(maximumError, Is.LessThan(0.5f), string.Join("\n", samples));
        }

        private readonly struct LookFrame
        {
            private readonly Quaternion root;
            private readonly Vector3 forward;
            private readonly Quaternion spawn;
            private readonly float weight;

            public LookFrame(Quaternion root, Vector3 forward, Quaternion spawn, float weight)
            {
                this.root = root;
                this.forward = forward;
                this.spawn = spawn;
                this.weight = weight;
            }

            public Quaternion Advance(Quaternion previousLocal)
            {
                var look = Quaternion.LookRotation(forward, root * previousLocal * Vector3.up);
                return Quaternion.Inverse(root) * Quaternion.Lerp(spawn, look, weight);
            }
        }

        // Track.UpdateTime writes the jump turn before ObjectAnimator applies the look in LateUpdate.
        [DefaultExecutionOrder(1000)]
        private sealed class RenderedPoseProbe : MonoBehaviour
        {
            public NoteContainer[] Notes;
            public readonly (Vector3 Position, Quaternion Rotation, Quaternion Root, Quaternion World)[] Poses = new
                (Vector3, Quaternion, Quaternion, Quaternion)[2];

            private void LateUpdate()
            {
                for (var i = 0; i < Notes.Length; i++)
                {
                    var note = Notes[i];
                    var visual = note.DirectionTarget;
                    Poses[i] = (visual.position, visual.rotation, visual.parent.rotation, note.Animator.WorldTarget.rotation);
                }
            }
        }

        private static float ScreenArrowRoll(Camera camera, Vector3 position, Quaternion rotation)
        {
            var face = position - rotation * Vector3.forward * 0.25f;
            var left = camera.WorldToScreenPoint(face - rotation * Vector3.up * 0.05f);
            var right = camera.WorldToScreenPoint(face + rotation * Vector3.up * 0.05f);
            return Mathf.Atan2(left.y - right.y, right.x - left.x) * Mathf.Rad2Deg;
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
        {
            if (probe != null)
                Object.DestroyImmediate(probe.gameObject);

            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            Settings.Instance.PlayerCameraOffsetZ = previousCameraOffset;
            Settings.ManuallyNotifySettingUpdatedEvent(nameof(Settings.PlayerCameraOffsetZ), previousCameraOffset);
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
