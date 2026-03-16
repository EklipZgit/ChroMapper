using System;
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
            var offset = useAnimationsOffset ? Math.Max(obj.HalfJumpDuration, Offset) + Track.JUMP_TIME : Offset;

            if (obj.SongBpmTime > curTime + offset) return;

            if (obj.HasMatchingTrack(BeatmapObjectContainerCollection.TrackFilterID))
                OnNotePassedThreshold?.Invoke(natural, nextNoteIndex, obj);

            nextNoteIndex++;
        }
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

    private void NoteGridContainerOnObjectSpawned(BaseObject obj) => OnObjSpawn(obj, ref nextNoteIndex);

    private void NoteGridContainerOnObjectDeleted(BaseObject obj) => OnObjDeleted(obj, ref nextNoteIndex);

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
