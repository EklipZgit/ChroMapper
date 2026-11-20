using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Containers;
using Beatmap.Info;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    public class AetherIntroPathTest : TestBase
    {
        private static readonly float[] Boundaries = { 5f, 8f, 32f, 48f, 61f, 64f };
        private JSONNode fixture;
        private bool previousAnimations;
        private AudioTimeSyncController clock;
        private bool previousClockEnabled;

        protected override IEnumerator OnMapLoaded()
        {
            previousAnimations = Settings.Instance.Animations;
            yield break;
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator IndependentPathsComposeAcrossIntroEventBoundariesDuringSeeks()
        {
            yield return LoadFixture();
            foreach (var boundary in Boundaries)
            {
                foreach (var offset in new[] { -0.015625f, 0f, 0.015625f, 1f, -0.015625f })
                {
                    clock.MoveToJsonTime(boundary + offset);
                    yield return null;
                    yield return null;
                    AssertNotePose(boundary);
                }
            }
        }

        [UnityTest]
        public IEnumerator IndependentPathsComposeAcrossIntroEventBoundariesDuringPlayback()
        {
            yield return LoadFixture();
            using var playback = new DeterministicPlaybackClock(clock);
            foreach (var boundary in Boundaries)
            {
                playback.StartAtJsonTime(boundary - 0.03125f);
                foreach (var offset in new[] { -0.015625f, 0f, 0.015625f, 1f })
                {
                    playback.AdvanceToJsonTime(boundary + offset);
                    yield return null;
                    yield return null;
                    Assert.That(clock.SongAudioSource.isPlaying, Is.False);
                    AssertNotePose(boundary);
                }
                playback.Pause();
            }
        }

        [UnityTest]
        public IEnumerator LocalDissolveReplacesInheritedPaths()
        {
            yield return LoadFixture("AetherLocalPathFixture.json");
            foreach (var beat in new[] { 100.5f, 101.25f, 100.5f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single(n => n.NoteData.JsonTime == 102.25f);
                Assert.That(note.MpbController.Mpb.GetFloat("_Cutout"), Is.EqualTo(0f).Within(0.0001f));
                Assert.That(note.ArrowMpbController.Mpb.GetFloat("_Cutout"), Is.EqualTo(0f).Within(0.0001f));
            }
        }

        [UnityTest]
        public IEnumerator LocalRotationReplacesInheritedPaths()
        {
            yield return LoadFixture("AetherLocalPathFixture.json");
            foreach (var beat in new[] { 533f, 534f, 533f })
            {
                clock.MoveToJsonTime(beat);
                yield return null;
                yield return null;
                var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                    .OfType<NoteContainer>().Single(n => n.NoteData.JsonTime == 549.001f);
                var lifetime = (clock.CurrentSongBpmTime - note.NoteData.SpawnSongBpmTime)
                    / (2f * note.NoteData.HalfJumpDuration);
                var source = fixture["colorNotes"][1]["customData"];
                var expected = Quaternion.Euler(source["worldRotation"].ReadVector3())
                    * Curve(source["animation"]["offsetWorldRotation"], PointDataParsers.ParseQuaternion)
                        .Interpolate(lifetime);
                Assert.That(
                    Quaternion.Angle(note.Animator.WorldTarget.localRotation, expected),
                    Is.LessThan(0.01f),
                    "The object's rotation curve replaces buildupNote's rotation path.");
            }
        }

        private IEnumerator LoadFixture(string fixtureName = "AetherIntroPathFixture.json")
        {
            fixture = JSON.Parse(File.ReadAllText(PathUtils.Combine(
                Application.dataPath,
                "Tests/Fixtures",
                fixtureName)));
            Settings.Instance.Animations = true;
            yield return TestUtils.ReloadMap(
                3,
                fixture.Clone(),
                beatsPerMinute: 145f,
                songLengthSeconds: 240,
                environmentName: "BillieEnvironment",
                difficultyInfo: new InfoDifficulty(new InfoDifficultySet { Characteristic = "Lawless" })
                {
                    Difficulty = "ExpertPlus",
                    NoteJumpSpeed = 16f,
                    NoteStartBeatOffset = -0.5f,
                    CustomRequirements = new List<string> { "Noodle Extensions", "Chroma" }
                });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            clock = Object.FindAnyObjectByType<AudioTimeSyncController>();
            previousClockEnabled = clock.enabled;
            clock.enabled = false;
            clock.SongAudioSource.Stop();
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Preview, false);
        }

        private void AssertNotePose(float boundary)
        {
            var noteBeat = fixture["colorNotes"].Children.First(n => n["b"].AsFloat >= boundary + 2f)["b"].AsFloat;
            var note = Object.FindAnyObjectByType<NoteGridContainer>().LoadedContainers.Values
                .OfType<NoteContainer>().Single(n => n.NoteData.JsonTime == noteBeat);
            var data = note.NoteData;
            var lifetime = (clock.CurrentSongBpmTime - data.SpawnSongBpmTime) / (2f * data.HalfJumpDuration);
            var rotation = Quaternion.identity;
            var offset = Vector3.zero;
            foreach (var track in data.CustomTrack.Children)
            {
                rotation *= SampleTrackRotation(track.Value, clock.CurrentJsonTime, lifetime);
                offset += SampleTrackPosition(track.Value, clock.CurrentJsonTime, lifetime);
            }
            Assert.That(
                Quaternion.Angle(note.Animator.WorldTarget.localRotation, rotation),
                Is.LessThan(0.01f),
                $"b{clock.CurrentJsonTime:R}, note b{noteBeat}: each track must retain its own rotation transition.");
            Assert.That(
                Vector3.Distance(note.Animator.LocalTarget.localPosition, offset * BeatmapConstant.LaneSize),
                Is.LessThan(0.001f),
                $"b{clock.CurrentJsonTime:R}, note b{noteBeat}: intro and fakeJump offsets must add.");
        }

        private JSONNode[] Requests(string track, string property, float beat) =>
            fixture["customData"]["customEvents"].Children.Where(e =>
                e["d"]["track"].Value == track && e["d"].HasKey(property) && e["b"].AsFloat <= beat).ToArray();

        private static PointDefinition<T> Curve<T>(JSONNode points, PointDefinition<T>.Parser parser) where T : struct =>
            new(parser, new IPointDefinition.UntypedParams { Points = points, TimeEnd = 1f }, null);

        // Heck keeps the previous curve on the same track, samples both at the note lifetime,
        // then blends with event time. Noodle multiplies rotations and adds positions in track order.
        private Quaternion SampleTrackRotation(string track, float beat, float lifetime)
        {
            var requests = Requests(track, "offsetWorldRotation", beat);
            if (requests.Length == 0)
                return Quaternion.identity;

            var current = requests[^1];
            var value = Curve(current["d"]["offsetWorldRotation"], PointDataParsers.ParseQuaternion).Interpolate(lifetime);
            if (requests.Length == 1 || current["d"]["duration"].AsFloat == 0f)
                return value;

            var previous = Curve(requests[^2]["d"]["offsetWorldRotation"], PointDataParsers.ParseQuaternion)
                .Interpolate(lifetime);
            var progress = Mathf.Clamp01((beat - current["b"].AsFloat) / current["d"]["duration"].AsFloat);
            return Quaternion.SlerpUnclamped(previous, value, Easing.HeckNamed(current["d"]["easing"].Value)(progress));
        }

        private Vector3 SampleTrackPosition(string track, float beat, float lifetime)
        {
            var requests = Requests(track, "offsetPosition", beat);
            if (requests.Length == 0)
                return Vector3.zero;

            var current = requests[^1];
            var value = Curve(current["d"]["offsetPosition"], PointDataParsers.ParseVector3).Interpolate(lifetime);
            if (requests.Length == 1 || current["d"]["duration"].AsFloat == 0f)
                return value;

            var previous = Curve(requests[^2]["d"]["offsetPosition"], PointDataParsers.ParseVector3).Interpolate(lifetime);
            var progress = Mathf.Clamp01((beat - current["b"].AsFloat) / current["d"]["duration"].AsFloat);
            return Vector3.LerpUnclamped(previous, value, Easing.HeckNamed(current["d"]["easing"].Value)(progress));
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
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Normal, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Editing);
            Settings.Instance.Animations = previousAnimations;
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.3.0" });
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
