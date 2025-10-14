using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.Serialization;

public abstract class RotatingLightsManagerBase : BasicEventManager<RotatingLightStateData>
{
    public abstract void UpdateOffset(bool isLeftEvent, BaseEvent evt);

    public abstract bool IsOverrideLightGroup();
    private readonly BasicEventStateChunksContainer<RotatingLightStateData> stateChunksContainer = new();

    public override void Initialize() => InitializeStates(stateChunksContainer);

    public override void UpdateTime(float currentTime)
    {
        if (stateChunksContainer.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) return;
        UpdateObject(stateChunksContainer.CurrentState);
    }

    private void UpdateObject(RotatingLightStateData stateData) => UpdateOffset(true, stateData.Base);

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
