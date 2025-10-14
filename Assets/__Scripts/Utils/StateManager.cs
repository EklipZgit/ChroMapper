using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class StateManager<T> : MonoBehaviour where T : BaseObject
{
    public AudioTimeSyncController Atsc;

    public abstract void Initialize();
    public abstract void UpdateTime(float time);
    public abstract void BuildFromData(IEnumerable<T> data);
    public abstract void InsertData(T data);
    public abstract void RemoveData(T data);
    public abstract void Reset();
}

public abstract class StateManager<TData, TBase> : StateManager<TBase>
    where TData : StateData<TBase> where TBase : BaseObject
{
    protected abstract TData CreateState(TBase data);

    protected StateChunksContainer<TData, TBase> InitializeStates(
        StateChunksContainer<TData, TBase> container,
        TData start,
        TData end)
    {
        container.GenerateChunk(Atsc);

        end.StartTime = end.EndTime;
        container.Chunks[0].Add(start);
        container.Chunks[^1].Add(end);

        container.SetStateAt(0);
        return container;
    }

    protected void HandleInsertState(StateChunksContainer<TData, TBase> container, TData newState)
    {
        var (prevChunk, prevIndex, prevState) = container.GetOverlappingStateFrom(newState);
        var (nextChunk, _, nextState) = container.GetNextStateFrom(newState);

        OnInsertUpdateToPreviousState(newState, prevState);
        OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        OnInsertUpdateFromNextState(newState, nextState);
        OnInsertUpdateToNextState(newState, nextState);

        var (_, chunk) = container.GetChunk(newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
            chunk.Insert(prevIndex + 1, newState);
    }

    protected virtual void OnInsertUpdateToPreviousState(TData newState, TData prevState) =>
        prevState.EndTime = newState.StartTime;

    protected virtual void OnInsertUpdateFromNextState(TData newState, TData nextState) =>
        newState.EndTime = nextState.StartTime;

    protected virtual void OnInsertUpdateToNextState(TData newState, TData nextState) { }

    protected virtual void OnInsertUpdateFromPreviousStateAndNextState(TData newState, TData prevState, TData nextState)
    {
    }

    protected virtual void OnInsertConsequentUpdateToNextState(TData newState, TData nextState) { }

    protected void HandleInsertUpdateConsequentStateFrom(StateChunksContainer<TData, TBase> container, TData currState)
    {
        var enumerator = container.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnInsertConsequentUpdateToNextState(currState, nextState);
        }
    }

    protected TData HandleRemoveState(StateChunksContainer<TData, TBase> container, TData stateToRemove)
    {
        var (_, currChunk) = container.GetChunk(stateToRemove.StartTime);
        var (_, _, prevState) = container.GetPreviousStateFrom(stateToRemove);
        var (_, _, nextState) = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextState(stateToRemove, prevState, nextState);
        currChunk.Remove(stateToRemove);

        return stateToRemove;
    }

    protected TData HandleRemoveState(StateChunksContainer<TData, TBase> container, TBase evt)
    {
        var (_, _, state) = container.GetStateFrom(evt);
        return HandleRemoveState(container, state);
    }

    protected virtual void OnRemoveUpdateToNextState(TData currState, TData nextState) { }

    protected void HandleRemoveUpdateConsequentStateFrom(StateChunksContainer<TData, TBase> container, TData currState)
    {
        var enumerator = container.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnRemoveUpdateToNextState(currState, nextState);
        }
    }

    protected virtual void
        OnRemoveUpdatePreviousAndNextState(TData currState, TData prevState, TData nextState) =>
        prevState.EndTime = nextState.StartTime;
}
