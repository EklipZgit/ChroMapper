using System;
using System.Collections.Generic;
using Beatmap.Base;
using Random = UnityEngine.Random;

public abstract class RotatingLightsManagerBase : BasicEventStateManager<RotatingLightStateData>
{
    public int Index; // because there are no grouping, we need to assign index for random
    public bool Mirror;
    
    public abstract void UpdateOffset(BaseEvent data, bool mirror, bool isLeftEvent);

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

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
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

public class RotatingLightStateData : BasicEventStateData
{
    public RotatingLightStateData(BaseEvent data) : base(data)
    {
    }
}
