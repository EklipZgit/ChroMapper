using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

/// <summary>
/// Base class for managing GLS event group states and effects.
/// IMPORTANT: This class manages the state caching system for GLS event groups.
/// States are stored in StateChunksContainer buckets based on their StartTime (SongBpmTime).
/// 
/// CRITICAL: When an event group's JsonTime changes (e.g., when moved via cut/paste),
/// the state must be properly removed and re-inserted via the StateManager's RemoveData/InsertData
/// mechanism. The UpdateData method in GLSManager handles this by:
/// 1. Removing the old state using the original time (to find it in the correct bucket)
/// 2. Inserting a new state with the updated time (to place it in the correct bucket)
/// 
/// If the state is not properly updated, the renderer will continue showing lights at the old
/// location instead of the new location after the event group is moved.
/// </summary>
public abstract class
    EventGroupEffect<TGroupState, TEventState, TGroup, TBox, TEvent> : StateManager<TGroupState, TGroup>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    [SerializeField] public int Count;

    // EmptyAutomaticAxisLaneDoesNotClaimAuthoredLights: automatic axis lanes are editor-only placement ghosts,
    // not serialized event boxes, so they must not participate in OEM element ownership or removal.
    private static bool IsAuthoredBox(TBox box) => !box.IsAutomaticAxisLane;

    // Beat Saber 1.44.1's V3 and V4 loaders preserve serialized event-box order, then BeatmapEventDataBoxGroup
    // claims each (element, concrete box type, subtype) only when that key is still absent. Consequently the first
    // valid converted box wins every overlap regardless of filter specificity; later boxes affect only unclaimed
    // elements. Color and FloatFX use subtype 0, while Rotation and Translation use the axis so different axes can
    // coexist. Eventless boxes still claim their keys, and a winner's generated events are bounded by the next
    // group's matching element/type/subtype start beat.
    public override void InsertData(TGroup data)
    {
        var taken = new HashSet<(Axis, int)>();
        foreach (var box in data.Boxes)
        {
            if (!IsAuthoredBox(box)) continue;
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            if (indexFilter == null) continue; // i pretend to not see
            // EmptySpecificLaneClaimsOverlapBeforeLaterAllLightsLane: OEM eventless boxes reserve their selected
            // elements but have no final node, so use relative beat zero without indexing an empty event array.
            var lastEventTime = GetEventCount(box) > 0
                ? GetLastEventTime(box)
                : 0f;
            var beatStep = DistributionHelper.GetBeatStep(
                DistributionHelper.GetDurationCount(indexFilter),
                (DistributionType)box.BeatDistributionType,
                box.BeatDistribution,
                lastEventTime);
            // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases carries dense chunk and light coordinates into playback without changing OEM duration or value-distribution ordering.
            foreach (var entry in indexFilter)
            {
                var (element, durationOrder, distributionOrder) = entry;
                var key = (box.GetAxis(), element);
                var container = GetGroupContainer(key);
                if (!taken.Add(key) || container is null) continue;

                var state = CreateState(data);

                state.StartTime = data.SongBpmTime;
                state.LocalJsonTime = data.JsonTime + (beatStep * durationOrder);

                state.BeatStep = beatStep;
                state.Box = box;

                state.ElementID = element;
                state.DurationOrder = durationOrder;
                state.DistributionOrder = distributionOrder;
                // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases stores both selected coordinates once so per-frame playback never traverses the filter.
                state.AffectedChunkOrder = entry.AffectedChunkOrder;
                state.AffectedChunkCount = indexFilter.VisibleCount;
                state.AffectedLightOrder = entry.AffectedLightOrder;
                state.AffectedLightCount = indexFilter.AffectedLightCount;
                state.ConvertedIndexFilter = indexFilter;

                HandleInsertState(container, state);
            }
        }
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        TGroupState newState,
        TGroupState prevState,
        TGroupState nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);

        RemoveEvents(prevState);

        RegenerateEvents(prevState, newState.LocalJsonTime);
        RegenerateEvents(newState, nextState.LocalJsonTime);
    }

    protected void RegenerateEvents(TGroupState state, float maxRelativeJsonTime)
    {
        var key = (state.Box.GetAxis(), state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        // The claim pass already converted this box's filter; re-converting here is O(lights) per edit.
        var indexFilter = state.ConvertedIndexFilter
            ?? IndexFilterHelper.Convert(state.Box.IndexFilter, Count);
        var distributionOffset = GetDistribution(indexFilter, state.Box, state.DistributionOrder);
        var events = GenerateEvents(state, distributionOffset, maxRelativeJsonTime);
        foreach (var data in events) HandleInsertEventState(container, data, out _, out _);
        state.Events = events;
    }

    // GLSColorTimeline maintains per-light query indexes around this relink, so the shared helper
    // reports the literal neighbors instead of forcing the data-only path to repeat the bucket
    // searches or duplicate the UsePrevious-skip rules.
    internal static void HandleInsertEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState newState,
        out TEventState prevState,
        out TEventState nextState)
    {
        prevState = container.GetOverlappingStateFrom(newState);
        nextState = container.GetNextStateFrom(newState);

        prevState.EndTime = newState.StartTime;
        prevState.Next = newState;

        newState.EndTime = nextState.StartTime;
        newState.Previous = prevState;
        if (newState.Previous.UsePrevious) newState.Previous = newState.Previous.Previous;
        newState.Next = nextState;

        nextState.Previous = newState;
        if (nextState.Previous.UsePrevious) nextState.Previous = nextState.Previous.Previous;

        container.AddState(newState);
    }

    public override void RemoveData(
        TGroup reference,
        TGroup original)
    {
        var taken = new HashSet<(Axis, int)>();
        // EmptyAllLightsLaneSuppressesEveryLaterOverlappingEvent: removal must traverse eventless claimants too so
        // elements owned only by an empty lane do not retain stale group states after the authored group is removed.
        foreach (var box in original.Boxes)
        {
            if (!IsAuthoredBox(box)) continue;
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            if (indexFilter == null) continue; // i also pretend to not see
            foreach (var (element, _, _) in indexFilter)
            {
                var key = (box.GetAxis(), element);
                var container = GetGroupContainer(key);
                if (!taken.Add(key) || container is null) continue;

                HandleRemoveState(container, reference, original);
            }
        }
    }

    private void RemoveEvents(TGroupState state)
    {
        var key = (state.Box.GetAxis(), state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        foreach (var evt in state.Events) HandleRemoveEventState(container, evt as TEventState, out _, out _);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        TGroupState currState,
        TGroupState prevState,
        TGroupState nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);

        RemoveEvents(prevState);
        RemoveEvents(currState);

        RegenerateEvents(prevState, nextState.LocalJsonTime);
    }

    // Same out-neighbors contract as HandleInsertEventState: the timeline indexes need the literal
    // neighbors resolved while the removed state still occupies its bucket slot.
    internal static void HandleRemoveEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState stateToRemove,
        out TEventState prevState,
        out TEventState nextState)
    {
        prevState = container.GetPreviousStateFrom(stateToRemove);
        nextState = container.GetNextStateFrom(stateToRemove);

        prevState.EndTime = nextState.StartTime;
        prevState.Next = nextState;

        nextState.Previous = prevState;
        if (nextState.Previous.UsePrevious) nextState.Previous = nextState.Previous.Previous;

        container.RemoveState(stateToRemove);
    }

    protected abstract StateChunksContainer<TGroupState, TGroup> GetGroupContainer((Axis axis, int element) key);
    protected abstract StateChunksContainer<TEventState, TEvent> GetEventContainer((Axis axis, int element) key);

    protected abstract
        IEnumerable<(StateChunksContainer<TGroupState, TGroup> groupContainer, StateChunksContainer<TEventState, TEvent>
            eventContainer)>
        GetContainers();

    protected abstract int GetEventCount(TBox box);
    protected abstract float GetLastEventTime(TBox box);

    protected abstract float GetDistribution(IndexFilterHelper.IndexFilter indexFilter, TBox box, int order);

    protected abstract TEventState[] GenerateEvents(
        TGroupState state,
        float distributionOffset,
        float maxRelativeJsonTime);
}

public abstract class EventGroupStateData<TGroup, TBox, TEvent> : StateData<TGroup>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    public float LocalJsonTime;
    public float BeatStep;

    public int ElementID;
    public int DurationOrder;
    public int DistributionOrder;
    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases caches both denominator pairs while preserving existing affected-chunk semantics for omitted or unknown fourth tokens.
    public int AffectedChunkOrder;
    public int AffectedChunkCount;
    public int AffectedLightOrder;
    public int AffectedLightCount;

    public TBox Box;
    // RegenerateEvents runs once per claimed element, so keep the claim-time conversion rather than
    // re-parsing the same box filter for every light.
    public IndexFilterHelper.IndexFilter ConvertedIndexFilter;
    public EventGroupEventStateData<TEvent>[] Events = Array.Empty<EventGroupEventStateData<TEvent>>();

    protected EventGroupStateData(TGroup data) : base(data)
    {
    }
}

public abstract class EventGroupEventStateData<T> : StateData<T> where T : BaseGLSEvent
{
    public EventGroupEventStateData<T> Previous;
    public EventGroupEventStateData<T> Next;

    public readonly EaseType EaseType;
    public readonly bool UsePrevious;

    protected EventGroupEventStateData(T data, float startTime, int easeType, int usePrevious) : base(data)
    {
        EaseType = (EaseType)easeType;
        UsePrevious = usePrevious == 1;
        StartTime = startTime;
    }
}

public abstract record EventGroupContainer<TGroupState, TEventState, TGroup, TBox, TEvent>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseGLSEvent
{
    public readonly StateChunksContainer<TGroupState, TGroup> GroupContainer = new();
    public readonly StateChunksContainer<TEventState, TEvent> EventContainer = new();
}
