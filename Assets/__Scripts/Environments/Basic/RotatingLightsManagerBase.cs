using System;
using System.Collections.Generic;
using Beatmap.Base;
using Random = UnityEngine.Random;

public abstract class RotatingLightsManagerBase : BasicEventManager<RotatingLightStateData>
{
    public int Index; // because there are no grouping, we need to assign index for random
    public bool Mirror;
    
    public abstract void UpdateOffset(BaseEvent evt, bool mirror, bool isLeftEvent);

    public abstract bool IsOverrideLightGroup();
    private readonly BasicEventStateChunksContainer<RotatingLightStateData> stateChunksContainer = new();

    public override void Initialize() => InitializeStates(stateChunksContainer);

    public override void UpdateTime(float currentTime)
    {
        if (stateChunksContainer.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) return;
        UpdateObject(stateChunksContainer.CurrentState);
    }

    private void UpdateObject(RotatingLightStateData stateData)
    {
        var data = stateData.Base;
        var hash = HashCode.Combine(data.SongBpmTime, Index);
        Random.InitState(hash);
        UpdateOffset(data, Mirror, true);
    }

    protected override RotatingLightStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> events)
    {
        foreach (var evt in events) InsertData(evt);
    }

    public override void InsertData(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
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

public class RotatingLightStateData : BasicEventStateData
{
    public RotatingLightStateData(BaseEvent evt) : base(evt)
    {
    }
}
