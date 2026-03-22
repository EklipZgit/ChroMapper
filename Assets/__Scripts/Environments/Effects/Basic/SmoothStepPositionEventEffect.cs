using Beatmap.Base;
using UnityEngine;

public class SmoothStepPositionEventEffect : BasicEventEffect<SmoothStepPositionStateData>
{
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;

    private readonly Vector3Tween tween = new();

    private Transform tr;
    private Vector3 initPos;
    private readonly BasicEventStateChunksContainer<SmoothStepPositionStateData> container = new();

    private void Awake()
    {
        tr = transform;
        initPos = tr.localPosition;
        tween.Easing = Easing.Cubic.InOut;
    }

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject();

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying)) UpdateObject();
        if (tween.UpdateTime(currentTime)) SetPosition(tween.Current);
    }

    private void UpdateObject()
    {
        var state = container.CurrentState;
        tween.StartTime = state.StartTime;
        tween.StartValue = state.StartPosition;
        tween.EndTime = state.EndTime;
        tween.EndValue = state.EndPosition;
    }

    private Vector3 GetPositionForValue(int value)
    {
        if (ClampValue) value = Mathf.Clamp(value, MinY, MaxY);
        return initPos + (MovementVector * (StepSize * (value - 4)));
    }

    private void SetPosition(Vector3 position) => tr.localPosition = position;

    protected override SmoothStepPositionStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.StartPosition = GetPositionForValue(data.Value);
        HandleInsertState(container, state);
    }

    protected override void OnInsertUpdateToPreviousState(
        SmoothStepPositionStateData newState,
        SmoothStepPositionStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.EndPosition = newState.StartPosition;
    }

    protected override void OnInsertUpdateFromNextState(
        SmoothStepPositionStateData newState,
        SmoothStepPositionStateData nextState)
    {
        base.OnInsertUpdateFromNextState(newState, nextState);
        newState.EndPosition = nextState.StartPosition;
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = HandleRemoveState(container, reference, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(reference.SongBpmTime);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        SmoothStepPositionStateData currState,
        SmoothStepPositionStateData prevState,
        SmoothStepPositionStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.EndPosition = nextState.StartPosition;
    }
}

public class SmoothStepPositionStateData : BasicEventStateData
{
    public Vector3 StartPosition;
    public Vector3 EndPosition;

    public SmoothStepPositionStateData(BaseEvent data) : base(data)
    {
    }
}
