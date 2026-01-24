using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public abstract class StateManager : MonoBehaviour, IBeatmapUpdate
{
    public AudioTimeSyncController Atsc;
    public int ID = -1;

    public abstract void Initialize();
    public abstract void Refresh();
    public abstract void UpdateTime(float time);
}

public abstract class StateManager<T> : StateManager where T : BaseObject
{
    public abstract void InsertData(T data);

    // TODO: ugly hack, object gets modified by reference and manager having more than one type/id
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
        container.Resize(Atsc.SongAudioSource.clip.length);

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

    protected virtual void OnInsertConsequentUpdateToNextState(TState newState, TState nextState) { }

    protected void HandleInsertUpdateConsequentStateFrom(
        StateChunksContainer<TState, TData> container,
        TState currState)
    {
        var enumerator = container.Collection.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnInsertConsequentUpdateToNextState(currState, nextState);
        }
    }

    protected TState HandleRemoveState(StateChunksContainer<TState, TData> container, TState stateToRemove)
    {
        var prevState = container.GetPreviousStateFrom(stateToRemove);
        var nextState = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextState(stateToRemove, prevState, nextState);
        container.RemoveState(stateToRemove);

        return stateToRemove;
    }

    protected TState
        HandleRemoveState(StateChunksContainer<TState, TData> container, TData reference, TData original) =>
        HandleRemoveState(container, container.GetStateFrom(reference, original));

    protected virtual void OnRemoveUpdateToNextState(TState currState, TState nextState) { }

    protected void HandleRemoveUpdateConsequentStateFrom(
        StateChunksContainer<TState, TData> container,
        TState currState)
    {
        var enumerator = container.Collection.EnumerateFrom(currState);
        enumerator.MoveNext(); // skip current state
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            OnRemoveUpdateToNextState(currState, nextState);
        }
    }

    protected virtual void
        OnRemoveUpdatePreviousAndNextState(TState currState, TState prevState, TState nextState) =>
        prevState.EndTime = nextState.StartTime;
}
