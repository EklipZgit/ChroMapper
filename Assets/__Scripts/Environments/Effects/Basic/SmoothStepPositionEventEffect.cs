using Beatmap.Base;
using UnityEngine;

public class SmoothStepPositionEventEffect : BasicEventEffect<SmoothStepPositionStateData>
{
    public bool ClampValue;
    public int MinY;
    public int MaxY;
    public Vector3 MovementVector;
    public float StepSize;
    public string EaseType;

    private readonly FloatTween tween = new();

    private Transform tr;
    private Vector3 initPos;
    private Vector3 startPos;
    private Vector3 endPos;
    private readonly BasicEventStateChunksContainer<SmoothStepPositionStateData> container = new();

    private void Awake()
    {
        tr = transform;
        initPos = tr.localPosition;
        tween.Easing = Easing.ByName.TryGetValue(EaseType, out var ease) ? ease : Easing.Linear;
    }

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject();

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject();
        if (tween.UpdateTime(currentTime)) SetPosition(Vector3.LerpUnclamped(startPos, endPos, tween.Current));
    }

    private void UpdateObject()
    {
        var state = container.CurrentState;
        startPos = state.StartPosition;
        endPos = state.EndPosition;
        tween.StartTime = state.StartTime;
        tween.EndTime = state.EndTime;
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

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
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
