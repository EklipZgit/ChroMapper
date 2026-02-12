using System;
using Beatmap.Base;

public class LightRotationEffect : BasicEventEffect<LightRotationStateData>, IEffectStateSignal<LightRotationStateData>
{
    public event Action<LightRotationStateData> OnStateChanged;
    private readonly BasicEventStateChunksContainer<LightRotationStateData> container = new();

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => OnStateChanged?.Invoke(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying))
            OnStateChanged?.Invoke(container.CurrentState);
    }

    protected override LightRotationStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
    }
}

public class LightRotationStateData : BasicEventStateData
{
    public LightRotationStateData(BaseEvent data) : base(data)
    {
    }
}
