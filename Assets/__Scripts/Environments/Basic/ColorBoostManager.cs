using System;
using System.Collections.Generic;
using Beatmap.Base;

public class ColorBoostManager : BasicEventManager<ColorBoostStateData>
{
    private readonly BasicEventStateChunksContainer<ColorBoostStateData> stateChunksContainer = new();
    public bool Boost;

    public event Action<bool> OnStateChange;

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
        OnStateChange(Boost);
    }

    protected override ColorBoostStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertData(evt);
    }

    public override void InsertData(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        state.Boost = evt.Value == 1;

        HandleInsertState(stateChunksContainer, state);
    }

    public override void RemoveData(BaseEvent evt)
    {
        var state = HandleRemoveState(stateChunksContainer, evt);
        if (stateChunksContainer.CurrentState != state) return;
        stateChunksContainer.SetStateAt(evt.SongBpmTime);
        UpdateObject(stateChunksContainer.CurrentState);
    }

    public override void Reset() => UpdateObject(stateChunksContainer.CurrentState);
}

public class ColorBoostStateData : BasicEventStateData
{
    public bool Boost;

    public ColorBoostStateData(BaseEvent evt) : base(evt)
    {
    }
}
