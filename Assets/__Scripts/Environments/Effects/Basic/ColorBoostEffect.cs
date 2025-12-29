using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostEffect : BasicEventStateManager<ColorBoostStateData>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> container = new();
    public ColorSchemeSO ColorScheme;
    public bool Boost;

    public event Action<bool> OnStateChanged;

    public override void Initialize() => InitializeStates(container);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(ColorBoostStateData stateData)
    {
        if (stateData.Boost == Boost) return;
        Boost = stateData.Boost;
        ColorScheme.SwapEnvironmentColors(Boost);
        OnStateChanged?.Invoke(Boost);
    }

    protected override ColorBoostStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

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

    public override void UpdateDirty() => UpdateObject(container.CurrentState);
}

public class ColorBoostStateData : BasicEventStateData
{
    public bool Boost;

    public ColorBoostStateData(BaseEvent data) : base(data)
    {
    }
}
