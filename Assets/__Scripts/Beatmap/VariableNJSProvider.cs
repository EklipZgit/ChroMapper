using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSProvider : StateManager<VariableNJSStateData, BaseNJSEvent>
{
    [Header("State")] public float NoteJumpSpeed;

    public float JumpDuration;
    public float JumpDistance;

    public float HalfJumpDurationInBeats;
    public float HalfJumpDuration;
    public float HalfJumpDistance;

    [Header("Cached Value")] public float BaseNoteJumpSpeed;
    public float BaseHalfJumpDurationInBeats;
    public float OneBeatDuration;

    public event Action OnChanged;

    private readonly VariableNJSStateChunksContainer stateChunksContainer = new();

    public override void Initialize()
    {
        var bpm = BeatSaberSongContainer.Instance.Info.BeatsPerMinute;
        BaseNoteJumpSpeed = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;

        OneBeatDuration = 60f / bpm;
        BaseHalfJumpDurationInBeats = SpawnParameterHelper.CalculateHalfJumpDuration(
            BaseNoteJumpSpeed,
            BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteStartBeatOffset,
            bpm);

        InitializeStates(
            stateChunksContainer,
            CreateState(new BaseNJSEvent { UsePrevious = 1 }),
            CreateState(new BaseNJSEvent { UsePrevious = 1 }));
        InsertData(new BaseNJSEvent());
    }

    public override void UpdateTime(float time)
    {
        stateChunksContainer.IsCurrentOrFindState(time, Atsc.IsPlaying);

        var currentState = stateChunksContainer.CurrentState;
        var normalizedTime = (time - currentState.StartTime) / (currentState.EndTime - currentState.StartTime);
        var njs = Mathf.Max(
            BaseNoteJumpSpeed
            + Mathf.Lerp(
                currentState.RelativeNjs,
                currentState.NextRelativeNjs,
                currentState.Easing(normalizedTime)),
            0.01f);

        if (Mathf.Approximately(njs, NoteJumpSpeed)) return;
        NoteJumpSpeed = njs;
        UpdateState();
    }

    public void UpdateState()
    {
        var factor = Mathf.Min(NoteJumpSpeed / BaseNoteJumpSpeed, 1f);
        HalfJumpDuration = OneBeatDuration * BaseHalfJumpDurationInBeats / factor;
        HalfJumpDurationInBeats = Atsc.GetBeatFromSeconds(HalfJumpDuration);
        JumpDuration = HalfJumpDuration * 2f;

        JumpDistance = NoteJumpSpeed * JumpDuration;
        HalfJumpDistance = JumpDistance * 0.5f;

        OnChanged?.Invoke();
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

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        VariableNJSStateData newState,
        VariableNJSStateData prevState,
        VariableNJSStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        newState.RelativeNjs = newState.Base.UsePrevious == 1 ? prevState.RelativeNjs : newState.RelativeNjs;
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

    public override void Reset() => UpdateState();

    protected override VariableNJSStateData CreateState(BaseNJSEvent data) => new(data);
}
