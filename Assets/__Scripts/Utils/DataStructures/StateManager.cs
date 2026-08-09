using System;
using Beatmap.Base;
using UnityEngine;

public abstract class StateManager : MonoBehaviour, IBeatmapUpdate
{
    public AudioTimeSyncController Atsc;
    public int ID = -1;

    public abstract void Initialize();
    public abstract void Refresh();
    public abstract void UpdateTime(bool isPlaying, float time);
}

public abstract class StateManager<T> : StateManager where T : BaseObject
{
    public abstract void InsertData(T data);

    // TODO: ugly hack, object gets modified by reference and manager having more than one type/id
    /// <summary>
    /// Removes data from the state manager.
    /// IMPORTANT: When an object's time (JsonTime) changes, the state must be removed using the
    /// original time before being re-inserted with the new time. The GetStateFrom method handles
    /// finding states that may be in wrong buckets due to time changes via a fallback linear search.
    /// 
    /// CRITICAL: Any code path that modifies an object's time must ensure RemoveData is called
    /// before InsertData to properly update the cache buckets.
    /// </summary>
    public abstract void RemoveData(T data, T original);
}

public abstract class StateManager<TState, TData> : StateManager<TData>
    where TState : StateData<TData> where TData : BaseObject
{
    protected abstract TState CreateState(TData data);

    protected StateChunksContainer<TState, TData> InitializeStates(
        StateChunksContainer<TState, TData> container,
        TState start,
        TState end)
    {
        container.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

        end.StartTime = end.EndTime;
        container.AddState(start);
        container.AddState(end);

        container.SetStateAt(0);
        return container;
    }

    protected void HandleInsertState(StateChunksContainer<TState, TData> container, TState newState)
    {
        var prevState = container.GetOverlappingStateFrom(newState);
        var nextState = container.GetNextStateFrom(newState);

        OnInsertUpdateToPreviousState(newState, prevState);
        OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        OnInsertUpdateFromNextState(newState, nextState);
        OnInsertUpdateToNextState(newState, nextState);

        container.AddState(newState);
    }

    protected virtual void OnInsertUpdateToPreviousState(TState newState, TState prevState) =>
        prevState.EndTime = newState.StartTime;

    protected virtual void OnInsertUpdateFromNextState(TState newState, TState nextState) =>
        newState.EndTime = nextState.StartTime;

    protected virtual void OnInsertUpdateToNextState(TState newState, TState nextState) { }

    protected virtual void OnInsertUpdateFromPreviousStateAndNextState(
        TState newState,
        TState prevState,
        TState nextState)
    {
    }

    protected virtual void OnInsertConsequentUpdateToNextState(TState currState, TState nextState) { }

    protected void HandleInsertUpdateConsequentStateFrom(
        StateChunksContainer<TState, TData> container,
        TState currState)
    {
        var enumerator = container.Collection.EnumerateAfter(currState);
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnInsertConsequentUpdateToNextState(currState, nextState);
        }
    }

    protected TState HandleRemoveState(StateChunksContainer<TState, TData> container, TState stateToRemove)
    {
        // Fail at the invalid cache boundary so callers cannot leave dependent state objects hanging.
        if (stateToRemove == null)
            throw new InvalidOperationException($"{GetType().Name} cannot remove an uncached state.");

        var prevState = container.GetPreviousStateFrom(stateToRemove);
        var nextState = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextState(stateToRemove, prevState, nextState);
        container.RemoveState(stateToRemove);

        return stateToRemove;
    }

    protected TState
        HandleRemoveState(StateChunksContainer<TState, TData> container, TData reference, TData original)
    {
        var stateToRemove = container.GetStateFrom(reference, original);
        // Fail at lookup rather than silently leaving dependent state objects in the cache.
        if (stateToRemove == null)
            throw new InvalidOperationException(
                $"{GetType().Name} could not find the state for {reference.GetType().Name} at {original.JsonTime}.");
        return HandleRemoveState(container, stateToRemove);
    }

    protected virtual void OnRemoveConsequentUpdateToNextState(TState currState, TState nextState) { }

    protected void HandleRemoveUpdateConsequentStateFrom(
        StateChunksContainer<TState, TData> container,
        TState currState)
    {
        // Consequent updates require the removed state as their ordering anchor.
        if (currState == null)
            throw new InvalidOperationException($"{GetType().Name} cannot update consequences for a missing state.");

        var enumerator = container.Collection.EnumerateAfter(currState);
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnRemoveConsequentUpdateToNextState(currState, nextState);
        }
    }

    protected virtual void
        OnRemoveUpdatePreviousAndNextState(TState currState, TState prevState, TState nextState) =>
        prevState.EndTime = nextState.StartTime;
}
