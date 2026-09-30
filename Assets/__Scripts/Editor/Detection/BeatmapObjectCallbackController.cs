using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

//Name and idea totally not stolen directly from Beat Saber
public class BeatmapObjectCallbackController : MonoBehaviour
{
    private static readonly int eventsToLookAhead = 75;
    private static readonly int notesToLookAhead = 25;

    [FormerlySerializedAs("notesContainer")] [SerializeField]
    private NoteGridContainer noteGridContainer;

    [FormerlySerializedAs("eventsContainer")] [SerializeField]
    private EventGridContainer eventGridContainer;

    [SerializeField] private AudioTimeSyncController timeSyncController;
    [SerializeField] private VariableNJSProvider vNjsProvider;
    [SerializeField] private UIMode uiMode;

    [SerializeField] private bool useOffsetFromConfig = true;

    [Tooltip("Whether or not to use the Despawn or Spawn offset from settings.")] [SerializeField]
    private bool useDespawnOffset;

    [FormerlySerializedAs("offset")] public float Offset;

    [SerializeField] private int nextNoteIndex;
    [SerializeField] private int nextEventIndex;
    [SerializeField] private int nextChainIndex;

    [FormerlySerializedAs("useAudioTime")] public bool UseAudioTime;

    private float curTime;

    // DeleteItAllNoteLookParityTest.PausedPreviewKeepsGameplayNoteSet /
    // SaltyFullMapPlacementParityTest.EarlyFakePairsUseNoodleLaneDistance: the sequential spawn walk
    // stops at the first not-yet-due note, so a note whose own HalfJumpDuration makes it due earlier
    // (fake pairs with authored njs/offset overrides) can never spawn while playing. Future notes
    // with HalfJumpDuration > Offset are indexed once per play session by their own due beat and are
    // emitted ahead of the cursor; earlyEmittedNotes marks them by identity so the walk neither
    // blocks on nor double-emits them. Rebuilt on play start, mid-play insert/delete and Offset
    // changes — never scanned per frame.
    private readonly List<(float dueSongBpmTime, int mapIndex, BaseObject note)> pendingNoteSpawns =
        new();
    private readonly HashSet<BaseObject> earlyEmittedNotes = new();
    // Consumed entries stay in the list behind this cursor (reset on each rebuild) so due-entry
    // processing is O(N) total instead of RemoveRange shifting the dense prefix every frame.
    private int pendingNoteSpawnCursor;
    private float pendingNoteSpawnOffset = float.NaN;

    public event Action<bool, int, BaseObject> OnNotePassedThreshold;
    public event Action<bool, int> OnRecursiveNoteCheckFinished;
    public event Action<bool, int, BaseObject> OnEventPassedThreshold;
    public event Action<bool, int> OnRecursiveEventCheckFinished;
    public event Action<bool, int, BaseObject> OnChainPassedThreshold;
    public event Action<bool, int> OnRecursiveChainCheckFinished;

    /// v3 version fields
    [FormerlySerializedAs("chainsContainer")] [SerializeField]
    private ChainGridContainer chainGridContainer;

    private void Start()
    {
        noteGridContainer.OnObjectSpawned += NoteGridContainerOnObjectSpawned;
        noteGridContainer.OnObjectDeleted += NoteGridContainerOnObjectDeleted;
        eventGridContainer.OnObjectSpawned += GridContainerOnObjectSpawnedGrid;
        eventGridContainer.OnObjectDeleted += GridContainerOnObjectDeletedGrid;
        chainGridContainer.OnObjectSpawned += ChainGridContainerOnObjectSpawned;
        chainGridContainer.OnObjectDeleted += ChainGridContainerOnObjectDeleted;
    }

    private void OnDestroy()
    {
        noteGridContainer.OnObjectSpawned -= NoteGridContainerOnObjectSpawned;
        noteGridContainer.OnObjectDeleted -= NoteGridContainerOnObjectDeleted;
        eventGridContainer.OnObjectSpawned -= GridContainerOnObjectSpawnedGrid;
        eventGridContainer.OnObjectDeleted -= GridContainerOnObjectDeletedGrid;
        chainGridContainer.OnObjectSpawned -= ChainGridContainerOnObjectSpawned;
        chainGridContainer.OnObjectDeleted -= ChainGridContainerOnObjectDeleted;
    }

    private void LateUpdate()
    {
        if (useOffsetFromConfig)
        {
            if (UIMode.SelectedMode is UIModeType.Playing or UIModeType.Preview)
            {
                if (useDespawnOffset)
                    Offset = 0;
                else
                    Offset = vNjsProvider.MaxHalfJumpDurationInBeats;
            }
            else
            {
                Offset = useDespawnOffset
                    ? Settings.Instance.Offset_Despawning * -1
                    : Settings.Instance.Offset_Spawning;
            }
        }

        if (timeSyncController.IsPlaying)
        {
            curTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
            // The due-time index is keyed by Offset; rebuild when it changed mid-play rather than
            // rescanning every frame.
            if (Offset != pendingNoteSpawnOffset) RebuildPendingNoteSpawns();
            RecursiveCheckNotes(true, true);
            RecursiveCheckEvents(true, true);

            if (chainGridContainer != null)
            {
                RecursiveCheckChains(true, true);
            }
        }
    }

    private void OnEnable() => timeSyncController.OnPlayToggled += OnPlayToggle;

    private void OnDisable() => timeSyncController.OnPlayToggled -= OnPlayToggle;

    private void OnPlayToggle(bool playing)
    {
        if (playing)
        {
            CheckAllNotes(false);
            CheckAllEvents(false);

            if (chainGridContainer != null)
            {
                CheckAllChains(false);
            }
        }
    }

    private void CheckAllNotes(bool natural)
    {
        var songTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
        nextNoteIndex = noteGridContainer.MapObjects.BinarySearchBy(songTime + Offset, obj => obj.SongBpmTime);
        if (nextNoteIndex < 0) nextNoteIndex = ~nextNoteIndex;

        // Fresh play session: drop stale early-emission marks and index the notes whose own
        // HalfJumpDuration can out-run the sequential cursor.
        earlyEmittedNotes.Clear();
        RebuildPendingNoteSpawns();

        OnRecursiveNoteCheckFinished?.Invoke(natural, nextNoteIndex - 1);
    }

    private void CheckAllEvents(bool natural)
    {
        var songTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
        nextEventIndex = eventGridContainer.MapObjects.BinarySearchBy(songTime + Offset, obj => obj.SongBpmTime);
        if (nextEventIndex < 0) nextEventIndex = ~nextEventIndex;

        OnRecursiveEventCheckFinished?.Invoke(natural, nextEventIndex - 1);
    }

    private void CheckAllChains(bool natural)
    {
        var songTime = UseAudioTime ? timeSyncController.CurrentAudioBeats : timeSyncController.CurrentSongBpmTime;
        nextChainIndex = chainGridContainer.MapObjects.BinarySearchBy(songTime + Offset, obj => obj.SongBpmTime);
        if (nextChainIndex < 0) nextChainIndex = ~nextChainIndex;

        OnRecursiveChainCheckFinished?.Invoke(natural, nextChainIndex - 1);
    }

    private void RecursiveCheckNotes(bool init, bool natural)
    {
        var objects = noteGridContainer.MapObjects;
        var useAnimationsOffset = useOffsetFromConfig && !useDespawnOffset && UIMode.AnimationMode;
        while (nextNoteIndex < objects.Count)
        {
            var obj = objects[nextNoteIndex];
            // Already spawned through the due-time index: never emit twice, and an early-emitted
            // long-hjd note must not block the walk when the animation offset no longer applies.
            // Remove (not Contains) also drops the mark once the cursor passes so marks do not
            // accumulate across long playback; a past-cursor note can never be re-emitted.
            if (earlyEmittedNotes.Remove(obj))
            {
                nextNoteIndex++;
                continue;
            }
            var offset = useAnimationsOffset ? Math.Max(obj.HalfJumpDuration, Offset) + Track.JUMP_TIME : Offset;

            if (obj.SongBpmTime > curTime + offset) break;

            if (obj.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID))
                OnNotePassedThreshold?.Invoke(natural, nextNoteIndex, obj);

            nextNoteIndex++;
        }

        // Emit due entries the sequential prefix has not reached yet (e.g. behind an ineligible
        // short-hjd note). Entries are sorted by due beat, so a persistent cursor only advances
        // over the due prefix — each entry is visited once and none are ever re-checked.
        if (useAnimationsOffset)
        {
            while (pendingNoteSpawnCursor < pendingNoteSpawns.Count
                && pendingNoteSpawns[pendingNoteSpawnCursor].dueSongBpmTime <= curTime)
            {
                var pending = pendingNoteSpawns[pendingNoteSpawnCursor++];
                if (earlyEmittedNotes.Contains(pending.note) || pending.mapIndex < nextNoteIndex)
                    continue;
                if (pending.note.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID))
                {
                    OnNotePassedThreshold?.Invoke(natural, pending.mapIndex, pending.note);
                    earlyEmittedNotes.Add(pending.note);
                }
            }
        }
    }

    // Indexes future notes (from the sequential cursor on) whose own HalfJumpDuration widens their
    // lookahead beyond the shared Offset; due beat is when their spawn window opens.
    private void RebuildPendingNoteSpawns()
    {
        pendingNoteSpawnOffset = Offset;
        pendingNoteSpawns.Clear();
        pendingNoteSpawnCursor = 0;
        var objects = noteGridContainer.MapObjects;
        for (var i = Math.Max(0, nextNoteIndex); i < objects.Count; ++i)
        {
            var obj = objects[i];
            if (obj.HalfJumpDuration > Offset && !earlyEmittedNotes.Contains(obj))
            {
                pendingNoteSpawns.Add(
                    (obj.SongBpmTime - Math.Max(obj.HalfJumpDuration, Offset) - Track.JUMP_TIME,
                        i, obj));
            }
        }
        pendingNoteSpawns.Sort((a, b) =>
        {
            var byDue = a.dueSongBpmTime.CompareTo(b.dueSongBpmTime);
            return byDue != 0 ? byDue : a.mapIndex.CompareTo(b.mapIndex);
        });
    }

    private void RecursiveCheckEvents(bool init, bool natural)
    {
        var objects = eventGridContainer.MapObjects;
        while (nextEventIndex < objects.Count)
        {
            var obj = objects[nextEventIndex];

            if (obj.SongBpmTime > curTime + Offset) return;

            OnEventPassedThreshold?.Invoke(natural, nextEventIndex, obj);
            nextEventIndex++;
        }
    }

    private void RecursiveCheckChains(bool init, bool natural)
    {
        var objects = chainGridContainer.MapObjects;
        var useAnimationsOffset = useOffsetFromConfig && !useDespawnOffset && UIMode.AnimationMode;
        while (nextChainIndex < objects.Count)
        {
            var obj = objects[nextChainIndex];
            var offset = useAnimationsOffset ? Math.Max(obj.HalfJumpDuration, Offset) + Track.JUMP_TIME : Offset;

            if (obj.TailSongBpmTime > curTime + offset) return;

            OnChainPassedThreshold?.Invoke(natural, nextChainIndex, obj);
            nextChainIndex++;
        }
    }

    private void NoteGridContainerOnObjectSpawned(BaseObject obj)
    {
        OnObjSpawn(obj, ref nextNoteIndex);
        // A mid-play insert shifts map indices and can add a new long-hjd note ahead of the cursor.
        if (timeSyncController.IsPlaying) RebuildPendingNoteSpawns();
    }

    private void NoteGridContainerOnObjectDeleted(BaseObject obj)
    {
        OnObjDeleted(obj, ref nextNoteIndex);
        if (!timeSyncController.IsPlaying) return;
        earlyEmittedNotes.Remove(obj);
        RebuildPendingNoteSpawns();
    }

    private void GridContainerOnObjectSpawnedGrid(BaseObject obj) => OnObjSpawn(obj, ref nextEventIndex);

    private void GridContainerOnObjectDeletedGrid(BaseObject obj) => OnObjDeleted(obj, ref nextEventIndex);

    private void ChainGridContainerOnObjectSpawned(BaseObject obj) => OnObjSpawn(obj, ref nextChainIndex);

    private void ChainGridContainerOnObjectDeleted(BaseObject obj) => OnObjDeleted(obj, ref nextChainIndex);

    private void OnObjSpawn(BaseObject obj, ref int idx)
    {
        if (!timeSyncController.IsPlaying || obj.SongBpmTime >= curTime + Offset) return;

        idx++;
    }

    private void OnObjDeleted(BaseObject obj, ref int idx)
    {
        if (!timeSyncController.IsPlaying || obj.SongBpmTime >= curTime + Offset) return;

        idx--;
    }
}
