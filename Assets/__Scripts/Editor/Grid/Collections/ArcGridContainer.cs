using System;
using System.Collections.Generic;
using System.Linq;
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

    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    private bool isPlaying;

    private Queue<ArcContainer> queuedUpdatingArcs = new();
    private const int maxRecomputePerFrame = 2;
    public override ObjectType ContainerType => ObjectType.Arc;

    public override ObjectContainer CreateContainer()
    {
        var con = ArcContainer.SpawnArc(null, ref arcPrefab);
        con.Animator.Context = Context;
        con.Animator.TracksManager = tracksManager;
        return con;
    }

    protected override void HandleObjectSpawned(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Arcs);

    protected override void HandleObjectDelete(BaseObject _, bool __ = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.Arcs);

    internal override void SubscribeToCallbacks()
    {
        Context.Atsc.OnPlayToggled += OnPlayToggle;
        UIMode.OnPreviewModeSwitched += OnUIPreviewModeSwitch;
    }

    internal override void UnsubscribeToCallbacks()
    {
        Context.Atsc.OnPlayToggled -= OnPlayToggle;
        UIMode.OnPreviewModeSwitched -= OnUIPreviewModeSwitch;
    }

    internal override void LateUpdate()
    {
        base.LateUpdate();
        ScheduleRecomputePosition();
    }

    private void OnUIPreviewModeSwitch() => RefreshPool(true);

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

    // TODO: not my proudest
    public IEnumerable<BaseArc> GetBetweenTail(float jsonTime, float jsonTime2) =>
        MapObjects.Where(x => jsonTime < x.TailJsonTime && x.TailJsonTime < jsonTime2);
}
