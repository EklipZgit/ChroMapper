using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public abstract class PlatformEventEffect : BasicEventStateManager<PlatformEventStateData>
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, BaseEvent data);

    private readonly Dictionary<int, BasicEventStateChunksContainer<PlatformEventStateData>> stateChunksContainerMap =
        new();

    public override void Initialize() => stateChunksContainerMap.Clear();

    public override void UpdateTime(float currentTime)
    {
        foreach (var container in stateChunksContainerMap.Values.Where(container =>
            !container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)))
            UpdateObject(container.CurrentState);
    }

    private void UpdateObject(PlatformEventStateData stateData)
    {
        var data = stateData.Base;
        OnEventTrigger(data.Type, data);
    }

    protected override PlatformEventStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        var baseEvents = dataList.ToList();
        var type = baseEvents.First().Type;
        if (!stateChunksContainerMap.ContainsKey(type))
        {
            stateChunksContainerMap[type] = InitializeStates(new());
            foreach (var state in stateChunksContainerMap[type].Chunks.SelectMany(state => state))
                state.Base.Type = type;
        }

        foreach (var data in baseEvents) InsertData(data);
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        HandleInsertState(stateChunksContainerMap[data.Type], state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var container = stateChunksContainerMap[original.Type];
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void UpdateDirty()
    {
        foreach (var container in stateChunksContainerMap.Values) UpdateObject(container.CurrentState);
    }
}

public class PlatformEventStateData : BasicEventStateData
{
    public PlatformEventStateData(BaseEvent data) : base(data)
    {
    }
}
