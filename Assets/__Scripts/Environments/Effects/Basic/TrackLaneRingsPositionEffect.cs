using System;
using Beatmap.Base;

public class TrackLaneRingsPositionEffect : BasicEventEffect<TrackLaneRingsPositionStateData>,
                                            IEffectStateSignal<(int index, TrackLaneRingsPositionStateData state)>
{
    public event Action<(int index, TrackLaneRingsPositionStateData state)> OnStateChanged;
    private readonly BasicEventStateChunksContainer<TrackLaneRingsPositionStateData> container = new();

    public override void Initialize() => InitializeStates(container);
    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(TrackLaneRingsPositionStateData state)
    {
        var index = container.Collection.IndexOf(state);
        OnStateChanged?.Invoke((index, container.CurrentState));
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.Step = data.CustomStep;
        state.Speed = data.CustomSpeed;

        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = container.GetStateFrom(data, original);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    protected override TrackLaneRingsPositionStateData CreateState(BaseEvent data) => new(data);
}

public class TrackLaneRingsPositionStateData : BasicEventStateData
{
    public float? Step;
    public float? Speed;

    public TrackLaneRingsPositionStateData(BaseEvent data) : base(data)
    {
    }
}
