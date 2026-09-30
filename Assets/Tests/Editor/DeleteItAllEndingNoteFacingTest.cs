using System.Collections;
using System.Collections.Generic;
using Beatmap.Enums;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SimpleJSON;
using Tests.Infrastructure;
using UnityEngine;
using UnityEngine.TestTools;

namespace Tests.Editor
{
    [Explicit]
    public class DeleteItAllEndingNoteFacingTest : TestBase
    {
        private bool animationsBeforeTest;
        private UIMode uiMode;
        private CameraManager cameraManager;
        private float previousPlayerCameraFOV;
        private float previousPlayerCameraOffsetZ;
        private float previousCameraFOV;
        private Vector3 previousEditingCameraPosition;
        private Quaternion previousEditingCameraRotation;
        private bool editingCameraMoved;

        private static readonly System.Reflection.PropertyInfo currentSecondsProperty =
            typeof(AudioTimeSyncController).GetProperty(nameof(AudioTimeSyncController.CurrentSeconds));
        private static readonly int cutoutId = Shader.PropertyToID("_Cutout");

        private const string SourceMapPath =
            "C:/Users/tdrak/BSManager/BSInstances/1.44.1/Beat Saber_Data/CustomLevels/4cf62 (DELETE IT ALL - Mawntee)/ExpertPlusStandard.dat";

        protected override IEnumerator OnMapLoaded()
        {
            animationsBeforeTest = Settings.Instance.Animations;
            Settings.Instance.Animations = true;
            previousPlayerCameraFOV = Settings.Instance.PlayerCameraFOV;
            previousPlayerCameraOffsetZ = Settings.Instance.PlayerCameraOffsetZ;
            previousCameraFOV = Settings.Instance.CameraFOV;
            Settings.Instance.PlayerCameraFOV = 90f;
            Settings.Instance.PlayerCameraOffsetZ = 0f;
            Settings.Instance.CameraFOV = 90f;
            yield return TestUtils.ReloadMap(
                3,
                JSON.Parse(File.ReadAllText(SourceMapPath)),
                beatsPerMinute: 155,
                environmentName: "InterscopeEnvironment",
                songLengthSeconds: 250);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
            uiMode = Object.FindAnyObjectByType<UIMode>();
            cameraManager = Object.FindAnyObjectByType<CameraManager>();
        }

        protected override void CleanupTestObjects()
        {
        }

        [UnityTest]
        public IEnumerator FinalBenjaminDotFacesAwayFromPlayingCameraDuringJump()
        {
            uiMode.SetUIMode(UIModeType.Playing, false);
            cameraManager.SelectCamera(CameraType.Playing);
            var camera = cameraManager.CameraControllers[1].Camera;

            yield return SeekTo(460f);
            var note = FindFakeDotNote("benjamin", 469f);
            Assert.That(note, Is.Not.Null,
                "No loaded fake blue dot on track 'benjamin' at b469. " + DumpFakeNoteInventory());

            var failures = new List<string>();
            foreach (var beat in new[] { 460f, 468.5f, 460f })
            {
                yield return SeekTo(beat);
                if (!note.gameObject.activeInHierarchy)
                {
                    failures.Add($"beat {beat}: benjamin note container inactive.");
                    continue;
                }

                var renderers = note.ModelController.MpbController.Renderers;
                var activeRenderers = renderers != null
                    ? renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                    : 0;
                var dotActive = note.DotModelController != null
                    && note.DotModelController.gameObject.activeInHierarchy;
                if (activeRenderers == 0 || !dotActive)
                {
                    failures.Add($"beat {beat}: benjamin dot not rendered " +
                        $"(activeRenderers={activeRenderers} dotActive={dotActive}).");
                    continue;
                }

                if (note.Animator != null) note.Animator.LateUpdate();
                var front = note.DirectionTarget != null
                    ? note.DirectionTarget
                    : note.ModelController.transform;
                var notePos = front.position;
                var toNote = (notePos - camera.transform.position).normalized;
                var forward = front.forward;
                var alignment = Vector3.Dot(toNote, forward);
                Debug.Log($"[BenjaminDiag] beat {beat}: notePos={notePos} camPos={camera.transform.position} " +
                    $"toNote={toNote} forward={forward} alignment={alignment:F3} " +
                    $"containerLocalPos={note.transform.localPosition} " +
                    $"parentRot={(note.transform.parent != null ? note.transform.parent.rotation.ToString() : "<none>")} " +
                    $"dotActive={dotActive} activeRenderers={activeRenderers}");
                if (alignment <= 0.9f)
                {
                    failures.Add($"beat {beat}: benjamin dot front does not point away from the playing " +
                        $"camera along the camera->note ray: alignment={alignment:F3} " +
                        $"notePos={notePos} camPos={camera.transform.position} " +
                        $"toNote={toNote} forward={forward}.");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [UnityTest]
        public IEnumerator PausedAndResumedPlaybackKeepsRenderedNoteSet()
        {
            uiMode.SetUIMode(UIModeType.Preview, false);
            cameraManager.SelectCamera(CameraType.Editing);
            var editingCam = cameraManager.CameraControllers[0].Camera;
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            previousEditingCameraPosition = editingCam.transform.position;
            previousEditingCameraRotation = editingCam.transform.rotation;
            editingCameraMoved = true;
            editingCam.transform.SetPositionAndRotation(
                new Vector3(0f, 1.65f, 0f), Quaternion.LookRotation(Vector3.forward));

            var failures = new List<string>();
            try
            {
                atsc.MoveToJsonTime(427f);
                atsc.TogglePlaying();
                atsc.SongAudioSource.Stop();
                atsc.StopScheduled = true;
                var songBeat427 = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(427f);
                currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat427));
                yield return null;
                yield return null;
                if (!atsc.IsPlaying) failures.Add("setup: deterministic playback did not start.");
                FinalizeNotePoses(noteGrid);
                var playing = CaptureRenderedNotes(noteGrid, editingCam, "playing@427");

                atsc.TogglePlaying();
                var pausedBeat = atsc.CurrentJsonTime;
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var paused = CaptureRenderedNotes(noteGrid, editingCam, $"paused@{pausedBeat:F3}");

                var resurrected = paused.Keys
                    .Where(k => !playing.ContainsKey(k) && k is Beatmap.Base.BaseNote n
                        && n.JsonTime < pausedBeat - 1f)
                    .ToList();

                atsc.TogglePlaying();
                var pausedSongBeat =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(pausedBeat);
                currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(pausedSongBeat));
                for (var frame = 0; frame < 6; ++frame)
                {
                    yield return null;
                    var frameSnap = CaptureRenderedNotes(noteGrid, editingCam, $"resume+{frame}f");
                    var tracked = resurrected
                        .Where(frameSnap.ContainsKey)
                        .Take(6)
                        .Select(k => $"{Describe(k)} pos={frameSnap[k].pos}")
                        .ToList();
                    Debug.Log($"[PauseDiag] resume+{frame}f: loaded={frameSnap.Count} " +
                        $"resurrectedStillPresent={resurrected.Count(frameSnap.ContainsKey)}" +
                        (tracked.Count > 0 ? " | " + string.Join(" | ", tracked) : ""));
                }
                FinalizeNotePoses(noteGrid);
                var resumed = CaptureRenderedNotes(noteGrid, editingCam, $"resumed@{pausedBeat:F3}");

                var stepped = new Dictionary<float, Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot>>();
                foreach (var stepBeat in new[] { pausedBeat + 1f, pausedBeat + 2f })
                {
                    var songBeat =
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(stepBeat);
                    currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
                    yield return null;
                    yield return null;
                    FinalizeNotePoses(noteGrid);
                    stepped[stepBeat] =
                        CaptureRenderedNotes(noteGrid, editingCam, $"resumed@{stepBeat:F3}");
                }

                atsc.TogglePlaying();
                yield return null;
                yield return null;
                atsc.MoveToJsonTime(455f);
                var pausedSeekBeat = atsc.CurrentJsonTime;
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var pausedSeek455 =
                    CaptureRenderedNotes(noteGrid, editingCam, $"pausedSeek@{pausedSeekBeat:F3}");

                atsc.TogglePlaying();
                var songBeat455 =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(pausedSeekBeat);
                currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat455));
                yield return null;
                yield return null;
                FinalizeNotePoses(noteGrid);
                var playing455 =
                    CaptureRenderedNotes(noteGrid, editingCam, $"resumed@{pausedSeekBeat:F3}");

                var benjamin = playing.Keys.FirstOrDefault(k =>
                    k is Beatmap.Base.BaseNote bn && bn.CustomFake
                        && bn.CustomTrack is JSONString s && s.Value == "benjamin");
                if (benjamin == null)
                {
                    failures.Add("playing@427: benjamin fake dot was not rendered before pause.");
                }
                else
                {
                    if (!paused.TryGetValue(benjamin, out var pausedBenjamin) || !pausedBenjamin.rendered)
                        failures.Add($"paused@{pausedBeat:F3}: benjamin fake dot disappeared while paused " +
                            $"(container recycled by pause RefreshPool).");
                    if (!resumed.TryGetValue(benjamin, out var resumedBenjamin) || !resumedBenjamin.rendered)
                        failures.Add($"resumed@{pausedBeat:F3}: benjamin fake dot stayed gone after resume.");
                    if (!pausedSeek455.TryGetValue(benjamin, out var seekBenjamin) || !seekBenjamin.rendered)
                        failures.Add($"pausedSeek@{pausedSeekBeat:F3}: benjamin fake dot " +
                            "disappeared on stopped seek while paused.");
                }

                foreach (var key in pausedSeek455.Keys.Union(playing455.Keys))
                {
                    var hasSeek = pausedSeek455.TryGetValue(key, out var s);
                    var hasPlay = playing455.TryGetValue(key, out var q);
                    if (hasSeek != hasPlay
                        || (hasSeek && hasPlay
                            && (s.rendered != q.rendered
                                || (s.rendered && Vector3.Distance(s.pos, q.pos) > 0.05f))))
                        failures.Add($"b455 pausedSeek vs resumed: '{Describe(key)}' differs " +
                            $"(present {hasSeek}->{hasPlay} rendered " +
                            $"{(hasSeek ? s.rendered : false)}->{(hasPlay ? q.rendered : false)} " +
                            $"pos {(hasSeek ? s.pos : Vector3.zero)}->{(hasPlay ? q.pos : Vector3.zero)}).");
                }

                if (resurrected.Count > 0)
                {
                    var sample = string.Join("; ", resurrected.Take(5).Select(k =>
                        $"{Describe(k)} pausedPos={paused[k].pos}"));
                    failures.Add($"paused@{pausedBeat:F3}: {resurrected.Count} already-passed notes " +
                        $"resurrected into Preview while paused, e.g. {sample}");
                }

                foreach (var key in paused.Keys.Intersect(resumed.Keys))
                {
                    var p = paused[key];
                    var r = resumed[key];
                    var delta = Vector3.Distance(p.pos, r.pos);
                    if (p.rendered != r.rendered || delta > 0.05f)
                        failures.Add($"beat {pausedBeat:F3}: '{Describe(key)}' pose differs paused vs resumed " +
                            $"(rendered {p.rendered}->{r.rendered} pos {p.pos}->{r.pos} delta={delta:F3}).");
                }
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        // User report: repeated back/forth scrubs in Preview or Playing leave different notes
        // on screen than continuous playback, with wrong positions anywhere in the map. Drive
        // deterministic production playback (audio stopped, CurrentSeconds stepped <=0.5 beat per
        // rendered frame) up to each section beat, sample the ACTUAL CurrentJsonTime, then compare
        // identity + renderer pose against 3 stopped reverse/forward scrub cycles to that exact
        // beat and the first frame after resume. No manual Animator.LateUpdate and no private
        // clock reset without a following production frame, so pose diffs are behavioral.
        [UnityTest]
        public IEnumerator MatchedTimeScrubParityAcrossSections()
        {
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            var noteGrid = Object.FindAnyObjectByType<NoteGridContainer>();
            Assert.That(atsc, Is.Not.Null, "AudioTimeSyncController missing.");
            Assert.That(noteGrid, Is.Not.Null, "NoteGridContainer missing.");
            Assert.That(currentSecondsProperty, Is.Not.Null, "CurrentSeconds property not found.");

            // Batchmode has no input devices; UIMode.OnPlayToggle -> SetLockState reads
            // Mouse.current when toggling playback in Playing mode.
            var testMouse = UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();

            var failures = new List<string>();
            var sections = new[] { 64f, 128f, 256f, 360f, 420.5f, 425f, 427f, 469f };
            var visibleSections = 0;
            try
            {
                foreach (var playing in new[] { true, false })
                {
                    var modeName = playing ? "Playing" : "Preview";
                    if (playing)
                    {
                        uiMode.SetUIMode(UIModeType.Playing, false);
                        cameraManager.SelectCamera(CameraType.Playing);
                    }
                    else
                    {
                        uiMode.SetUIMode(UIModeType.Preview, false);
                        cameraManager.SelectCamera(CameraType.Editing);
                        var editingCam = cameraManager.CameraControllers[0].Camera;
                        if (!editingCameraMoved)
                        {
                            previousEditingCameraPosition = editingCam.transform.position;
                            previousEditingCameraRotation = editingCam.transform.rotation;
                            editingCameraMoved = true;
                        }
                        editingCam.transform.SetPositionAndRotation(
                            new Vector3(0f, 1.65f, 0f), Quaternion.LookRotation(Vector3.forward));
                    }
                    var cam = cameraManager.CameraControllers[playing ? 1 : 0].Camera;
                    yield return null;

                    foreach (var section in sections)
                    {
                        // Deterministic playback from 4 beats back in <=0.5-beat rendered steps.
                        var start = Mathf.Max(0f, section - 4f);
                        atsc.MoveToJsonTime(start);
                        atsc.TogglePlaying();
                        atsc.SongAudioSource.Stop();
                        atsc.StopScheduled = true;
                        yield return null; yield return null;
                        if (!atsc.IsPlaying)
                        {
                            failures.Add($"{modeName} section {section}: playback did not start.");
                            atsc.StopScheduled = false;
                            continue;
                        }
                        for (var t = start + 0.5f; t <= section && atsc.CurrentJsonTime < section - 0.001f; t += 0.5f)
                        {
                            var songBeat = (float)BeatSaberSongContainer.Instance.Map
                                .JsonTimeToSongBpmTime(Mathf.Min(t, section));
                            currentSecondsProperty.SetValue(atsc, atsc.GetSecondsFromBeat(songBeat));
                            yield return null; yield return null;
                        }
                        var sampled = atsc.CurrentJsonTime;
                        var playback = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} playback@{sampled:F3}");
                        var nearKeys = playback.Keys
                            .Where(k => Mathf.Abs(k.JsonTime - sampled) <= 4f).ToList();
                        var visiblePlayback = playback.Count(p => p.Value.visible);
                        Debug.Log($"[ScrubParity] {modeName} section {section}: sampled={sampled:F3} " +
                            $"loaded={playback.Count} visible={visiblePlayback} " +
                            $"nearIds=[{string.Join("; ", nearKeys.Take(8).Select(Describe))}]");
                        if (Mathf.Approximately(section, 64f))
                            DumpSpawnWindowDiagnostics(noteGrid, atsc,
                                $"{modeName} playback@{sampled:F3}", 62f, 74f);
                        if (Mathf.Approximately(section, 425f))
                            DumpB427WindowDiagnostics(noteGrid, atsc,
                                $"{modeName} playback@{sampled:F3}");
                        atsc.TogglePlaying();
                        yield return null;
                        if (visiblePlayback == 0)
                        {
                            Debug.Log($"[ScrubParity] {modeName} section {section}: no visually " +
                                "contributing note during playback - informational only.");
                            continue;
                        }
                        visibleSections++;

                        for (var cycle = 0; cycle < 3; ++cycle)
                        {
                            atsc.MoveToJsonTime(Mathf.Max(0f, sampled - 6f));
                            yield return null;
                            atsc.MoveToJsonTime(sampled + 3f);
                            yield return null;
                            atsc.MoveToJsonTime(sampled);
                            yield return null; yield return null;
                            var scrubbed = CaptureRenderedNotes(noteGrid, cam,
                                $"{modeName} scrub-cycle{cycle}@{sampled:F3}");
                            if (cycle == 0 && Mathf.Approximately(section, 64f))
                                DumpSpawnWindowDiagnostics(noteGrid, atsc,
                                    $"{modeName} firstSeek@{sampled:F3}", 62f, 74f);
                            if (Mathf.Approximately(section, 425f))
                                DumpB427WindowDiagnostics(noteGrid, atsc,
                                    $"{modeName} scrub-cycle{cycle}@{sampled:F3}");
                            CompareNoteSnapshots(playback, scrubbed, sampled,
                                $"{modeName} scrub-cycle{cycle}@{sampled:F3}", failures);

                            // A/B pixel proof on the Playing camera at the frozen seek state:
                            // the b64.5 oneHand note (body renderers enabled, cutout < .98) must
                            // actually rasterize into the view, not just pass snapshot parity.
                            if (cycle == 0 && playing && Mathf.Approximately(section, 64f))
                            {
                                var target = noteGrid.LoadedContainers.Values
                                    .OfType<Beatmap.Containers.NoteContainer>()
                                    .FirstOrDefault(c => c.NoteData != null
                                        && Mathf.Approximately(c.NoteData.JsonTime, 64.5f)
                                        && c.NoteData.CustomTrack is JSONString s
                                        && s.Value == "oneHand");
                                var mpb = target != null && target.MpbController != null
                                    ? target.MpbController.Mpb
                                    : null;
                                var cutout = mpb != null ? mpb.GetFloat(cutoutId) : float.NaN;
                                var group = target == null
                                    ? new List<Renderer>()
                                    : (target.ModelController.MpbController.Renderers
                                        ?? Enumerable.Empty<Renderer>())
                                        .Where(r => r != null && r.enabled
                                            && r.gameObject.activeInHierarchy)
                                        .ToList();
                                if (group.Count == 0 || !(cutout < 0.98f))
                                {
                                    failures.Add($"{modeName} A/B @{sampled:F3}: b64.5 oneHand body " +
                                        $"renderers unavailable for pixel proof (group={group.Count} " +
                                        $"cutout={cutout:F2}).");
                                }
                                else
                                {
                                    var delta = CaptureGroupDelta(cam, group);
                                    Debug.Log($"=== A/B pixel proof: {modeName} b{sampled:F3} oneHand " +
                                        $"maxDiff={delta.MaxDiff} pixels={delta.Count} ===");
                                    if (delta.Count < 10)
                                        failures.Add($"{modeName} A/B @{sampled:F3}: b64.5 oneHand " +
                                            $"changed {delta.Count} pixels (maxDiff={delta.MaxDiff}), " +
                                            "expected >=10.");
                                }
                            }
                        }

                        // Resume applies an audio-latency offset to CurrentSeconds, so compare the
                        // resumed frame against a stopped seek to the ACTUAL resumed beat.
                        atsc.TogglePlaying();
                        yield return null; yield return null;
                        var resumedBeat = atsc.CurrentJsonTime;
                        var resumed = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} resumed@{resumedBeat:F3}");
                        atsc.TogglePlaying();
                        yield return null;
                        atsc.MoveToJsonTime(resumedBeat);
                        yield return null; yield return null;
                        var seekRef = CaptureRenderedNotes(noteGrid, cam,
                            $"{modeName} seekRef@{resumedBeat:F3}");
                        CompareNoteSnapshots(seekRef, resumed, resumedBeat,
                            $"{modeName} resumed@{resumedBeat:F3} vs seek", failures);
                    }
                }
                if (visibleSections < 3)
                    failures.Add($"only {visibleSections} sections had a visible note during " +
                        "playback - too few to be a meaningful parity check.");
            }
            finally
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
                UnityEngine.InputSystem.InputSystem.RemoveDevice(testMouse);
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        private static void CompareNoteSnapshots(
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> expected,
            Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> actual,
            float sampledBeat,
            string phase,
            List<string> failures)
        {
            foreach (var key in expected.Keys.Union(actual.Keys))
            {
                var hasE = expected.TryGetValue(key, out var e);
                var hasA = actual.TryGetValue(key, out var a);
                // Only notes that were actually visually contributing (enabled renderer body
                // intersecting the camera frustum, cutout < 0.98) count - offscreen preload
                // membership differences between playback and seek histories are not defects.
                var matteredE = hasE && e.visible;
                var matteredA = hasA && a.visible;
                if (!matteredE && !matteredA) continue;
                if (matteredE != matteredA)
                {
                    failures.Add($"{phase}: '{Describe(key)}' visible differs " +
                        $"(visible {matteredE}->{matteredA} loaded {hasE}->{hasA} " +
                        $"pos {(hasE ? e.pos : Vector3.zero)}->{(hasA ? a.pos : Vector3.zero)}).");
                    continue;
                }
                if (e.rendered != a.rendered)
                {
                    failures.Add($"{phase}: '{Describe(key)}' rendered differs " +
                        $"({e.rendered}->{a.rendered}).");
                    continue;
                }
                var dPos = Vector3.Distance(e.pos, a.pos);
                var dRot = Quaternion.Angle(e.rot, a.rot);
                var dCut = Mathf.Abs(e.cutout - a.cutout);
                if (dPos > 0.05f || dRot > 5f || (float.IsFinite(dCut) && dCut > 0.05f))
                    failures.Add($"{phase}: '{Describe(key)}' pose differs " +
                        $"(pos {e.pos}->{a.pos} dPos={dPos:F3} rotAngle={dRot:F1} " +
                        $"cutout {e.cutout:F2}->{a.cutout:F2}).");
            }
        }

        // Replicates the private PausedGameplayNoteFilter inputs data-only (never invokes it) to
        // explain which spawn-window term admits or drops a note after a stopped seek, plus the
        // MapObjects ordering/index of each authored candidate.
        private static void DumpSpawnWindowDiagnostics(
            NoteGridContainer grid, AudioTimeSyncController atsc, string phase,
            float fromBeat, float toBeat)
        {
            var time = atsc.CurrentSongBpmTime;
            var spawnOffset = grid.SpawnCallbackController.Offset;
            var despawnOffset = grid.DespawnCallbackController.Offset;
            var despawnFloor = time + despawnOffset;
            var spawnCap = time + spawnOffset;
            var animLookahead = UIMode.AnimationMode;

            var walkStart = grid.MapObjects.FindIndex(o => o.SongBpmTime >= spawnCap);
            var spawnFloor = walkStart >= 0 && walkStart < grid.MapObjects.Count
                ? grid.MapObjects[walkStart].JsonTime
                : float.PositiveInfinity;

            var window = new List<string>();
            var lo = Mathf.Max(0, walkStart - 3);
            var hi = Mathf.Min(grid.MapObjects.Count - 1, walkStart + 5);
            for (var i = lo; i <= hi; ++i)
            {
                var o = grid.MapObjects[i];
                window.Add($"[{i}] b{o.JsonTime:F3} bpm={o.SongBpmTime:F3} {Describe(o)}");
            }

            var candidates = BeatSaberSongContainer.Instance.Map.Notes
                .Where(n => n.JsonTime >= fromBeat && n.JsonTime <= toBeat)
                .OrderBy(n => n.JsonTime)
                .ToList();
            var lines = new List<string>();
            foreach (var n in candidates)
            {
                var idx = grid.MapObjects.IndexOf(n);
                var loaded = grid.LoadedContainers.TryGetValue(n, out var con);
                var active = loaded && con.gameObject.activeInHierarchy;
                var renderers = loaded && con is Beatmap.Containers.NoteContainer nc
                    ? nc.ModelController.MpbController.Renderers?.Count(r => r != null && r.enabled) ?? 0
                    : -1;
                var inCap = n.SongBpmTime >= despawnFloor && n.SongBpmTime <= spawnCap;
                var pastFloor = n.JsonTime < spawnFloor;
                var lookahead = animLookahead
                    ? Mathf.Max(n.HalfJumpDuration, spawnOffset) + Track.JUMP_TIME
                    : spawnOffset;
                var inLookahead = n.SongBpmTime <= time + lookahead;
                var includes = inCap || (!pastFloor && inLookahead);
                lines.Add($"  {Describe(n)} idx={idx} bpm={n.SongBpmTime:F3} hjd={n.HalfJumpDuration:F3} " +
                    $"loaded={loaded} active={active} rend={renderers} " +
                    $"inCap={inCap} pastFloor={pastFloor} look={lookahead:F2} inLook={inLookahead} " +
                    $"=> includes={includes}");
            }
            Debug.Log($"[SpawnDiag] {phase}: songBpm={time:F3} spawnOff={spawnOffset:F3} " +
                $"despawnOff={despawnOffset:F3} spawnCap={spawnCap:F3} despawnFloor={despawnFloor:F3} " +
                $"walkStartIdx={walkStart} spawnFloor={spawnFloor:F3} animLookahead={animLookahead} " +
                $"isPlaying={atsc.IsPlaying}\n  MapObjects near walkStart:\n    {string.Join("\n    ", window)}\n" +
                string.Join("\n", lines));
        }

        // For each loaded container keyed to a b427-window note, dump identity (map key vs
        // container data), animator state, aggregator counts (never Get()), transform lineage and
        // body bounds - to tell data-identity desync from animation/track rewrites.
        private static void DumpB427WindowDiagnostics(
            NoteGridContainer grid, AudioTimeSyncController atsc, string phase)
        {
            var lines = new List<string>();
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                var data = note.NoteData;
                if (data.JsonTime < 426.5f || data.JsonTime > 427.5f) continue;
                var anim = note.Animator;
                var front = note.DirectionTarget != null ? note.DirectionTarget : note.transform;
                var body = note.ModelController.MpbController.Renderers?
                    .FirstOrDefault(r => r != null && r.enabled);
                var lineage = new List<string>();
                for (var t = note.transform; t != null && lineage.Count < 8; t = t.parent)
                    lineage.Add($"{t.name}@local{t.localPosition} world{t.position} " +
                        $"lRot{t.localEulerAngles} wRot{t.eulerAngles}");
                var trackInfo = "<none>";
                if (anim != null && anim.AnimationTrack != null)
                {
                    var tr = anim.AnimationTrack;
                    var rotAt = anim.TracksManager != null
                        ? anim.TracksManager.GetRotationAtTime(data.SongBpmTime)
                        : float.NaN;
                    trackInfo = $"trackName={tr.gameObject.name} rotValue={tr.RotationValue} " +
                        $"selfLRot={(tr.SelfTransform != null ? tr.SelfTransform.localEulerAngles.ToString() : "<null>")} " +
                        $"selfWRot={(tr.SelfTransform != null ? tr.SelfTransform.eulerAngles.ToString() : "<null>")} " +
                        $"parentRot={(tr.ObjectParentTransform != null ? tr.ObjectParentTransform.eulerAngles.ToString() : "<null>")} " +
                        $"rotAtBpm={rotAt:F1}";
                }
                var animType = anim != null ? anim.TargetType.ToString() : "<null>";
                var animActive = anim != null && anim.isActiveAndEnabled;
                var animLife = anim != null && anim.AnimatedLife;
                var animTrack = anim != null && anim.AnimatedTrack;
                var worldN = anim != null ? anim.WorldPosition.Count : -1;
                var offsetN = anim != null ? anim.OffsetPosition.Count : -1;
                var localN = anim != null ? anim.LocalPosition.Count : -1;
                lines.Add($"  {Describe(pair.Key)} keyIsNoteData={ReferenceEquals(pair.Key, data)} " +
                    $"keyIsObjectData={ReferenceEquals(pair.Key, note.ObjectData)} " +
                    $"animType={animType} animActive={animActive} " +
                    $"animLife={animLife} animTrack={animTrack} " +
                    $"worldN={worldN} offsetN={offsetN} " +
                    $"localN={localN} front={front.position} " +
                    $"bodyBounds={(body != null ? body.bounds.ToString() : "<none>")} " +
                    $"active={note.gameObject.activeInHierarchy} authoredRot={data.Rotation} " +
                    $"customWorldRot={data.CustomWorldRotation} {trackInfo} " +
                    $"lineage={string.Join(" <- ", lineage)}");
            }
            Debug.Log($"[B427Diag] {phase}: beat={atsc.CurrentJsonTime:F3} " +
                $"songBpm={atsc.CurrentSongBpmTime:F3} isPlaying={atsc.IsPlaying} " +
                $"loadedB427={lines.Count}\n" + string.Join("\n", lines));
        }

        private sealed class PauseNoteSnapshot
        {
            public bool active;
            public bool rendered;
            public Vector3 pos;
            public Vector3 forward;
            public Quaternion rot;
            public int activeRenderers;
            public bool dotActive;
            public bool inView;
            public bool boundsInFrustum;
            public bool visible;
            public float cutout;
        }

        private static void FinalizeNotePoses(NoteGridContainer grid)
        {
            foreach (var container in grid.LoadedContainers.Values)
            {
                if (container is Beatmap.Containers.NoteContainer note
                    && note.gameObject.activeInHierarchy
                    && note.Animator != null
                    && note.Animator.isActiveAndEnabled)
                {
                    note.Animator.LateUpdate();
                }
            }
        }

        private static Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot> CaptureRenderedNotes(
            NoteGridContainer grid, Camera cam, string phase)
        {
            var snapshot = new Dictionary<Beatmap.Base.BaseObject, PauseNoteSnapshot>();
            var interesting = new List<string>();
            var frustum = GeometryUtility.CalculateFrustumPlanes(cam);
            foreach (var pair in grid.LoadedContainers)
            {
                if (pair.Value is not Beatmap.Containers.NoteContainer note || note.NoteData == null)
                    continue;
                var front = note.DirectionTarget != null ? note.DirectionTarget : note.transform;
                var pos = front.position;
                var renderers = note.ModelController.MpbController.Renderers;
                var activeRenderers = renderers != null
                    ? renderers.Count(r => r != null && r.enabled && r.gameObject.activeInHierarchy)
                    : 0;
                // A note only counts as visually contributing when an enabled body renderer's
                // bounds intersect the active camera frustum - loaded offscreen preload history
                // is not a scrub defect.
                var bodyInFrustum = renderers != null && renderers.Any(r =>
                    r != null && r.enabled && r.gameObject.activeInHierarchy
                    && GeometryUtility.TestPlanesAABB(frustum, r.bounds));
                var dotActive = note.DotModelController != null
                    && note.DotModelController.gameObject.activeInHierarchy;
                var vp = cam.WorldToViewportPoint(pos);
                var inView = vp.z > 0f && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;
                var mpb = note.MpbController != null ? note.MpbController.Mpb : null;
                var cutout = mpb != null ? mpb.GetFloat(cutoutId) : float.NaN;
                var visible = note.gameObject.activeInHierarchy && activeRenderers > 0
                    && bodyInFrustum && (!float.IsFinite(cutout) || cutout < 0.98f);
                snapshot[pair.Key] = new PauseNoteSnapshot
                {
                    active = note.gameObject.activeInHierarchy,
                    rendered = note.gameObject.activeInHierarchy && activeRenderers > 0,
                    pos = pos,
                    forward = front.forward,
                    rot = front.rotation,
                    activeRenderers = activeRenderers,
                    dotActive = dotActive,
                    inView = inView,
                    boundsInFrustum = bodyInFrustum,
                    visible = visible,
                    cutout = cutout,
                };
                var data = note.NoteData;
                if (data.CustomFake || (data.JsonTime >= 420f && data.JsonTime <= 472f)
                    || (data.CustomTrack is JSONString oneHand && oneHand.Value == "oneHand"))
                {
                    interesting.Add($"{Describe(pair.Key)} active={note.gameObject.activeInHierarchy} " +
                        $"renderers={activeRenderers} dot={dotActive} pos={pos} fwd={front.forward} " +
                        $"cutout={cutout:F2} inView={inView} inFrustum={bodyInFrustum} visible={visible} " +
                        $"vp={vp} bounds={(renderers != null && renderers.Count > 0 ? renderers[0].bounds.ToString() : "<none>")}");
                }
            }
            Debug.Log($"[PauseDiag] {phase}: loadedNotes={snapshot.Count} rendered=" +
                $"{snapshot.Values.Count(s => s.rendered)}\n  " + string.Join("\n  ", interesting));
            return snapshot;
        }

        private static string Describe(Beatmap.Base.BaseObject obj)
        {
            if (obj is Beatmap.Base.BaseNote n)
            {
                var track = n.CustomTrack is JSONString s ? s.Value : "-";
                return $"b{n.JsonTime:F3} track={track} type={n.Type} d{n.CutDirection} " +
                    $"x{n.PosX}y{n.PosY} fake={n.CustomFake}";
            }
            return $"b{obj.JsonTime:F3} {obj.GetType().Name}";
        }

        private static Beatmap.Containers.NoteContainer FindFakeDotNote(string track, float beat)
        {
            return Object.FindAnyObjectByType<NoteGridContainer>()
                .LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>()
                .Where(c => c.NoteData != null
                    && c.NoteData.CustomFake
                    && c.NoteData.CustomTrack is JSONString t && t.Value == track
                    && c.NoteData.CutDirection == (int)NoteCutDirection.Any
                    && Mathf.Abs(c.NoteData.JsonTime - beat) < 0.25f)
                .OrderBy(c => Mathf.Abs(c.NoteData.JsonTime - beat))
                .FirstOrDefault();
        }

        private static string DumpFakeNoteInventory()
        {
            var grid = Object.FindAnyObjectByType<NoteGridContainer>();
            var loaded = grid.LoadedContainers.Values
                .OfType<Beatmap.Containers.NoteContainer>()
                .Where(c => c.NoteData != null)
                .ToList();
            var fakes = loaded.Where(c => c.NoteData.CustomFake).ToList();
            var near = loaded.Where(c => c.NoteData.JsonTime > 440f)
                .OrderBy(c => c.NoteData.JsonTime)
                .Select(c => $"b{c.NoteData.JsonTime} t{(c.NoteData.CustomTrack is JSONString s ? s.Value : c.NoteData.CustomTrack?.ToString() ?? "<null>")} d{c.NoteData.CutDirection} fake={c.NoteData.CustomFake}")
                .ToList();
            return $"loadedNotes={loaded.Count} loadedFakes={fakes.Count} " +
                $"fakeBeatRange={(fakes.Count > 0 ? fakes.Min(c => c.NoteData.JsonTime).ToString("F2") + ".." + fakes.Max(c => c.NoteData.JsonTime).ToString("F2") : "<none>")} " +
                $"notes>440=[{string.Join("; ", near)}]";
        }

        private static IEnumerator SeekTo(float beat)
        {
            Object.FindAnyObjectByType<AudioTimeSyncController>().MoveToJsonTime(beat);
            yield return null;
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator RestoreEditingMode()
        {
            if (uiMode != null)
                uiMode.SetUIMode(UIModeType.Normal, false);
            if (cameraManager != null)
            {
                cameraManager.SelectCamera(CameraType.Editing);
                if (editingCameraMoved)
                {
                    cameraManager.CameraControllers[0].Camera.transform.SetPositionAndRotation(
                        previousEditingCameraPosition, previousEditingCameraRotation);
                    editingCameraMoved = false;
                }
            }
            var atsc = Object.FindAnyObjectByType<AudioTimeSyncController>();
            if (atsc != null)
            {
                atsc.StopScheduled = false;
                if (atsc.IsPlaying) atsc.CancelPlaying();
            }
            Settings.Instance.Animations = animationsBeforeTest;
            Settings.Instance.PlayerCameraFOV = previousPlayerCameraFOV;
            Settings.Instance.PlayerCameraOffsetZ = previousPlayerCameraOffsetZ;
            Settings.Instance.CameraFOV = previousCameraFOV;
            yield break;
        }

        // Same A/B capture pattern as CensoredFullMapVisibilityTest.CaptureGroupDelta: render the
        // camera once with the group enabled and once with it disabled at an unchanged beat, then
        // count pixels whose max RGB channel delta reaches 8/255.
        private static (int MaxDiff, int Count) CaptureGroupDelta(
            Camera camera, List<Renderer> group)
        {
            var previousTarget = camera.targetTexture;
            var sceneTexture = new RenderTexture(1024, 512, 24, RenderTextureFormat.ARGB32);
            var withGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var withoutGroup = new Texture2D(1024, 512, TextureFormat.RGBA32, false);
            var original = group.Select(r => r.enabled).ToList();
            try
            {
                camera.targetTexture = sceneTexture;
                camera.Render();
                ReadRenderTexture(sceneTexture, withGroup);

                for (var i = 0; i < group.Count; i++) group[i].enabled = false;
                try
                {
                    camera.Render();
                    ReadRenderTexture(sceneTexture, withoutGroup);
                }
                finally
                {
                    for (var i = 0; i < group.Count; i++) group[i].enabled = original[i];
                }

                var maxDiff = 0;
                var count = 0;
                for (var y = 0; y < 512; y++)
                {
                    for (var x = 0; x < 1024; x++)
                    {
                        var a = withGroup.GetPixel(x, y);
                        var b = withoutGroup.GetPixel(x, y);
                        var diff = Mathf.RoundToInt(255f * Mathf.Max(
                            Mathf.Abs(a.r - b.r),
                            Mathf.Max(Mathf.Abs(a.g - b.g), Mathf.Abs(a.b - b.b))));
                        maxDiff = Mathf.Max(maxDiff, diff);
                        if (diff >= 8) count++;
                    }
                }
                return (maxDiff, count);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                Object.Destroy(sceneTexture);
                Object.Destroy(withGroup);
                Object.Destroy(withoutGroup);
            }
        }

        private static void ReadRenderTexture(RenderTexture source, Texture2D destination)
        {
            var previousActive = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                destination.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
                destination.Apply();
            }
            finally
            {
                RenderTexture.active = previousActive;
            }
        }

        [UnityOneTimeTearDown]
        public IEnumerator RestoreEmptySharedMap()
        {
            yield return TestUtils.ReloadMap(3, new JSONObject { ["version"] = "3.2.0" },
                forceSceneReload: true);
            TestUtils.CaptureCurrentMapAsSharedBaseline();
        }
    }
}
