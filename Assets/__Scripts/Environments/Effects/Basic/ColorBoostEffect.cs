using System;
using Beatmap.Base;

public class ColorBoostEffect : BasicEventEffect<ColorBoostStateData>, IEffectStateSignal<bool>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> container = new();
    public ColorSchemeSO ColorScheme;
    public bool Boost;
    
    public event Action<bool> OnStateChanged;

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, isPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(ColorBoostStateData stateData)
    {
        if (stateData.Boost == Boost) return;
        Boost = stateData.Boost;
        ColorScheme.SwapEnvironmentColors(Boost);
        OnStateChanged?.Invoke(Boost);
    }

    protected override ColorBoostStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.Boost = data.Value == 1;

        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }
}

public class ColorBoostStateData : BasicEventStateData
{
    public bool Boost;

    public ColorBoostStateData(BaseEvent data) : base(data)
    {
    }
}
