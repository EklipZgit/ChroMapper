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
    public class MopeMopeFrozenNoteTest : TestBase
    {
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");
        private bool previousAnimations;
        private bool previousClockEnabled;
        private AudioTimeSyncController clock;
        private UIMode mode;
        private CameraManager cameras;
        private NoteGridContainer grid;
        private BaseNote source;
        private Vector3 playingPosition;
        private Quaternion playingRotation;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FrozenFaceDespawnsWhenTrackTimeReachesOneDuringPlayback()
        {
            yield return PlayToBeat200();
            TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, 225f);
            yield return null;
            yield return null;
            Assert.That(grid.LoadedContainers.ContainsKey(source), Is.False,
                "Native NoteJump finishes at normalized time one, including when track time holds exactly one.");
        }

        [UnityTest]
        public IEnumerator FrozenFaceSurvivesPauseAndResumeAtBeat200()
        {
            yield return PlayToBeat200();
            var failures = new List<string>();
            TestUtils.PauseDeterministicPlayback(clock);
            yield return null;
            yield return null;
            CheckFrozenFace("paused", failures);
            grid.RefreshPool(true);
            yield return null;
            yield return null;
            CheckFrozenFace("forced paused refresh", failures);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 200f);
            yield return null;
            yield return null;
            CheckFrozenFace("resumed", failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator FrozenFaceReturnsWhenSeekingFromEarlierAndLaterBeats()
        {
            yield return PlayToBeat200();
            TestUtils.PauseDeterministicPlayback(clock);
            var failures = new List<string>();
            foreach (var earlierOrLater in new[] { 0f, 400f, 0f, 400f })
            {
                clock.MoveToJsonTime(earlierOrLater);
                yield return null;
                yield return null;
                clock.MoveToTimeInSeconds(60f);
                yield return null;
                yield return null;
                CheckFrozenFace($"seek from b{earlierOrLater}", failures);
            }

            clock.MoveToJsonTime(225f);
            yield return null;
            yield return null;
            Assert.That(grid.LoadedContainers.ContainsKey(source), Is.False, "The note must leave when track time reaches its end.");
            clock.MoveToJsonTime(200f);
            yield return null;
            yield return null;
            CheckFrozenFace("seek back after track time released the note", failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private IEnumerator PlayToBeat200()
        {
            Settings.Instance.Animations = true;
            // The original b191.5 fake note and its sole FaceTrack event use only inline curves.
            var map = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests",
                "Fixtures",
                "MopeMopeFrozenNoteFixture.json")));
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
            grid = Object.FindAnyObjectByType<NoteGridContainer>();
            source = grid.MapObjects.Single();
            cameras.SelectCamera(CameraType.Playing);
            mode.SetUIMode(UIModeType.Playing, false);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock, 185f);
            for (var beat = 185f; beat <= 200f; beat += 0.5f)
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock, beat);
                yield return null;
                yield return null;
            }

            Assert.That(clock.CurrentSeconds, Is.EqualTo(60f).Within(0.001f));
            Assert.That(grid.LoadedContainers.ContainsKey(source), Is.True, "Playback must reproduce the frozen face.");
            var note = (NoteContainer)grid.LoadedContainers[source];
            playingPosition = note.Animator.LocalTarget.position;
            playingRotation = note.Animator.LocalTarget.rotation;
            var failures = new List<string>();
            CheckFrozenFace("playing", failures);
            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private void CheckFrozenFace(string phase, List<string> failures)
        {
            if (!grid.LoadedContainers.TryGetValue(source, out var container))
            {
                failures.Add($"{phase}: the b191.5 FaceTrack note disappeared at b200.");
                return;
            }

            var note = (NoteContainer)container;
            Assert.That(note.gameObject.activeInHierarchy, Is.True, phase);
            Assert.That(note.ModelController.MpbController.Renderers.Any(renderer => renderer.enabled), Is.True, phase);
            Assert.That(Vector3.Distance(note.Animator.LocalTarget.position, playingPosition), Is.LessThan(0.001f), phase);
            Assert.That(Quaternion.Angle(note.Animator.LocalTarget.rotation, playingRotation), Is.LessThan(0.01f), phase);
            Assert.That(note.MpbController.Mpb.GetFloat(cutoutId), Is.EqualTo(0.7f).Within(0.001f), phase);
            Assert.That(note.ArrowMpbController.Mpb.GetFloat(cutoutId), Is.EqualTo(0.4f).Within(0.001f), phase);
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
