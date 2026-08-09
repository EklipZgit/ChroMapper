using Beatmap.Base;
using UnityEngine;

public class TrackLaneRingsRotationEffect : BasicEventEffect<TrackLaneRingsRotationStateData>
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

    protected void Start() => Effect.Manager.Atsc = Atsc;

    public override void Initialize() => InitializeStates(container);

    public override void Refresh()
    {
        container.SetStateAt(Atsc.CurrentSongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (container.IsCurrentOrFindState(currentTime, isPlaying)) return;
        container.SetStateAt(currentTime);
        UpdateObject(container.CurrentState);
    }

    public void UpdateObject(TrackLaneRingsRotationStateData state)
    {
        if (state == null) return;

        var step = GetStep();
        if (state.Base.CustomData != null && state.Base.CustomStep != null) step = state.Base.CustomStep.Value;

        var rotation = state.Base.CustomRingRotation ?? Rotation;
        var prop = state.Base.CustomProp ?? PropagationSpeed;
        var speed = state.Base.CustomSpeed ?? FlexySpeed;

        state.Direction = state.Base.CustomData != null && state.Base.CustomDirection != null
            ? state.Base.CustomDirection == 0
            : state.Direction;

        var counterSpin = state.Base.CustomData?.HasKey("_counterSpin") == true &&
                          state.Base.CustomData["_counterSpin"].AsBool;

        Effect.AddRingRotationEvent(
            state.RotationInitial,
            step,
            prop,
            speed,
            rotation,
            state.Direction,
            counterSpin);
    }

    private float GetStep() => StepType switch
    {
        RotationStepType.Range0ToMax => Random.Range(0f, Step),
        RotationStepType.Range => Random.Range(0f - Step, Step),
        RotationStepType.MaxOr0 => Random.value > 0.5f ? Step : 0f,
        _ => 0f
    };

    protected override TrackLaneRingsRotationStateData CreateState(BaseEvent data) =>
        new(data) { RotationInitial = Effect.StartupRotationAngle, RotationChange = 0f };

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
        TrackLaneRingsRotationStateData currState,
        TrackLaneRingsRotationStateData nextState) =>
        nextState.RotationInitial += currState.RotationChange;

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = container.GetStateFrom(reference, original);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    protected override void OnRemoveConsequentUpdateToNextState(
        TrackLaneRingsRotationStateData currState,
        TrackLaneRingsRotationStateData nextState)
    {
        base.OnRemoveConsequentUpdateToNextState(currState, nextState);
        if (nextState != null)
        {
            nextState.RotationInitial -= currState.RotationChange;
        }
    }
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
