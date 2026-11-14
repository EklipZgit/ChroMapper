using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    public class TrackTimePrecedenceTest : TestBase
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

        [UnityTearDown]
        public IEnumerator RestoreAnimations()
        {
            mode = Object.FindAnyObjectByType<UIMode>();
            if (mode != null)
                mode.SetUIMode(UIModeType.Normal, false);

            cameras = Object.FindAnyObjectByType<CameraManager>();
            if (cameras != null)
                cameras.SelectCamera(CameraType.Editing);

            if (clock != null)
                clock.enabled = previousClockEnabled;

            Settings.Instance.Animations = previousAnimations;
            yield break;
        }

        [UnityTest]
        public IEnumerator TrackWithoutAValueDoesNotHideANoteHeldByALaterTrack()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"
                    {
                        ""version"":""3.3.0"",
                        ""colorNotes"":[
                            {""b"":4,""x"":0,""y"":0,""c"":0,""d"":8,""customData"":{""track"":[""A"",""B""]}}
                        ],
                        ""customData"":{""customEvents"":[
                            {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""B"",""duration"":0,""time"":[[0.25,0]]}},
                            {""b"":40,""t"":""AnimateTrack"",""d"":{""track"":""A"",""duration"":0,""time"":[[0.25,0]]}}
                        ]}
                    }"),
                beatsPerMinute: 120,
                songLengthSeconds: 60);

            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(tracksManager.TryGetTrackTime("A", 10f, out _), Is.False);

            var note = BeatSaberSongContainer.Instance.Map.Notes.Single();
            var index = new FrozenNoteTrackIndex();
            index.Reset(BeatSaberSongContainer.Instance.Map.Notes);
            var notes = new HashSet<BaseNote>();
            index.GetNotesBefore(40f, 10f, tracksManager, notes);
            Assert.That(notes, Does.Contain(note));
        }

        [UnityTest]
        public IEnumerator NoteSpawnPoseUsesTheFirstTrackTime()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(@"
                    {
                        ""version"":""3.3.0"",
                        ""colorNotes"":[
                            {""b"":8,""x"":0,""y"":0,""c"":0,""d"":8,""customData"":{
                                ""track"":[""A"",""B""],
                                ""animation"":{""offsetPosition"":[[0,0,0,0],[0,40,0,1]]}
                            }},
                            {""b"":8,""x"":2,""y"":0,""c"":0,""d"":8,""customData"":{
                                ""track"":""A"",
                                ""animation"":{""offsetPosition"":[[0,0,0,0],[0,40,0,1]]}
                            }}
                        ],
                        ""customData"":{""customEvents"":[
                            {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""A"",""duration"":0,""time"":[[0.25,0]]}},
                            {""b"":0,""t"":""AnimateTrack"",""d"":{""track"":""B"",""duration"":0,""time"":[[0.75,0]]}}
                        ]}
                    }"),
                beatsPerMinute: 120,
                songLengthSeconds: 60);

            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            mode = Object.FindAnyObjectByType<UIMode>();
            cameras = Object.FindAnyObjectByType<CameraManager>();
            mode.SetUIMode(UIModeType.Preview, false);
            cameras.SelectCamera(CameraType.Playing);
            clock.MoveToJsonTime(7f);
            yield return null;
            yield return null;

            var notes = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>()
                .Where(note => note.NoteData.JsonTime == 8f)
                .ToArray();
            Assert.That(notes.Length, Is.EqualTo(2));
            var bothTracks = notes.Single(note => note.NoteData.CustomTrack is JSONArray track && track.Count == 2);
            var firstTrackOnly = notes.Single(note => note.NoteData.CustomTrack is JSONString);
            var tracksManager = Object.FindAnyObjectByType<TracksManager>();
            Assert.That(tracksManager.TryGetTrackTime("A", 8f, out var firstTrackTime), Is.True);
            Assert.That(firstTrackTime, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(tracksManager.TryGetTrackTime("B", 8f, out var secondTrackTime), Is.True);
            Assert.That(secondTrackTime, Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(
                bothTracks.Animator.AnimatedProperties.TryGetValue(
                    AnimationPropertyKeys.OffsetPositionKey,
                    out var offsetProperty),
                Is.True);
            var pathStart = bothTracks.NoteData.SpawnSongBpmTime;
            var pathEnd = bothTracks.NoteData.SongBpmTime + bothTracks.NoteData.HalfJumpDuration;
            var path = (AnimateProperty<Vector3>)offsetProperty;
            var firstPathPosition = path.GetLerpedValue(Mathf.LerpUnclamped(pathStart, pathEnd, firstTrackTime));
            var secondPathPosition = path.GetLerpedValue(Mathf.LerpUnclamped(pathStart, pathEnd, secondTrackTime));
            Assert.That(
                Mathf.Abs(firstPathPosition.y - secondPathPosition.y),
                Is.GreaterThan(1f),
                $"The path must distinguish track A from track B: A={firstPathPosition}, B={secondPathPosition}.");

            bothTracks.Animator.GetNoteSpawnPose(
                bothTracks,
                out var actualPosition,
                out _,
                out _);
            firstTrackOnly.Animator.GetNoteSpawnPose(
                firstTrackOnly,
                out var expectedPosition,
                out _,
                out _);

            Assert.That(
                actualPosition.y,
                Is.EqualTo(expectedPosition.y).Within(0.001f),
                "The multi-track note should use track A's path time, matching the note assigned only to A.");
        }
    }
}
