using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class TrackLaneRingsRotationEffect : BasicEventStateManager<TrackLaneRingsRotationStateData>
{
    public TrackLaneRingsRotation Effect;

    public float Rotation;
    public float Step;
    public RotationStepType StepType;
    public int PropagationSpeed;
    public float FlexySpeed;

    private string ringName;

    private readonly BasicEventStateChunksContainer<TrackLaneRingsRotationStateData> container = new();

    private void Awake() => ringName = gameObject.name;

    public override void Initialize() => InitializeStates(container);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(TrackLaneRingsRotationStateData stateData)
    {
        var data = stateData.Base;
        if (data.CustomNameFilter != null && ringName.Contains(data.CustomNameFilter)) return;

        var step = StepType switch
        {
            RotationStepType.Range0ToMax => Random.Range(0f, Step),
            RotationStepType.Range => Random.Range(0f - Step, Step),
            RotationStepType.MaxOr0 => Random.value > 0.5f ? Step : 0f,
            _ => 0f
        };

        Effect.AddRingRotationEvent(
            stateData.RotationInitial, // TODO: this cause it to snap in unusual way
            step,
            PropagationSpeed,
            FlexySpeed,
            stateData.Direction,
            data);
    }

    protected override TrackLaneRingsRotationStateData CreateState(BaseEvent data) =>
        new(data) { RotationInitial = Effect.StartupRotationAngle, RotationChange = 0f };

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    protected override void OnInsertUpdateToPreviousState(
        TrackLaneRingsRotationStateData newStateData,
        TrackLaneRingsRotationStateData previousStateData)
    {
        base.OnInsertUpdateToPreviousState(newStateData, previousStateData);
        newStateData.RotationInitial = previousStateData.RotationInitial + previousStateData.RotationChange;
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.RotationChange = data.CustomRingRotation ?? Rotation;
        state.Direction = Random.value < 0.5f;
        if (data.CustomData != null) state.Direction = data.CustomDirection == 0;
        state.RotationChange = state.Direction ? state.RotationChange : -state.RotationChange;

        HandleInsertState(container, state);
        HandleInsertUpdateConsequentStateFrom(container, state);
    }

    protected override void OnInsertConsequentUpdateToNextState(
        TrackLaneRingsRotationStateData currStateData,
        TrackLaneRingsRotationStateData nextStateData) =>
        nextStateData.RotationInitial += currStateData.RotationChange;

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var (_, _, state) = container.GetStateFrom(data, original);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    protected override void OnRemoveUpdateToNextState(
        TrackLaneRingsRotationStateData currStateData,
        TrackLaneRingsRotationStateData nextStateData)
    {
        base.OnRemoveUpdateToNextState(currStateData, nextStateData);
        nextStateData.RotationInitial -= currStateData.RotationChange;
    }

    public override void UpdateDirty() => UpdateObject(container.CurrentState);
}

public class TrackLaneRingsRotationStateData : BasicEventStateData
{
    // unfortunately, you cannot modulo this out, so there's a chance this can overflow
    public float RotationInitial;
    public float RotationChange;
    public bool Direction;

    public TrackLaneRingsRotationStateData(BaseEvent data) : base(data)
    {
    }
}

public enum RotationStepType : byte
{
    Range0ToMax,
    Range,
    MaxOr0
}
