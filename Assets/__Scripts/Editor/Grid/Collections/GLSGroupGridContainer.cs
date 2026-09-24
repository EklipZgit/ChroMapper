using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GLSGroupGridContainer<TGroup> : BeatmapObjectContainerCollection<TGroup>
    where TGroup : BaseEventBoxGroup
{
    private const double DefaultTargetFrameRate = 60d;
    private const int UncappedTargetFrameRateThreshold = 1000;
    private const double RemainingFrameReserveSeconds = 0.001d;
    // GLSScrolling averaged 0.12 ms per ribbon, so reserve a small bounded upload slice even when the base frame exceeds its display deadline.
    private const double MinimumRibbonWorkSeconds = 0.002d;
    private const int MaximumPreviewConfigurationStepsPerFrame = 24;

    private readonly struct PendingPreviewConfiguration : System.IComparable<PendingPreviewConfiguration>
    {
        public PendingPreviewConfiguration(GLSGroupContainer container, long sequence)
        {
            Container = container;
            Priority = container.PreviewConfigurationPrioritySongBpmTime;
            Sequence = sequence;
        }

        public GLSGroupContainer Container { get; }
        private float Priority { get; }
        private long Sequence { get; }

        public int CompareTo(PendingPreviewConfiguration other)
        {
            var comparison = Priority.CompareTo(other.Priority);
            return comparison != 0
                ? comparison
                : Sequence.CompareTo(other.Sequence);
        }
    }

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
    // GLSScrolling spent 459 ms in preview preparation; index crossing events once per refresh instead of scanning them for every loaded group.
    private readonly Dictionary<BaseEventBoxGroup, HashSet<BaseGLSEvent>> retainedPreviewEventsByGroup = new();
    private readonly Stack<HashSet<BaseGLSEvent>> spareRetainedPreviewEventSets = new();
    // distinguish parents rebound during this refresh from genuine pool surplus - perf
    private readonly HashSet<GLSGroupContainer> previouslyLoadedContainers = new();
    // A reusable sorted queue makes cross-group loading follow ascending node time without per-refresh sorting allocations.
    private readonly SortedSet<PendingPreviewConfiguration> pendingPreviewConfigurations = new();
    private readonly Dictionary<GLSGroupContainer, PendingPreviewConfiguration> queuedPreviewContainers = new();
    private readonly HashSet<GLSGroupContainer> hiddenReboundContainers = new();
    private readonly HashSet<GLSGroupContainer> reboundContainers = new();
    private long previewConfigurationSequence;
    private float previewLowerBound;
    private float previewUpperBound;
    private bool deferAutomaticPreviewConfiguration;
    // GLSScrolling reuses recycled GLS bodies during one synchronous node refresh before deactivating leftovers.
    private bool deferPooledContainerDeactivation;

    // Only the GLS collection has the synchronous node-binding phase needed for active pool reuse.
    protected override bool DeferPooledContainerDeactivation => deferPooledContainerDeactivation;

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
        // Anonymous subscription has no handle for StopNotifyingBySettingName; this collection
        // is the only subscriber to the key, so clearing it matches the other grid containers.
        Settings.ClearSettingNotifications(nameof(Settings.GLSOuterTrackGhostNodeOpacity));
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
                    GetRetainedPreviewEvents(group),
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
        // GLSScrolling reuses recycled owners before their TextMeshPro components unregister at this chunk boundary.
        deferPooledContainerDeactivation = true;
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
        IndexRetainedPreviewEvents();

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
                // ColdScrubConfiguresRetainedColorSourceImmediately shares this exact node-binding path with newly retained sources.
                ConfigureLoadedGroup(groupContainer);
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
        var retainedEvents = GetRetainedPreviewEvents(container.EventBoxGroupData);
        // GLSScrolling repeatedly prepared unchanged visible groups at each chunk; keep their nodes and pending ribbons as-is.
        if (!remainsHidden
            && container.HasSamePreviewNodeWindow(previewLowerBound, previewUpperBound, retainedEvents))
        {
            return;
        }
        if (remainsHidden)
        {
            hiddenReboundContainers.Add(container);
        }

        container.BeginPreviewNodeConfiguration(
            eventGridContainer.IsBoostAt,
            previewLowerBound,
            previewUpperBound,
            retainedEvents,
            false,
            remainsHidden,
            true);
        // ScrollingShowsEveryGlsNodeBeforeRibbonWork requires every visible GLS node before the scrub refresh returns.
        while (!container.ProcessPreviewNodeConfigurationStep())
        {
        }
        hiddenReboundContainers.Remove(container);
        if (queuedPreviewContainers.TryGetValue(container, out var queuedEntry))
        {
            pendingPreviewConfigurations.Remove(queuedEntry);
            queuedPreviewContainers.Remove(container);
        }
        // Only color ribbon uploads remain deferred; other GLS node families finish during this refresh.
        if (container.HasPendingPreviewRibbons)
        {
            EnqueuePreviewConfiguration(container);
        }
    }

    // ColdScrubConfiguresRetainedColorSourceImmediately requires the color collection's post-pool source to bind its node in the same refresh.
    protected void ConfigureLoadedGroup(GLSGroupContainer groupContainer)
    {
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
                GetRetainedPreviewEvents(groupContainer.EventBoxGroupData));
        }
    }

    private void SchedulePreviewSuspension(GLSGroupContainer container)
    {
        hiddenReboundContainers.Remove(container);
        // Recycled roots are already hidden; finish their cheap suspension without delaying new visible nodes.
        container.SuspendPreviewGhosts();
        if (queuedPreviewContainers.TryGetValue(container, out var queuedEntry))
        {
            pendingPreviewConfigurations.Remove(queuedEntry);
            queuedPreviewContainers.Remove(container);
        }
    }

    private void ProcessNextPreviewConfiguration()
    {
        // GLSScrolling called the frame-budget calculation after every queued node; compute its display target once per frame.
        var frameDeadline = Math.Max(
            GetCurrentFrameDeadline(),
            Time.realtimeSinceStartupAsDouble + MinimumRibbonWorkSeconds);
        var processedSteps = 0;
        while (pendingPreviewConfigurations.Count > 0
            && processedSteps < MaximumPreviewConfigurationStepsPerFrame)
        {
            var queuedEntry = pendingPreviewConfigurations.Min;
            pendingPreviewConfigurations.Remove(queuedEntry);
            var container = queuedEntry.Container;
            if (container == null)
            {
                queuedPreviewContainers.Remove(container);
                hiddenReboundContainers.Remove(container);
                continue;
            }

            queuedPreviewContainers.Remove(container);
            // Scrub nodes are already complete; spend this frame's spare time on one color ribbon.
            if (container.ProcessNextPreviewRibbon())
            {
                hiddenReboundContainers.Remove(container);
            }
            else
            {
                // The next ghost can have a different time, so snapshot a fresh priority after every work unit.
                EnqueuePreviewConfiguration(container);
            }
            processedSteps++;
            // PreviewSchedulerUsesTheConfiguredFrameDeadline batches only while this frame retains time before its configured presentation deadline.
            if (Time.realtimeSinceStartupAsDouble >= frameDeadline)
            {
                return;
            }
        }
        // GLSScrolling only pays Unity/TextMeshPro deactivation for owners that found no replacement in this refresh.
        DeactivateUnusedActivePooledContainers();
        deferPooledContainerDeactivation = false;
    }

    private static double GetCurrentFrameDeadline()
    {
        var displayRefreshRate = Screen.currentResolution.refreshRateRatio.value;
        double targetFrameRate;
        if (QualitySettings.vSyncCount > 0 && displayRefreshRate > 0d)
        {
            targetFrameRate = displayRefreshRate / QualitySettings.vSyncCount;
        }
        else if (Application.targetFrameRate > 0
            && Application.targetFrameRate <= UncappedTargetFrameRateThreshold)
        {
            targetFrameRate = Application.targetFrameRate;
        }
        else
        {
            // ChroMapper's default 9999 FPS means uncapped; use the monitor as its practical presentation target.
            targetFrameRate = displayRefreshRate > 0d
                ? displayRefreshRate
                : DefaultTargetFrameRate;
        }

        var targetFrameDuration = 1d / targetFrameRate;
        var reserve = Math.Min(RemainingFrameReserveSeconds, targetFrameDuration * 0.25d);
        // Unity samples unscaledTime at frame start; the scheduler compares only the running clock per ribbon.
        return Time.unscaledTimeAsDouble + targetFrameDuration - reserve;
    }

    private void EnqueuePreviewConfiguration(GLSGroupContainer container)
    {
        var entry = new PendingPreviewConfiguration(container, previewConfigurationSequence++);
        pendingPreviewConfigurations.Add(entry);
        queuedPreviewContainers[container] = entry;
    }

    // QueuedPreviewConfigurationsPrioritizeEarlierGroups observes the same non-mutating head used by the scheduler.
    internal GLSGroupContainer NextPendingPreviewConfiguration => pendingPreviewConfigurations.Count > 0
        ? pendingPreviewConfigurations.Min.Container
        : null;

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

    private void IndexRetainedPreviewEvents()
    {
        // A single data-only grouping keeps every BeginPreviewNodeConfiguration lookup proportional to that owner's crossing ribbons.
        foreach (var events in retainedPreviewEventsByGroup.Values)
        {
            events.Clear();
            spareRetainedPreviewEventSets.Push(events);
        }
        retainedPreviewEventsByGroup.Clear();

        foreach (var previewEvent in RetainedPreviewEvents)
        {
            var group = previewEvent.EventBoxGroupData;
            if (!retainedPreviewEventsByGroup.TryGetValue(group, out var events))
            {
                events = spareRetainedPreviewEventSets.Count > 0
                    ? spareRetainedPreviewEventSets.Pop()
                    : new HashSet<BaseGLSEvent>();
                retainedPreviewEventsByGroup.Add(group, events);
            }
            events.Add(previewEvent);
        }
    }

    private ISet<BaseGLSEvent> GetRetainedPreviewEvents(BaseEventBoxGroup group) =>
        group != null && retainedPreviewEventsByGroup.TryGetValue(group, out var events)
            ? events
            : null;

    protected bool IsGroupOnActivePage(int groupId) => glsGroupGridProvider.ActiveGlsTrackIds.Contains(groupId);

    protected override bool ShouldRetainContainerOutsideBounds(
        BaseObject obj,
        float lowerBound,
        float upperBound) => obj is TGroup group
        && IsGroupOnActivePage(group.ID)
        && retainedGroups.Contains(group);

    // GLSScrolling keeps ghost text registered until this owner is either rebound or finally deactivated.
    protected override void HandleContainerDespawn(ObjectContainer container, BaseObject obj) =>
        ((GLSGroupContainer)container).ResetForPool(deferPooledContainerDeactivation);

    // Unreused owners must hide their separately parented ghost root as well as the body.
    protected override void HandleUnusedActivePooledContainer(ObjectContainer container) =>
        ((GLSGroupContainer)container).DeactivateUnusedPooledPreviewRoot();

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
            // An active pooled ghost root can follow the new track and bind its nodes before the frame renders.
            if (groupContainer.HasActivePooledPreviewRoot)
                groupContainer.SetPreviewParent(con.transform.parent);
            else
                reboundContainers.Add(groupContainer);
        }
        else
        {
            groupContainer.SetPreviewParent(con.transform.parent);
            groupContainer.ConfigurePreviewNodes(
                eventGridContainer.IsBoostAt,
                previewLowerBound,
                previewUpperBound,
                GetRetainedPreviewEvents(e));
        }
    }
}
