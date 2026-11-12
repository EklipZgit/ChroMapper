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
    public class Issue628RotationReproTest : TestBase
    {
        private static readonly string[] RotationSources = { "AssignPathAnimation", "AnimateTrack", "NoRotation", "AuthoredRotation" };

        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        [Explicit("Requires the test map downloaded from GitHub issue 628 under workspace Scratch/Issue628/map.")]
        public IEnumerator OriginalMapRetains360TurnWithOffsetWorldRotation()
        {
            return CheckRotation("ExpertPlus360Degree.dat", true);
        }

        [UnityTest]
        public IEnumerator ReducedPairRetains360TurnWithOffsetWorldRotation()
        {
            return CheckRotation("Issue628RotationFixture.json", false);
        }

        [UnityTest]
        public IEnumerator LegacyPairRetains360TurnWithOffsetWorldRotation()
        {
            return CheckRotation("Issue628LegacyRotationFixture.json", false);
        }

        [UnityTest]
        public IEnumerator RotationSourcesPreserveTheBaseTurn([ValueSource(nameof(RotationSources))] string source)
        {
            return CheckRotation("Issue628RotationFixture.json", false, source);
        }

        [UnityTest]
        public IEnumerator RotationCompositionMatchesDuringPlayback()
        {
            return CheckRotation("Issue628RotationFixture.json", false, playback: true);
        }

        private IEnumerator CheckRotation(
            string fileName,
            bool original,
            string source = "Individual",
            bool playback = false)
        {
            Settings.Instance.Animations = true;
            var root = PathUtils.Combine(Application.dataPath, "..", "..", "Scratch", "Issue628");
            var fixturePath = original
                ? PathUtils.Combine(root, "map", fileName)
                : PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", fileName);
            var map = JSON.Parse(File.ReadAllText(fixturePath));
            var legacy = map.HasKey("_version");
            var authoredBase = Quaternion.Euler(10f, -55f, 8f);
            var fixedOffset = Quaternion.Euler(13f, 17f, 29f);
            if (source != "Individual")
            {
                foreach (var note in map["colorNotes"].Children)
                {
                    note["customData"]["animation"].Remove("offsetWorldRotation");
                    if (source == "AuthoredRotation")
                    {
                        note["customData"]["worldRotation"] = JSON.Parse("[10,-55,8]");
                        note["customData"]["animation"]["offsetWorldRotation"] = JSON.Parse("[13,17,29]");
                    }
                }

                if (source is "AssignPathAnimation" or "AnimateTrack")
                {
                    map["customData"]["customEvents"].AsArray.Add(new JSONObject
                    {
                        ["b"] = 0,
                        ["t"] = source,
                        ["d"] = new JSONObject
                        {
                            ["track"] = "circle",
                            ["offsetWorldRotation"] = JSON.Parse("[13,17,29]")
                        }
                    });
                }
            }

            yield return TestUtils.ReloadMap(
                legacy ? 2 : 3,
                map,
                beatsPerMinute: 170f,
                environmentName: "GlassDesertEnvironment",
                songLengthSeconds: 30,
                forceSceneReload: true,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "360Degree" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 16f,
                    NoteStartBeatOffset = 0f,
                    CustomRequirements = new List<string> { "Noodle Extensions", "Chroma" }
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
            var results = new JSONArray();
            var failures = new List<string>();
            var label = original
                ? "original"
                : (legacy ? "legacy-" : "current-") + source;
            if (playback)
                label += "-playback";

            var directory = PathUtils.Combine(root, System.DateTime.UtcNow.ToString("yyyyMMddTHHmmssfff") + "-" + label);
            Directory.CreateDirectory(directory);
            if (playback)
                TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 23f);

            var beats = playback
                ? new[] { 23f, 24.5f, 26f, 27f }
                : new[] { 23f, 26f, 27f, 25.5f, 26f };
            foreach (var beat in beats)
            {
                if (playback)
                {
                    typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds))
                        .SetValue(clock, clock.GetSecondsFromBeat(beat));
                }
                else
                    clock.MoveToJsonTime(beat);

                yield return null;
                yield return null;
                var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Where(value => value.NoteData.JsonTime == 28f).ToArray();
                Assert.That(notes.Length, Is.EqualTo(2));
                foreach (var note in notes)
                {
                    note.Animator.ApplyAnimation();
                    var data = note.NoteData;
                    var baseYaw = note.Animator.TracksManager.GetRotationAtTime(data.SongBpmTime);
                    Assert.That(baseYaw, Is.EqualTo(-30));
                    var offset = source == "NoRotation"
                        ? Quaternion.identity
                        : fixedOffset;
                    if (source == "Individual")
                    {
                        var path = (Beatmap.Animations.AnimateProperty<Quaternion>)note.Animator.AnimatedProperties[legacy ? "_rotation" : "offsetWorldRotation"];
                        offset = path.GetLerpedValue(clock.CurrentSongBpmTime);
                    }

                    var expectedBase = source == "AuthoredRotation"
                        ? authoredBase
                        : Quaternion.Euler(0f, -30f, 0f);
                    var expected = expectedBase * offset;
                    var actual = note.Animator.WorldTarget.rotation;
                    var error = Quaternion.Angle(actual, expected);
                    results.Add(new JSONObject
                    {
                        ["beat"] = beat,
                        ["noteBeat"] = data.JsonTime,
                        ["color"] = data.Type,
                        ["baseYaw"] = baseYaw,
                        ["offsetEuler"] = offset.eulerAngles.ToString(),
                        ["actualEuler"] = actual.eulerAngles.ToString(),
                        ["expectedEuler"] = expected.eulerAngles.ToString(),
                        ["errorDegrees"] = error
                    });
                    if (error > 0.05f)
                        failures.Add($"b{beat} color {data.Type}: expected base turn plus path offset, angular error {error:F3} degrees.");

                }
                if (beat == 26f)
                    LinearRenderCapture.Capture(
                        cameras.PlayerCameraController.Camera,
                        PathUtils.Combine(directory, "actual.png"));

            }
            File.WriteAllText(PathUtils.Combine(directory, "rotations.json"), results.ToString(2));
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTearDown]
        public IEnumerator RestoreScene()
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
