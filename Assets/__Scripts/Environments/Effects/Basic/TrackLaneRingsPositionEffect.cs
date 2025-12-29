using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Object = UnityEngine.Object;

public class TrackLaneRingsPositionEffect : BasicEventStateManager<TrackLaneRingsPositionStateData>
{
    public TrackLaneRingsManager Manager;

    public float MinPositionStep;
    public float MaxPositionStep;
    public float MoveSpeed;

    private readonly BasicEventStateChunksContainer<TrackLaneRingsPositionStateData> container = new();

    public override void Initialize() => InitializeStates(container);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(TrackLaneRingsPositionStateData state)
    {
        var index = container.GetStateIndex(state);

        var zoomed = index % 2 == 0;
        var step = state.UseCustom ? state.Step : zoomed ? MaxPositionStep : MinPositionStep;
        var speed = state.Speed;

        for (var i = 0; i < Manager.Rings.Length; i++)
        {
            var destPosZ = i * step;
            Manager.Rings[i].SetPosition(destPosZ, speed);
        }
    }

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.UseCustom = data.CustomStep.HasValue;
        state.Step = data.CustomStep ?? 0f;
        state.Speed = data.CustomSpeed ?? MoveSpeed;

        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var (_, _, state) = container.GetStateFrom(data, original);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void UpdateDirty() => UpdateObject(container.CurrentState);

    protected override TrackLaneRingsPositionStateData CreateState(BaseEvent data) => new(data);
}

public class TrackLaneRingsPositionStateData : BasicEventStateData
{
    public bool UseCustom;
    public float Step;
    public float Speed;

    public TrackLaneRingsPositionStateData(BaseEvent data) : base(data)
    {
    }
}
