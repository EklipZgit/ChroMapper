using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using System.Collections.Generic;
using UnityEngine;

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>
    where TGroup : BaseEventBoxGroup
{
    private readonly struct ActivePageContainerPoolFilter : IContainerPoolFilter
    {
        private readonly HashSet<int> activeGroupIds;

        public ActivePageContainerPoolFilter(HashSet<int> activeGroupIds) => this.activeGroupIds = activeGroupIds;

        public bool Includes(TGroup group) => activeGroupIds.Contains(group.ID);
    }

    [SerializeField] protected GLSGroupGridProvider glsGroupGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSGroupAppearanceSO glsGroupAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // Reuse the retention set because pool refreshes happen frequently while scrolling.
    private readonly HashSet<TGroup> retainedGroups = new();

    // Windowed pooling shares one data-only set across refreshes for ribbons crossing the viewport edge.
    protected readonly HashSet<BaseGLSEvent> RetainedPreviewEvents = new();
    // distinguish parents rebound during this refresh from genuine pool surplus - perf
    private readonly HashSet<GLSGroupContainer> previouslyLoadedContainers = new();
    private readonly LinkedList<GLSGroupContainer> pendingPreviewConfigurations = new();
    private readonly Dictionary<GLSGroupContainer, LinkedListNode<GLSGroupContainer>> queuedPreviewContainers = new();
    private readonly HashSet<GLSGroupContainer> hiddenReboundContainers = new();
    private readonly HashSet<GLSGroupContainer> reboundContainers = new();
    private float previewLowerBound;
    private float previewUpperBound;
    private bool deferAutomaticPreviewConfiguration;

    // Outer GLS queue previews use the finalized event grid's boost timeline at their represented node time.
    public bool IsBoostAt(float jsonTime) => eventGridContainer.IsBoostAt(jsonTime);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        glsGroupGridProvider.OnGroupPageChanged += HandleGroupPageChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated += RefreshBoostDependentAppearances;
        // Rebuild loaded groups immediately when the ghost-preview setting changes.
        Settings.NotifyBySettingName(nameof(Settings.GLSOuterTrackGhostNodeOpacity), _ => RefreshPool(true));
        Settings.NotifyBySettingName(nameof(Settings.GLSInnerEventPreviewShrink), _ => RefreshPool(true));
    }
    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        glsGroupGridProvider.OnGroupPageChanged -= HandleGroupPageChanged;
        eventGridContainer.OnBoostAppearanceRangeInvalidated -= RefreshBoostDependentAppearances;
    }

    private void RefreshBoostDependentAppearances(float startJsonTime, float endJsonTime)
    {
        foreach (var pair in LoadedContainers)
        {
            // LoadedContainers is base-typed, so restore this collection's GLS group type before querying preview data.
            if (pair.Key is not TGroup group)
            {
                continue;
            }

            var orderedEvents = group.OrderedEvents;
            if (HasPreviewEventInRange(orderedEvents, startJsonTime, endJsonTime))
            {
                (pair.Value as GLSGroupContainer).ConfigurePreviewNodes(
                    eventGridContainer.IsBoostAt,
                    previewLowerBound,
                    previewUpperBound,
                    RetainedPreviewEvents,
                    true);
            }
        }
    }

    private static bool HasPreviewEventInRange(
        System.Collections.Generic.List<BaseGLSEvent> orderedEvents,
        float startJsonTime,
        float endJsonTime)
    {
        // Reuse the shared sorted-list lookup so preview invalidation retains one binary-search implementation.
        var eventIndex = orderedEvents.BinarySearchBy(startJsonTime, evt => evt.JsonTime);
        if (eventIndex < 0)
        {
            eventIndex = ~eventIndex;
        }

        // BinarySearchBy can return the final item past the end of the range, so confirm the lower range bound too.
        return eventIndex < orderedEvents.Count
            && orderedEvents[eventIndex].JsonTime >= startJsonTime
            && orderedEvents[eventIndex].JsonTime < endJsonTime;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false) =>
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    private void HandleGroupPageChanged(string _) => RefreshPool();

    internal override void LateUpdate()
    {
        deferAutomaticPreviewConfiguration = true;
        base.LateUpdate();
        deferAutomaticPreviewConfiguration = false;
        ProcessNextPreviewConfiguration();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        if (!deferAutomaticPreviewConfiguration)
            CancelPendingPreviewConfigurations();

        previouslyLoadedContainers.Clear();
        foreach (var container in LoadedContainers.Values)
        {
            if (container is GLSGroupContainer groupContainer)
                previouslyLoadedContainers.Add(groupContainer);
        }

        previewLowerBound = lowerBound;
        previewUpperBound = upperBound;
        PrepareRetainedPreviewEvents(lowerBound);

        // Keep a parent group loaded while its final preview node still overlaps the unload boundary.
        retainedGroups.Clear();
        foreach (var loadedObject in ObjectsWithContainers)
        {
            if (loadedObject is TGroup group
                && group.SongBpmTime < lowerBound
                && IsGroupOnActivePage(group.ID)
                && group.HasMatchingTrack(TrackFilterID)
                && GetLastPreviewTime(group) >= lowerBound)
            {
                retainedGroups.Add(group);
            }
        }

        base.RefreshPool(
            lowerBound,
            upperBound,
            forceRefresh,
            new ActivePageContainerPoolFilter(glsGroupGridProvider.ActiveGlsTrackIds));

        // Restore a parent recycled by the normal start-time pool check so its preview ghosts remain visible.
        foreach (var group in retainedGroups)
        {
            if (!LoadedContainers.ContainsKey(group))
                CreateContainerFromPool(group);
        }

        // Existing parents need only add/remove the few previews that crossed this window boundary.
        foreach (var container in LoadedContainers.Values)
        {
            var groupContainer = container as GLSGroupContainer;
            if (groupContainer != null)
            {
                previouslyLoadedContainers.Remove(groupContainer);
                if (deferAutomaticPreviewConfiguration)
                {
                    SchedulePreviewConfiguration(
                        groupContainer,
                        reboundContainers.Remove(groupContainer));
                }
                else
                {
                    groupContainer.ConfigurePreviewNodes(
                        eventGridContainer.IsBoostAt,
                        previewLowerBound,
                        previewUpperBound,
                        RetainedPreviewEvents);
                }
            }
        }

        foreach (var unusedContainer in previouslyLoadedContainers)
        {
            if (deferAutomaticPreviewConfiguration)
            {
                SchedulePreviewSuspension(unusedContainer);
            }
            else
            {
                unusedContainer.SuspendPreviewGhosts();
            }
        }
    }

    private void SchedulePreviewConfiguration(GLSGroupContainer container, bool hideUntilConfigured)
    {
        var remainsHidden = hideUntilConfigured || hiddenReboundContainers.Contains(container);
        if (remainsHidden)
        {
            hiddenReboundContainers.Add(container);
        }

        container.BeginPreviewNodeConfiguration(
            eventGridContainer.IsBoostAt,
            previewLowerBound,
            previewUpperBound,
            RetainedPreviewEvents,
            false,
            remainsHidden);
        if (queuedPreviewContainers.TryGetValue(container, out var queuedNode))
        {
            if (remainsHidden)
            {
                pendingPreviewConfigurations.Remove(queuedNode);
                pendingPreviewConfigurations.AddFirst(queuedNode);
            }
        }
        else
        {
            var newNode = hideUntilConfigured
                ? pendingPreviewConfigurations.AddFirst(container)
                : pendingPreviewConfigurations.AddLast(container);
            queuedPreviewContainers.Add(container, newNode);
        }
    }

    private void SchedulePreviewSuspension(GLSGroupContainer container)
    {
        hiddenReboundContainers.Remove(container);
        container.BeginPreviewGhostSuspension();
        if (!queuedPreviewContainers.ContainsKey(container))
            queuedPreviewContainers.Add(container, pendingPreviewConfigurations.AddLast(container));
    }

    private void ProcessNextPreviewConfiguration()
    {
        while (pendingPreviewConfigurations.Count > 0)
        {
            var queuedNode = pendingPreviewConfigurations.First;
            pendingPreviewConfigurations.RemoveFirst();
            var container = queuedNode.Value;
            if (container == null)
            {
                queuedPreviewContainers.Remove(container);
                hiddenReboundContainers.Remove(container);
                continue;
            }

            if (container.ProcessPreviewNodeConfigurationStep())
            {
                queuedPreviewContainers.Remove(container);
                hiddenReboundContainers.Remove(container);
            }
            else
            {
                pendingPreviewConfigurations.AddLast(queuedNode);
            }
            return;
        }
    }

    private void CancelPendingPreviewConfigurations()
    {
        foreach (var container in queuedPreviewContainers.Keys)
        {
            if (container != null)
                container.CancelPreviewNodeConfiguration();
        }
        pendingPreviewConfigurations.Clear();
        queuedPreviewContainers.Clear();
        hiddenReboundContainers.Clear();
        reboundContainers.Clear();
    }

    protected virtual void PrepareRetainedPreviewEvents(float lowerBound) => RetainedPreviewEvents.Clear();

    protected bool IsGroupOnActivePage(int groupId) => glsGroupGridProvider.ActiveGlsTrackIds.Contains(groupId);

    protected override bool ShouldRetainContainerOutsideBounds(
        BaseObject obj,
        float lowerBound,
        float upperBound) => obj is TGroup group
        && IsGroupOnActivePage(group.ID)
        && retainedGroups.Contains(group);

    protected override void HandleContainerDespawn(ObjectContainer container, BaseObject obj) =>
        ((GLSGroupContainer)container).ResetForPool();

    private static float GetLastPreviewTime(TGroup group)
    {
        var orderedEvents = group.OrderedEvents;
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
            BeatmapContext.TrackDefinitions,
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
        pos.x = 0.5f + GLSGroupContainer.GetPositionFromTrackDefinition(BeatmapContext.TrackDefinitions, e);
        pos.y = 0.5f;
        con.transform.localPosition = pos;

        var groupContainer = con as GLSGroupContainer;
        // Looks dumb but this is necessary, its also not hot path
        groupContainer.GlsLightCount = BeatmapContext.GetGlsLightCount(e.ID);
        if (deferAutomaticPreviewConfiguration)
        {
            reboundContainers.Add(groupContainer);
        }
        else
        {
            groupContainer.SetPreviewParent(con.transform.parent);
            groupContainer.ConfigurePreviewNodes(
                eventGridContainer.IsBoostAt,
                previewLowerBound,
                previewUpperBound,
                RetainedPreviewEvents);
        }
    }
}
