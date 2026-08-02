using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>
    where TGroup : BaseEventBoxGroup
{
    [SerializeField] private GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // Reuse the retention set because pool refreshes happen frequently while scrolling.
    private readonly System.Collections.Generic.HashSet<TGroup> retainedGroups = new();

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        // Rebuild loaded groups immediately when the ghost-preview setting changes.
        Settings.NotifyBySettingName(nameof(Settings.GLSOuterTrackGhostNodeOpacity), _ => RefreshPool(true));
    }
    internal override void UnsubscribeToCallbacks() => BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Keep a parent group loaded while its final preview node still overlaps the unload boundary.
        retainedGroups.Clear();
        foreach (var loadedObject in ObjectsWithContainers)
        {
            if (loadedObject is TGroup group
                && group.SongBpmTime < lowerBound
                && group.HasMatchingTrack(TrackFilterID)
                && GetLastPreviewTime(group) >= lowerBound)
            {
                retainedGroups.Add(group);
            }
        }

        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Restore a parent recycled by the normal start-time pool check so its preview ghosts remain visible.
        foreach (var group in retainedGroups)
        {
            if (!LoadedContainers.ContainsKey(group))
                CreateContainerFromPool(group);
        }
    }

    private static float GetLastPreviewTime(TGroup group)
    {
        var orderedEvents = group.ReadOnlyOrderedEvents;
        if (orderedEvents.Count == 0)
        {
            return group.SongBpmTime;
        }

        // OrderedEvents is maintained when GLS previews are rebuilt, avoiding a nested box/event scan per pool refresh.
        return orderedEvents[orderedEvents.Count - 1].SongBpmTime;
    }

    public override ObjectContainer CreateContainer() =>
        GLSGroupContainer.SpawnGLSGroup(
            null,
            BeatmapContext.TracksDefinition,
            ref eventPrefab);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var e = obj as BaseEventBoxGroup;
        con.transform.SetParent(
            glsGroupGridProvider.IdToTracks.TryGetValue(e.ID, out var track)
                ? track.Track.ObjectParentTransform
                : TargetTransform,
            false);

        var pos = con.transform.localPosition;
        pos.x = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(BeatmapContext.TracksDefinition, e);
        pos.y = 0.5f;
        con.transform.localPosition = pos;

        // Rebuild previews with boost evaluated at each represented inner event's absolute time.
        (con as GLSGroupContainer).ConfigurePreviewNodes(eventGridContainer.IsBoostAt);
    }
}
