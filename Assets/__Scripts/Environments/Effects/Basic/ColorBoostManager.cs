using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventStateManager<ColorBoostStateData>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> stateChunksContainer = new();
    public ColorSchemeSO ColorScheme;
    public bool Boost;

    public event Action<bool> OnStateChanged;

    public override void Initialize() => InitializeStates(stateChunksContainer);

    public override void UpdateTime(float currentTime)
    {
        if (stateChunksContainer.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) return;
        UpdateObject(stateChunksContainer.CurrentState);
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

        HandleInsertState(stateChunksContainer, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(stateChunksContainer, data, original);
        if (stateChunksContainer.CurrentState != state) return;
        stateChunksContainer.SetStateAt(data.SongBpmTime);
        UpdateObject(stateChunksContainer.CurrentState);
    }

    public override void UpdateDirty() => UpdateObject(stateChunksContainer.CurrentState);
}
