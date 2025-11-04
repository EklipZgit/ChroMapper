using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSProvider : StateManager<VariableNJSStateData, BaseNJSEvent>
{
    public float BaseNjs;
    public float CurrentNjs = 10f;

    private bool init;

    private readonly VariableNJSStateChunksContainer stateChunksContainer = new();

    public override void Initialize()
    {
        InitializeStates(
            stateChunksContainer,
            CreateState(new BaseNJSEvent { UsePrevious = 1 }),
            CreateState(new BaseNJSEvent { UsePrevious = 1 }));
    }

    public override void UpdateTime(float time)
    {
        stateChunksContainer.IsCurrentOrFindState(time, Atsc.IsPlaying);

        var currentState = stateChunksContainer.CurrentState;
        var normalizedTime = (time - currentState.StartTime) / (currentState.EndTime - currentState.StartTime);
        CurrentNjs = Mathf.Max(
            BaseNjs
            + Mathf.Lerp(
                currentState.RelativeNjs,
                currentState.NextRelativeNjs,
                currentState.Easing(normalizedTime)),
            0.01f);
    }

    public override void BuildFromData(IEnumerable<BaseNJSEvent> data)
    {
        foreach (var evt in data) InsertData(evt);
    }

    protected override void OnInsertUpdateToPreviousState(VariableNJSStateData newState, VariableNJSStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.NextRelativeNjs = newState.Base.UsePrevious == 1 ? prevState.RelativeNjs : newState.RelativeNjs;
        var easingId = newState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => newState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    protected override void OnInsertUpdateFromNextState(VariableNJSStateData newState, VariableNJSStateData nextState)
    {
        base.OnInsertUpdateFromNextState(newState, nextState);
        newState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? newState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        newState.Easing = Easing.FromID(easingId);
    }

    public override void InsertData(BaseNJSEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.RelativeNjs = data.RelativeNJS;

        HandleInsertState(stateChunksContainer, state);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        VariableNJSStateData currState,
        VariableNJSStateData prevState,
        VariableNJSStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.NextRelativeNjs = nextState.Base.UsePrevious == 1 ? prevState.RelativeNjs : nextState.RelativeNjs;
        var easingId = nextState.Base.Easing switch
        {
            >= 4 and <= 18 => 0,
            _ => nextState.Base.Easing
        };
        prevState.Easing = Easing.FromID(easingId);
    }

    public override void RemoveData(BaseNJSEvent data)
    {
        var state = HandleRemoveState(stateChunksContainer, data);
        if (state == stateChunksContainer.CurrentState) stateChunksContainer.SetStateAt(data.SongBpmTime);
    }

    public override void Reset() { }

    protected override VariableNJSStateData CreateState(BaseNJSEvent data) => new(data);
}
