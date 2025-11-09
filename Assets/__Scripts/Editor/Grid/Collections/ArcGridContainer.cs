using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// Note that <see cref="ArcGridContainer"></see> uses `UseChunkLoadingWhenPlaying`. Therefore arc doesn't fade after passing through.
/// </summary>
public class ArcGridContainer : BeatmapObjectContainerCollection<BaseArc>
{
    [SerializeField] private GameObject arcPrefab;
    [SerializeField] private ArcAppearanceSO arcAppearanceSO;

    public const float ViewEpsilon = 0.1f; // original view is too small ?? sometimes cause error.

    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    private bool isPlaying;

    private Queue<ArcContainer> queuedUpdatingArcs = new();
    private const int maxRecomputePerFrame = 2;
    public override ObjectType ContainerType => ObjectType.Arc;

    public override ObjectContainer CreateContainer()
    {
        var con = ArcContainer.SpawnArc(null, ref arcPrefab);
        con.Animator.Atsc = AudioTimeSyncController;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Arcs);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Arcs);

    internal override void SubscribeToCallbacks()
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        if (notesContainer != null) notesContainer.OnContainerSpawned += CheckUpdatedNote;

        AudioTimeSyncController.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        var notesContainer = GetCollectionForType(ObjectType.Note) as NoteGridContainer;
        if (notesContainer != null) notesContainer.OnContainerSpawned -= CheckUpdatedNote;

        AudioTimeSyncController.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    internal override void LateUpdate()
    {
        base.LateUpdate();
        ScheduleRecomputePosition();
    }

    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }

    private void CheckUpdatedNote(BaseObject obj)
    {
        var note = obj as BaseNote;
        if (note.Type == (int)NoteType.Bomb) return;
        var arcs = GetBetween(note.JsonTime - ViewEpsilon, note.JsonTime + ViewEpsilon);
        foreach (BaseArc arc in arcs)
        {
            LoadedContainers.TryGetValue(arc, out var con);
            var container = con as ArcContainer;
            if (container == null) continue;
            container.DetectConnectedNote();

            break;
        }
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

    protected override void HandleContainerSpawn(ObjectContainer container, BaseObject obj) =>
        (container as ArcContainer).DetectConnectedNote();

    /// <summary>
    /// When playing, disable all indicator blocks
    /// </summary>
    /// <param name="isPlaying"></param>
    private void OnPlayToggle(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        // if (isPlaying) RefreshPool(true); // I dont know if removing this line affects anything, we'll see
        foreach (ArcContainer obj in LoadedContainers.Values)
        {
            obj.SetIndicatorBlocksActive(!this.isPlaying);
        }
    }

    public void UpdateColor(Color red, Color blue) => arcAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var arc = con as ArcContainer;
        var arcData = obj as BaseArc;
        arc.NotifySplineChanged(arcData);
        arcAppearanceSO.SetArcAppearance(arc);
        arc.Setup();
        arc.SetIndicatorBlocksActive(false);

        if (!arc.Animator.AnimatedTrack)
        {
            var track = tracksManager.GetTrackAtTime(arcData.SongBpmTime);
            track.AttachContainer(con);
        }
    }

    /// <summary>
    /// Push a container into waiting queue to recompute.
    /// </summary>
    /// <param name="container"></param>
    public void RequestForSplineRecompute(ArcContainer container)
    {
        queuedUpdatingArcs.Enqueue(container);
    }

    /// <summary>
    /// Only compute several splines per frame, avoid burst stuck.
    /// </summary>
    /// <returns></returns>
    private void ScheduleRecomputePosition()
    {
        for (int i = 0; i < maxRecomputePerFrame && queuedUpdatingArcs.Count != 0; ++i)
        {
            var container = queuedUpdatingArcs.Dequeue();
            container.RecomputePosition();
            container.SetIndicatorBlocksActive(!isPlaying);
        }
    }
}
