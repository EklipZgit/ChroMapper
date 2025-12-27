using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public abstract class
    EventGroupEffect<TGroupState, TEventState, TGroup, TBox, TEvent> : StateManager<TGroupState, TGroup>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseObject
{
    [SerializeField] public int Count;

    public override void BuildFromData(IEnumerable<TGroup> dataList)
    {
        Initialize();
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(TGroup data)
    {
        var taken = new HashSet<(Axis, int)>();
        foreach (var box in data.Boxes.Where(b => GetEventCount(b) > 0))
        {
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            var beatStep = DistributionHelper.GetBeatStep(
                DistributionHelper.GetCount(indexFilter),
                (DistributionType)box.BeatDistributionType,
                box.BeatDistribution,
                GetLastEventTime(box));
            foreach (var (element, durationOrder, distributionOrder) in indexFilter)
            {
                var axis = GetAxis(box);
                var key = (axis, element);
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

    protected void RegenerateEvents(TGroupState state, float maxJsonTime)
    {
        var axis = GetAxis(state.Box);
        var key = (axis, state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        var indexFilter = IndexFilterHelper.Convert(state.Box.IndexFilter, Count);
        var distributionOffset = GetDistribution(indexFilter, state.Box, state.DistributionOrder);
        var events = GenerateEvents(state, distributionOffset, maxJsonTime);
        foreach (var data in events) HandleInsertEventState(container, data);
        state.Events = events;
    }

    private static void HandleInsertEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState newState)
    {
        var (prevChunk, prevIndex, prevState) = container.GetOverlappingStateFrom(newState);
        var (nextChunk, _, nextState) = container.GetNextStateFrom(newState);

        prevState.Next = newState;
        newState.Previous = prevState;
        newState.Next = nextState;
        nextState.Previous = newState;

        prevState.EndTime = newState.StartTime;
        newState.EndTime = nextState.StartTime;

        var (_, chunk) = container.GetChunk(newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
            chunk.Insert(prevIndex + 1, newState);
    }

    public override void RemoveData(
        TGroup data,
        TGroup original)
    {
        foreach (var (groupContainer, _) in GetContainers()) HandleRemoveState(groupContainer, data, original);
    }

    private void RemoveEvents(TGroupState state)
    {
        var axis = GetAxis(state.Box);
        var key = (axis, state.ElementID);
        var container = GetEventContainer(key);
        if (container is null) return;

        foreach (var evt in state.Events) HandleRemoveEventState(container, evt as TEventState);
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

    private static void OnRemoveUpdatePreviousAndNextEventState(TEventState prevState, TEventState nextState)
    {
        prevState.EndTime = nextState.StartTime;

        prevState.Next = nextState;
        nextState.Previous = prevState;
    }

    private void HandleRemoveEventState(
        StateChunksContainer<TEventState, TEvent> container,
        TEventState stateToRemove)
    {
        var (_, currChunk) = container.GetChunk(stateToRemove.StartTime);
        var (_, _, prevState) = container.GetPreviousStateFrom(stateToRemove);
        var (_, _, nextState) = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextEventState(prevState, nextState);
        currChunk.Remove(stateToRemove);
    }

    protected abstract Axis GetAxis(TBox box);

    protected abstract StateChunksContainer<TGroupState, TGroup> GetGroupContainer((Axis axis, int element) key);
    protected abstract StateChunksContainer<TEventState, TEvent> GetEventContainer((Axis axis, int element) key);

    protected abstract
        IEnumerable<(StateChunksContainer<TGroupState, TGroup> groupContainer, StateChunksContainer<TEventState, TEvent>
            eventContainer)>
        GetContainers();

    protected abstract int GetEventCount(TBox box);
    protected abstract float GetLastEventTime(TBox box);

    protected abstract float GetDistribution(IndexFilterHelper.IndexFilter indexFilter, TBox box, int order);

    protected abstract TEventState[] GenerateEvents(TGroupState state, float distributionOffset, float maxJsonTime);
}

public abstract class EventGroupStateData<TGroup, TBox, TEvent> : StateData<TGroup>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseObject
{
    public float LocalJsonTime;
    public float BeatStep;

    public int ElementID;
    public int DurationOrder;
    public int DistributionOrder;

    public TBox Box;
    public EventGroupEventStateData<TEvent>[] Events = Array.Empty<EventGroupEventStateData<TEvent>>();

    protected EventGroupStateData(TGroup data) : base(data)
    {
    }
}

public abstract class EventGroupEventStateData<T> : StateData<T> where T : BaseObject
{
    public EventGroupEventStateData<T> Previous;
    public EventGroupEventStateData<T> Next;

    public float Distribution;

    protected EventGroupEventStateData(T data, float startTime, float offset) : base(data)
    {
        StartTime = startTime;
        Distribution = offset;
    }
}

public abstract record EventGroupContainer<TGroupState, TEventState, TGroup, TBox, TEvent>
    where TGroupState : EventGroupStateData<TGroup, TBox, TEvent>
    where TEventState : EventGroupEventStateData<TEvent>
    where TGroup : BaseEventBoxGroup<TBox>
    where TBox : BaseEventBox
    where TEvent : BaseObject
{
    public readonly StateChunksContainer<TGroupState, TGroup> GroupContainer = new();
    public readonly StateChunksContainer<TEventState, TEvent> EventContainer = new();
}
