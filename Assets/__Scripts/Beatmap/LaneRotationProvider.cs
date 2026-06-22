using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class LaneRotationProvider : StateManager<RotationEventStateData, BaseObject>
{
    [Header("State")] public float EditRotation;
    public float PlaybackRotation;

    public event Action<float> OnEditChanged;
    public event Action<float> OnPlaybackChanged;

    private readonly RotationEventStateChunksContainer container = new();

    public override void Initialize()
    {
        InitializeStates(
            container,
            CreateState(new BaseRotationEvent()),
            CreateState(new BaseRotationEvent()));
        InsertData(new BaseRotationEvent());
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        container.IsCurrentOrFindState(time, Atsc.IsPlaying);
        UpdateState(time);
    }

    public void UpdateState(float time)
    {
        var state = container.CurrentState;
        if (state.Absolute)
        {
            if (Mathf.Approximately(state.NextAbsoluteRotation, PlaybackRotation)) return;
            PlaybackRotation = state.NextAbsoluteRotation;
        }
        else if (Mathf.Approximately(time, state.StartTime))
        {
            if (Mathf.Approximately(state.EarlyRotation, PlaybackRotation)) return;
            PlaybackRotation = state.EarlyRotation;
        }
        else
        {
            if (Mathf.Approximately(state.LateRotation, PlaybackRotation)) return;
            PlaybackRotation = state.LateRotation;
        }

        OnPlaybackChanged?.Invoke(PlaybackRotation);
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        RotationEventStateData newState,
        RotationEventStateData prevState,
        RotationEventStateData nextState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.NextAbsoluteRotation = newState.Rotation;

        OnInsertConsequentUpdateToNextState(prevState, newState);
        OnInsertConsequentUpdateToNextState(newState, nextState);
    }

    // early + late rotation is combined
    // if early rotation happens after another rotation event, take late rotation
    protected override void OnInsertConsequentUpdateToNextState(
        RotationEventStateData currState,
        RotationEventStateData nextState)
    {
        if (!Mathf.Approximately(currState.StartTime, nextState.StartTime))
            nextState.EarlyRotation = currState.LateRotation;

        if (nextState.ExecutionTime == ExecutionTime.Early)
        {
            if (Mathf.Approximately(currState.StartTime, nextState.StartTime))
                nextState.EarlyRotation = currState.EarlyRotation + nextState.Rotation;
            else
                nextState.EarlyRotation = currState.LateRotation + nextState.Rotation;
        }

        nextState.LateRotation = currState.LateRotation + nextState.Rotation;
    }

    public override void InsertData(BaseObject data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;

        HandleInsertState(container, state);
        if (!state.Absolute) HandleInsertUpdateConsequentStateFrom(container, state);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        RotationEventStateData currState,
        RotationEventStateData prevState,
        RotationEventStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.NextAbsoluteRotation = nextState.Rotation;
        OnInsertConsequentUpdateToNextState(prevState, nextState);
    }

    protected override void OnRemoveConsequentUpdateToNextState(
        RotationEventStateData currState,
        RotationEventStateData nextState) =>
        OnInsertConsequentUpdateToNextState(currState, nextState);

    public override void RemoveData(BaseObject reference, BaseObject original)
    {
        var state = container.GetStateFrom(reference, original);
        HandleRemoveState(container, state);
        if (!state.Absolute)
            HandleRemoveUpdateConsequentStateFrom(container, container.GetStateAt(state.StartTime).state);

        if (state == container.CurrentState) container.SetStateAt(reference.SongBpmTime);
    }

    public override void Refresh() => UpdateState(Atsc.CurrentSongBpmTime);

    protected override RotationEventStateData CreateState(BaseObject data) => new(data);
}

public class RotationEventStateChunksContainer : StateChunksContainer<RotationEventStateData, BaseObject>
{
}

public class RotationEventStateData : StateData<BaseObject>
{
    public readonly bool Absolute;
    public readonly ExecutionTime ExecutionTime;
    public readonly float Rotation;

    public float EarlyRotation;
    public float LateRotation;

    public float NextAbsoluteRotation;

    public RotationEventStateData(BaseObject obj) : base(obj)
    {
        switch (obj)
        {
            case BaseGrid grid:
                Absolute = true;
                Rotation = grid.Rotation;
                break;
            case BaseRotationEvent evt:
                ExecutionTime = evt.ExecutionTime;
                Rotation = evt.Rotation;
                break;
        }
    }
}
