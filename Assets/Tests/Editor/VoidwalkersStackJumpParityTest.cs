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
    public class VoidwalkersStackJumpParityTest : TestBase
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
        public IEnumerator StackHeightMatchesNativeNoteJumpAcrossSmallScrubs()
        {
            yield return CheckScrubs(false);
        }

        [UnityTest]
        public IEnumerator NoodleStackHeightMatchesHeckSpawnData()
        {
            yield return CheckScrubs(true);
        }

        [UnityTest]
        public IEnumerator StackHeightMatchesNativeNoteJumpDuringPlayback()
        {
            yield return LoadFixture(false);
            TestUtils.StartDeterministicPlaybackAtSongBpmTime(clock,
                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(225.75f));
            foreach (var beat in new[] { 225.75f, 225.890f, 226f, 226.25f })
            {
                TestUtils.AdvanceDeterministicPlaybackAtSongBpmTime(clock,
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(beat));
                yield return null;
                yield return null;
                CheckStack(beat, false);
            }
        }

        [UnityTest]
        public IEnumerator RemovingAndRestoringLowerNoteRefreshesUpperNoteHeight()
        {
            yield return LoadFixture(false);
            clock.MoveToJsonTime(225.890f);
            yield return null;
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var notes = GetStack();
            var lower = notes[0].NoteData;
            var upper = notes[1];
            var stackedY = upper.AssignedTrack.ObjectParentTransform.localPosition.y;
            grid.DeleteObject(lower, triggersAction: false);
            try
            {
                clock.MoveToJsonTime(225.890f);
                yield return null;
                CheckHeight(upper, 0, 1.4f, 225.890f);
                Assert.That(upper.AssignedTrack.ObjectParentTransform.localPosition.y,
                    Is.LessThan(stackedY - 0.2f));
            }
            finally
            {
                grid.SpawnObject(lower);
            }
            clock.MoveToJsonTime(225.890f);
            yield return null;
            CheckStack(225.890f, false);
        }

        [UnityTest]
        public IEnumerator ConnectedArcFollowsTheCorrectedNoteHeight()
        {
            yield return LoadFixture(false);
            foreach (var tail in new[] { false, true })
            {
                clock.MoveToJsonTime(tail ? 190.8f : 188.8f);
                yield return null;
                yield return null;
                var arc = Object.FindAnyObjectByType<ArcGridContainer>().LoadedContainers.Values
                    .OfType<ArcContainer>().Single(value => value.ArcData.JsonTime == 189f);
                var data = tail ? arc.ArcData.TailNotes.Single() : arc.ArcData.HeadNotes.Single();
                var note = (NoteContainer)Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers[data];
                CheckHeight(note, tail ? 1 : 0, tail ? 1.9f : 0.85f, tail ? 190.8f : 188.8f);
                var index = tail ? arc.SplineRenderer.positionCount - 1 : 0;
                var endpoint = arc.SplineRenderer.transform.TransformPoint(arc.SplineRenderer.GetPosition(index));
                // SliderMeshController attaches at the cut edge. This fixture cuts right at the head and up-left at the tail.
                var edgeOffsetY = tail ? -0.225f / Mathf.Sqrt(2f) : 0f;
                Assert.That(
                    endpoint.y,
                    Is.EqualTo(note.DirectionTarget.position.y + edgeOffsetY).Within(0.001f),
                    "The original b189-b191 arc must remain attached to its note's cut edge during the jump.");
            }
        }

        private IEnumerator LoadFixture(bool noodle)
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3,
                JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath, "Tests", "Fixtures", "VoidwalkersStackJumpFixture.json"))),
                beatsPerMinute: 142f, songLengthSeconds: 110,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 18f, NoteStartBeatOffset = -0.5f,
                    CustomRequirements = noodle ? new List<string> { "Noodle Extensions" } : new List<string>()
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

        private IEnumerator CheckScrubs(bool noodle)
        {
            yield return LoadFixture(noodle);
            foreach (var beat in new[] { 225.890f, 225.75f, 225.890f + 1f / 64f, 225.890f, 226.25f, 225.890f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                CheckStack(beat, noodle);
            }
        }

        private void CheckStack(float beat, bool noodle)
        {
            var notes = GetStack();
            Assert.That(notes.Length, Is.EqualTo(2));
            for (var layer = 0; layer < notes.Length; layer++)
            {
                // Shipped StandardGameplay values are .85, 1.4 and 1.9 for the three jump apex layers.
                var apexY = noodle ? 0.525f * layer + 0.858333f : layer == 0 ? 0.85f : 1.4f;
                CheckHeight(notes[layer], layer, apexY, beat);
            }
        }

        private static NoteContainer[] GetStack() =>
            Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values.OfType<NoteContainer>()
                .Where(value => value.NoteData.JsonTime == 227f && value.NoteData.Color == 1)
                .OrderBy(value => value.NoteData.PosY).ToArray();

        private void CheckHeight(NoteContainer note, int floorLayer, float apexY, float beat)
        {
            var data = note.NoteData;
            var halfDuration = clock.GetSecondsFromBeat(data.HalfJumpDuration);
            var elapsed = clock.CurrentSeconds - (clock.GetSecondsFromBeat(data.SongBpmTime) - halfDuration);
            var startY = 0.25f + floorLayer * 0.6f;
            var gravity = 2f * (apexY - startY) / (halfDuration * halfDuration);
            var expectedY = startY + gravity * halfDuration * elapsed - 0.5f * gravity * elapsed * elapsed;
            Assert.That(note.AssignedTrack.ObjectParentTransform.localPosition.y,
                Is.EqualTo(expectedY).Within(0.0003f),
                $"Beat {beat}: stack layer {floorLayer} must follow NoteJump.ManualUpdate from its own before-jump layer.");
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
