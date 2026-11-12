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
    public class MopeMopeNoteSpawnTest : TestBase
    {
        private bool previousAnimations;
        private bool previousHideSpawnEffect;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            previousHideSpawnEffect = Settings.Instance.HideNoteSpawnEffect;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator BlueRightSpawnFlashUsesTheOffLanePathOriginDuringSeeksAndPlayback()
        {
            yield return LoadFixture("MopeMopeNoteSpawnFixture.json");
            clock.MoveToJsonTime(137f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single();
            var expectedPosition = new Vector3(
                note.NoteData.GetPosition().x + 20f * BeatmapConstant.LaneSize,
                note.JumpStartHeight - 0.15f,
                note.NoteData.HalfJumpDistance + BeatmapConstant.NoteZOffset);
            yield return CheckSpawnPose(note, expectedPosition, Quaternion.identity);
        }

        [UnityTest]
        public IEnumerator ATrackSpawnFlashUsesTheRaisedAndAngledPathOriginDuringSeeksAndPlayback()
        {
            yield return LoadFixture("MopeMopeAngledNoteSpawnFixture.json");
            clock.MoveToJsonTime(175f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single();
            Assert.That(note.NoteData.SpawnSongBpmTime, Is.EqualTo(175f));
            var expectedPosition = new Vector3(
                note.NoteData.GetPosition().x,
                note.JumpStartHeight + 4f * BeatmapConstant.LaneSize - 0.15f,
                note.NoteData.HalfJumpDistance + BeatmapConstant.NoteZOffset);
            yield return CheckSpawnPose(note, expectedPosition, Quaternion.Euler(0f, 0f, 45f));
        }

        [UnityTest]
        public IEnumerator FlyBombSpawnFlashStartsBelowThePlayerWithItsFirstPathRotation()
        {
            yield return LoadFixture("MopeMopeFlyBombSpawnFixture.json");
            clock.MoveToJsonTime(206.5f);
            yield return null;
            yield return null;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single();
            var expectedRotation = Quaternion.Euler(90f, 180f, 90f);
            var expectedPosition = expectedRotation * new Vector3(
                note.NoteData.GetPosition().x,
                note.JumpStartHeight,
                note.NoteData.HalfJumpDistance + BeatmapConstant.NoteZOffset);
            expectedPosition.y -= 0.15f;
            yield return CheckSpawnPose(note, expectedPosition, expectedRotation);
        }

        private IEnumerator LoadFixture(string fixtureName)
        {
            Settings.Instance.Animations = true;
            Settings.Instance.HideNoteSpawnEffect = false;
            // 129bf ExpertPlus: each fixture keeps one original note and its transform path, with no named points.
            var map = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                fixtureName)));
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
        }

        private IEnumerator CheckSpawnPose(NoteContainer note, Vector3 expectedPosition, Quaternion expectedRotation)
        {
            var source = note.NoteData;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var birth = note.SpawnEffect.SpawnSeconds;
            foreach (var playing in new[] { false, true })
            {
                mode.SetUIMode(playing ? UIModeType.Playing : UIModeType.Preview, false);
                if (playing)
                    TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, clock.GetBeatFromSeconds(birth));

                foreach (var elapsed in new[] { 0f, 0.12f, 0.3f, -0.01f, 0.12f })
                {
                    var beat = clock.GetBeatFromSeconds(birth + elapsed);
                    if (playing)
                        TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                    else
                        clock.MoveToSongBpmTime(beat);

                    yield return null;
                    yield return null;
                    // A reverse seek can replace the pooled container while preserving the source note.
                    var loaded = grid.LoadedContainers.TryGetValue(source, out var container);
                    var visible = loaded && ((NoteContainer)container).SpawnEffect.SpriteRenderer.enabled;
                    Assert.That(
                        visible,
                        Is.EqualTo(elapsed >= 0f),
                        $"b{source.JsonTime}: playing={playing}, elapsed={elapsed}, "
                        + $"clock={clock.CurrentSeconds:R}, birth={birth:R}, loaded={loaded}.");
                    if (elapsed < 0f)
                        continue;

                    note = (NoteContainer)container;
                    // Noodle's BeatEffectUseWorldPosition samples the note at jump start, then leaves the effect there.
                    Assert.That(
                        Quaternion.Angle(note.SpawnEffect.transform.rotation, expectedRotation),
                        Is.LessThan(0.01f),
                        $"The b{note.NoteData.JsonTime} flash must retain its path's birth rotation (playing={playing}).");
                    Assert.That(
                        Vector3.Distance(note.SpawnEffect.transform.position, expectedPosition),
                        Is.LessThan(0.001f),
                        $"The b{note.NoteData.JsonTime} flash must stay at {expectedPosition} (playing={playing}, elapsed={elapsed}).");
                    if (elapsed > 0f)
                        Assert.That(Vector3.Distance(note.Animator.LocalTarget.position, expectedPosition), Is.GreaterThan(0.5f));

                }
                if (playing)
                    TestUtils.PauseDeterministicPlayback(clock);

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
            Settings.Instance.HideNoteSpawnEffect = previousHideSpawnEffect;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
