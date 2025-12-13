using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Object = UnityEngine.Object;

public abstract class TrackLaneRingsManagerBase : BasicEventStateManager<RingRotationStateData>
{
    public RingFilter RingFilter;

    private readonly Dictionary<int, BasicEventStateChunksContainer<RingRotationStateData>> stateChunksContainerMap =
        new();

    public abstract void HandlePositionEvent(RingRotationStateData stateData, BaseEvent data, int index);
    public abstract void HandleRotationEvent(RingRotationStateData stateData, BaseEvent data, int index);
    public virtual float GetInitialRotation() => 0f;
    public virtual float GetRotationStep() => 0f;
    public virtual bool GetDirection() => false;

    public abstract Object[] GetToDestroy();

    public override void Initialize()
    {
        stateChunksContainerMap.Clear();
        foreach (var type in new List<int> { 8, 9 }.Where(type => !stateChunksContainerMap.ContainsKey(type)))
        {
            stateChunksContainerMap[type] =
                InitializeStates(new BasicEventStateChunksContainer<RingRotationStateData>());
            foreach (var state in stateChunksContainerMap[type].Chunks.SelectMany(chunk => chunk))
                state.Base.Type = type;
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var container in stateChunksContainerMap.Values.Where(container =>
            !container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)))
            UpdateObject(container.CurrentState);
    }

    private void UpdateObject(RingRotationStateData stateData)
    {
        var data = stateData.Base;
        var index = stateChunksContainerMap[stateData.Base.Type].GetStateIndex(stateData);
        switch (data.Type)
        {
            case 8:
                if (data.CustomNameFilter != null)
                {
                    var filter = data.CustomNameFilter;
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        if (RingFilter == RingFilter.Big) HandleRotationEvent(stateData, data, index);
                    }
                    else if (filter.Contains("Small") || filter.Contains("Panels") || filter.Contains("Triangle"))
                    {
                        if (RingFilter == RingFilter.Small) HandleRotationEvent(stateData, data, index);
                    }
                    else
                        HandleRotationEvent(stateData, data, index);
                }
                else
                    HandleRotationEvent(stateData, data, index);

                break;
            case 9:
                HandlePositionEvent(stateData, data, index);
                break;
        }
    }

    protected override RingRotationStateData CreateState(BaseEvent data) =>
        new(data) { RotationInitial = GetInitialRotation(), RotationChange = 0f };

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    protected override void OnInsertUpdateToPreviousState(
        RingRotationStateData newStateData,
        RingRotationStateData previousStateData)
    {
        base.OnInsertUpdateToPreviousState(newStateData, previousStateData);
        newStateData.RotationInitial = previousStateData.RotationInitial + previousStateData.RotationChange;
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        state.RotationChange = data.CustomRingRotation ?? GetRotationStep();
        state.Direction = GetDirection();
        if (data.CustomData != null) state.Direction = data.CustomDirection == 0;
        state.RotationChange = state.Direction ? state.RotationChange : -state.RotationChange;

        var container = stateChunksContainerMap[data.Type];
        HandleInsertState(container, state);
        HandleInsertUpdateConsequentStateFrom(container, state);
    }

    protected override void OnInsertConsequentUpdateToNextState(
        RingRotationStateData currStateData,
        RingRotationStateData nextStateData) =>
        nextStateData.RotationInitial += currStateData.RotationChange;

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var container = stateChunksContainerMap[original.Type];
        var (_, _, state) = container.GetStateFrom(data, original);
        HandleRemoveUpdateConsequentStateFrom(container, state);
        HandleRemoveState(container, state);

        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    protected override void OnRemoveUpdateToNextState(
        RingRotationStateData currStateData,
        RingRotationStateData nextStateData)
    {
        base.OnRemoveUpdateToNextState(currStateData, nextStateData);
        nextStateData.RotationInitial -= currStateData.RotationChange;
    }

    public override void UpdateDirty()
    {
        foreach (var ringType in stateChunksContainerMap.Values) UpdateObject(ringType.CurrentState);
    }
}

public class RingRotationStateData : BasicEventStateData
{
    // unfortunately, you cannot modulo this out, so there's a chance this can overflow
    public float RotationInitial;
    public float RotationChange;
    public bool Direction;

    public RingRotationStateData(BaseEvent data) : base(data)
    {
    }
}

public enum RingFilter
{
    Big,
    Small
}
