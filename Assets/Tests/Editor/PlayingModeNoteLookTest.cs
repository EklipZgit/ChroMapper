using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Tests.Editor
{
    // Playing mode rebuilds the game's per-frame note facing by replaying a bounded look-history
    // window over the recently recorded frame steps. The replay must keep each frame's own step:
    // spacing every sample by the current frame's delta rescales the whole window each frame and
    // shows up as a visible rotation stutter while the note flies in.
    public class PlayingModeNoteLookTest : PreviewWorkflowTestBase
    {
        private static readonly PropertyInfo playbackSeconds = typeof(AudioTimeSyncController)
            .GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));

        private AudioTimeSyncController atsc;
        private NoteContainer note;
        private BeatmapObjectContainerCollection<BaseNote> collection;
        private BaseNote data;
        private RenderedFacingProbe renderedProbe;

        // Container pools recycle the note when the clock moves, so every sample re-reads the
        // live pooled instance for the same map object instead of holding a stale reference.
        private Quaternion FacingRotation =>
            collection.LoadedContainers.TryGetValue(data, out var loaded)
                ? ((NoteContainer)loaded).DirectionTarget.localRotation
                : note.DirectionTarget.localRotation;

        [UnityTest]
        public IEnumerator PlayingNoteLookDoesNotPopOnAnOutlierFrame()
        {
            yield return LoadNoteAndEnterPlaying();
            var jumpWindow = data.HalfJumpDuration * 2f;
            var probeBeat = data.SpawnSongBpmTime + jumpWindow * 0.46f;
            var targetSeconds = atsc.GetSecondsFromBeat(probeBeat);

            var rotations = new List<Quaternion>();
            foreach (var lastStep in new[] { HistoryStepSeconds, 0.09f, 0.004f })
            {
                yield return SampleFacingAfterUniformHistory(targetSeconds, lastStep, rotations);
            }

            var spread = 0f;
            for (var i = 0; i < rotations.Count; i++)
            for (var j = i + 1; j < rotations.Count; j++)
                spread = Mathf.Max(spread, AngleDelta(rotations[i], rotations[j]));
            // An anomalous frame legitimately displaces the newest sample's previous position by
            // the extra step, about a degree of facing here. The grid-rescale bug instead moved
            // the whole replay window and measured ~6 degrees in this fixture.
            Assert.That(spread, Is.LessThan(2f),
                "Facing at a fixed song time popped on one anomalous frame: " +
                string.Join(", ", rotations.Select(r => r.eulerAngles.ToString("F2"))));
        }

        [UnityTest]
        public IEnumerator PlayingNoteLookAdvancesSmoothlyAcrossJitteredFrames()
        {
            yield return LoadNoteAndEnterPlaying();
            var jumpWindow = data.HalfJumpDuration * 2f;
            var startBeat = data.SpawnSongBpmTime + jumpWindow * 0.405f;
            var endBeat = data.SpawnSongBpmTime + jumpWindow * 0.495f;

            // Fill the recorded frame-delta history so the measured window replays entirely on
            // the steps driven here instead of fallback entries from the fixture walk-in.
            var t = atsc.GetSecondsFromBeat(startBeat) - WarmupSeconds;
            for (var i = 0; i < ObjectAnimator.NoteLookHistoryFrames + 2; i++)
            {
                playbackSeconds.SetValue(atsc, t += HistoryStepSeconds);
                yield return null;
            }

            var stepPattern = new[] { 0.011f, 0.038f, 0.009f, 0.027f, 0.042f, 0.013f, 0.031f, 0.018f };
            var first = renderedProbe.Rotation;
            var previous = first;
            var steps = new List<float>();
            var path = 0f;
            foreach (var delta in stepPattern)
            {
                var next = Mathf.Min(t + delta, atsc.GetSecondsFromBeat(endBeat));
                t = next;
                playbackSeconds.SetValue(atsc, t);
                yield return null;
                var facing = renderedProbe.Rotation;
                var deltaAngle = AngleDelta(previous, facing);
                steps.Add(deltaAngle);
                path += deltaAngle;
                previous = facing;
            }

            var netDisplacement = AngleDelta(first, previous);
            Assert.That(netDisplacement, Is.GreaterThan(0.1f),
                "The facing did not move during the look window; smoothness cannot be judged.");
            // A stuttered facing pops back and forth, so its travelled path far exceeds the net
            // displacement; a smooth facing only ever moves forward.
            Assert.That(path, Is.LessThanOrEqualTo(netDisplacement * 1.5f + 0.02f),
                $"Facing moved non-monotonically across jittered frames. pathDeg={path:F3} " +
                $"netDeg={netDisplacement:F3}");
            // This fixture's facing moves about 0.05 degrees per step; the grid-rescale bug popped
            // it by 0.2 degrees or more on single frames.
            Assert.That(steps.Max(), Is.LessThan(0.15f),
                $"Facing popped on a jittered frame. degrees per frame: " +
                string.Join(", ", steps.Select(s => s.ToString("F3"))));
        }

        // Quaternion.Angle cannot resolve the sub-0.05-degree steps the facing takes between
        // frames; wrap-minimal euler components retain that resolution away from singularities.
        private static float AngleDelta(Quaternion a, Quaternion b)
        {
            var d = b.eulerAngles - a.eulerAngles;
            return new Vector3(
                Mathf.DeltaAngle(0f, d.x),
                Mathf.DeltaAngle(0f, d.y),
                Mathf.DeltaAngle(0f, d.z)).magnitude;
        }

        private IEnumerator LoadNoteAndEnterPlaying()
        {
            Settings.Instance.Animations = true;
            // Mirrors the duplicated rotating notes near beat 251.5 in the reported map: an
            // animated world/local rotation sweeping through the long jump window is what makes
            // the look-history replay sensitive to the step grid it is evaluated on.
            yield return TestUtils.ReloadMap(2, JSON.Parse(@"{
                ""_version"":""2.2.0"",
                ""_notes"":[{""_time"":48,""_lineIndex"":0,""_lineLayer"":0,""_type"":1,
                    ""_cutDirection"":5,""_customData"":{
                        ""_noteJumpStartBeatOffset"":24,""_noteJumpMovementSpeed"":12,
                        ""_disableNoteGravity"":true,""_fake"":true,""_interactable"":false,
                        ""_animation"":{
                            ""_rotation"":[[-17,9,-10,0],[0,0,0,0.5]],
                            ""_localRotation"":[[-6,14,-40,0],[24,-38,189,0.25],[0,0,0,0.5]],
                            ""_dissolve"":[[0,0],[0.2,0.25],[0,0.475]],
                            ""_dissolveArrow"":[[0,0],[0.45,0.25],[0,0.475]]
                        }}}]
            }"), beatsPerMinute: 120f, environmentName: "BillieEnvironment");

            atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            Object.FindAnyObjectByType<UIMode>().SetUIMode(UIModeType.Playing, false);
            Object.FindAnyObjectByType<CameraManager>().SelectCamera(CameraType.Playing);

            collection = BeatmapObjectContainerCollection
                .GetCollectionForType<NoteGridContainer>(ObjectType.Note);
            data = collection.MapObjects.Single();
            var startBeat = data.SpawnSongBpmTime + data.HalfJumpDuration * 2f * 0.4f;
            atsc.MoveToSongBpmTime(startBeat);
            yield return null;
            Assert.That(collection.LoadedContainers.TryGetValue(data, out var loaded), Is.True,
                "Fixture note did not spawn inside its look window.");
            note = (NoteContainer)loaded;

            BeginDeterministicPlayback(startBeat);
            renderedProbe = new GameObject("RenderedFacingProbe").AddComponent<RenderedFacingProbe>();
            renderedProbe.Source = () => FacingRotation;
            yield return null;
            Assert.That(note.Animator != null && note.Animator.enabled && note.gameObject.activeInHierarchy
                && atsc.IsPlaying && UIMode.PreviewMode, Is.True,
                "Fixture note is not driven by an active animator in playing mode.");
        }

        private const float HistoryStepSeconds = 1f / 60f;
        private const float WarmupSeconds =
            (ObjectAnimator.NoteLookHistoryFrames + 2) * HistoryStepSeconds;

        // Native facing depends on the real sequence of recent frames, so each run rebuilds the
        // same uniform history behind the probe and varies only the last arriving step.
        private IEnumerator SampleFacingAfterUniformHistory(float targetSeconds,
            float lastStepSeconds, List<Quaternion> rotations)
        {
            for (var k = ObjectAnimator.NoteLookHistoryFrames + 2; k >= 0; k--)
            {
                playbackSeconds.SetValue(atsc,
                    targetSeconds - lastStepSeconds - k * HistoryStepSeconds);
                yield return null;
            }
            playbackSeconds.SetValue(atsc, targetSeconds);
            yield return null;
            rotations.Add(renderedProbe.Rotation);
        }

        // Keep production playback callbacks active while using a clock independent of native audio.
        private void BeginDeterministicPlayback(float songBpmTime)
        {
            atsc.MoveToSongBpmTime(songBpmTime);
            if (!atsc.IsPlaying)
            {
                atsc.TogglePlaying();
            }
            atsc.SongAudioSource.Stop();
            atsc.StopScheduled = true;
            // The test owns the clock while production playback state and time callbacks stay active.
            // Unity's frame clock would advance beyond the requested sample during these yields.
            atsc.enabled = false;
            playbackSeconds.SetValue(atsc, atsc.GetSecondsFromBeat(songBpmTime));
        }

        // DirectionTarget.localRotation is written by Track.UpdateTime during Update and again by
        // the note-look pass during LateUpdate. A null yield resumes between those phases, so the
        // pose the frame actually renders is captured here after every animation LateUpdate ran.
        [DefaultExecutionOrder(1000)]
        private sealed class RenderedFacingProbe : MonoBehaviour
        {
            public Func<Quaternion> Source;
            public Quaternion Rotation;

            private void LateUpdate()
            {
                if (Source != null)
                    Rotation = Source();
            }
        }

        [UnityTearDown]
        public IEnumerator StopPlayback()
        {
            if (renderedProbe != null)
            {
                Object.Destroy(renderedProbe.gameObject);
                renderedProbe = null;
            }
            if (atsc != null)
            {
                if (atsc.IsPlaying)
                {
                    TestUtils.PauseDeterministicPlayback(atsc);
                }
                atsc.enabled = true;
            }
            yield break;
        }
    }
}
