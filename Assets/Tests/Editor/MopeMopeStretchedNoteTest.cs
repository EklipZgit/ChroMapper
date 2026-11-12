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
    public class MopeMopeStretchedNoteTest : TestBase
    {
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
        public IEnumerator StretchedSideNoteUsesTheGamesBaseLayerNearBeat235() => CheckHeight(false);

        [UnityTest]
        public IEnumerator ExplicitCustomHeightRetainsTheHighNoodleLayer() => CheckHeight(true);

        private IEnumerator CheckHeight(bool explicitHeight)
        {
            Settings.Instance.Animations = true;
            var map = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "MopeMopeStretchedNoteFixture.json")));
            if (explicitHeight)
                map["_notes"][0]["_customData"]["_position"] = new JSONArray { [0] = -5f, [1] = 8f };

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
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Preview, false);
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var source = grid.MapObjects.Single();
            // BeatmapTypeConverters maps raw layer 8 to Base. Only an explicit Noodle Y keeps layer 8.
            var apex = explicitHeight ? 5.058333f : 0.858333f;
            foreach (var approach in new[] { 220f, 400f })
            {
                clock.MoveToJsonTime(approach);
                yield return null;
                clock.MoveToJsonTime(235f);
                yield return null;
                yield return null;
                CheckPose();
            }

            mode.SetUIMode(UIModeType.Playing, false);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 235f);
            foreach (var beat in new[] { 235f, 235.1875f, 235.5f })
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                yield return null;
                yield return null;
                CheckPose();
            }

            void CheckPose()
            {
                Assert.That(grid.LoadedContainers.ContainsKey(source), Is.True);
                var note = (NoteContainer)grid.LoadedContainers[source];
                Assert.That(source.PosY, Is.EqualTo(8), "Editing and export must preserve the original raw layer.");
                Assert.That(note.JumpApexHeight, Is.EqualTo(apex).Within(0.0001f));
                var normalized = (clock.CurrentSongBpmTime - source.SpawnSongBpmTime) / (source.HalfJumpDuration * 2f);
                var expectedY = 0.25f + (apex - 0.25f) * 4f * normalized * (1f - normalized);
                Assert.That(note.Animator.LocalTarget.position.y, Is.EqualTo(expectedY).Within(0.001f));
                Assert.That(note.Animator.LocalTarget.lossyScale.y, Is.InRange(9f, 12.001f));
                var bottom = note.ModelController.MpbController.Renderers
                    .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
                    .Min(renderer => renderer.bounds.min.y);
                if (!explicitHeight)
                    Assert.That(bottom, Is.LessThan(0f), "The stretched note must extend below the player's feet.");

            }
        }

        [UnityTearDown]
        public IEnumerator RestoreMap()
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
