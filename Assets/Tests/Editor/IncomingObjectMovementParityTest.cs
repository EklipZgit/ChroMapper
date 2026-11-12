using System.Collections;
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
    public class IncomingObjectMovementParityTest : TestBase
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

        private IEnumerator LoadFixture()
        {
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(3, JSON.Parse(File.ReadAllText(PathUtils.Combine(Application.dataPath,
                    "Tests", "Fixtures", "NativeIncomingObjectsFixture.json"))), beatsPerMinute: 142f,
                songLengthSeconds: 90,
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Standard" })
                {
                    Difficulty = "ExpertPlus", NoteJumpSpeed = 18f, NoteStartBeatOffset = -0.5f
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

        [UnityTest]
        public IEnumerator ArcUsesNativeSliderTravelBeforeAndAfterJumpStart()
        {
            yield return LoadFixture();
            var data = Object.FindAnyObjectByType<ArcGridContainer>().MapObjects.Single();
            var headSeconds = clock.GetSecondsFromBeat(data.SongBpmTime);
            var jumpStart = headSeconds - clock.GetSecondsFromBeat(data.HalfJumpDuration);
            foreach (var offset in new[] { -0.49f, -0.25f, -0.01f, 0.01f, 0.25f, -0.25f })
            {
                clock.MoveToTimeInSeconds(jumpStart + offset);
                yield return null;
                yield return null;
                var arc = Object.FindAnyObjectByType<ArcGridContainer>().LoadedContainers.Values
                    .OfType<ArcContainer>().Single();
                // SliderMovement.ManualUpdate extrapolates jump travel, including before the head's jump start.
                var expectedZ = (headSeconds - clock.CurrentSeconds) * 18f + BeatmapConstant.ZOffset;
                Assert.That(arc.AssignedTrack.ObjectParentTransform.position.z,
                    Is.EqualTo(expectedZ).Within(0.001f), $"Arc at jump-start offset {offset}.");
            }
        }

        [UnityTest]
        public IEnumerator EveryChainSliceUsesItsOwnNativeFloorTiming()
        {
            yield return LoadFixture();
            var data = Object.FindAnyObjectByType<ChainGridContainer>().MapObjects.Single();
            var headSeconds = clock.GetSecondsFromBeat(data.SongBpmTime);
            var tailSeconds = clock.GetSecondsFromBeat(data.TailSongBpmTime);
            var halfSeconds = clock.GetSecondsFromBeat(data.HalfJumpDuration);
            foreach (var offset in new[] { -0.49f, -0.25f, -0.01f, 0.01f, 0.25f, -0.25f })
            {
                clock.MoveToTimeInSeconds(headSeconds - halfSeconds + offset);
                yield return null;
                yield return null;
                var chain = Object.FindAnyObjectByType<ChainGridContainer>().LoadedContainers.Values
                    .OfType<ChainContainer>().Single();
                for (var index = 0; index < data.SliceCount - 1; index++)
                {
                    var sliceSeconds = Mathf.Lerp(headSeconds, tailSeconds, (index + 1f) / (data.SliceCount - 1f));
                    var expectedZ = GetNativeNoteZ(sliceSeconds, halfSeconds);
                    // At this beat, float seconds can lose a few millimetres during the 200 m/s approach.
                    Assert.That(chain.Nodes[index].transform.position.z,
                        Is.EqualTo(expectedZ).Within(0.004f), $"Chain slice {index} at jump-start offset {offset}.");
                }
            }
        }

        [UnityTest]
        public IEnumerator BombRetainsNativeFloorAndJumpTravel()
        {
            yield return LoadFixture();
            var data = Object.FindAnyObjectByType<NoteGridContainer>().MapObjects.Single(value => value.Type == 3);
            var noteSeconds = clock.GetSecondsFromBeat(data.SongBpmTime);
            var halfSeconds = clock.GetSecondsFromBeat(data.HalfJumpDuration);
            foreach (var offset in new[] { -0.25f, -0.01f, 0.01f, 0.25f, -0.25f })
            {
                clock.MoveToTimeInSeconds(noteSeconds - halfSeconds + offset);
                yield return null;
                yield return null;
                var bomb = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single(value => value.NoteData.Type == 3);
                Assert.That(bomb.DirectionTarget.position.z,
                    Is.EqualTo(GetNativeNoteZ(noteSeconds, halfSeconds, BeatmapConstant.NoteZOffset)).Within(0.001f));
            }
        }

        private float GetNativeNoteZ(float noteSeconds, float halfSeconds, float zOffset = BeatmapConstant.ZOffset)
        {
            var jumpStart = noteSeconds - halfSeconds;
            return (clock.CurrentSeconds < jumpStart
                ? 18f * halfSeconds + 100f * (jumpStart - clock.CurrentSeconds) / 0.5f
                : (noteSeconds - clock.CurrentSeconds) * 18f) + zOffset;
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
