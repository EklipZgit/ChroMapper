using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public abstract class PlatformEventManager : BasicEventManager<PlatformEventStateData>
{
    public abstract int[] ListeningEventTypes { get; }

    public abstract void OnEventTrigger(int type, BaseEvent evt);

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
        var evt = stateData.Base;
        OnEventTrigger(evt.Type, evt);
    }

    protected override PlatformEventStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> events)
    {
        var baseEvents = events.ToList();
        var type = baseEvents.First().Type;
        if (!stateChunksContainerMap.ContainsKey(type))
        {
            stateChunksContainerMap[type] = InitializeStates(new());
            foreach (var state in stateChunksContainerMap[type].Chunks.SelectMany(state => state))
                state.Base.Type = type;
        }

        foreach (var evt in baseEvents) InsertData(evt);
    }

    public override void InsertData(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        HandleInsertState(stateChunksContainerMap[evt.Type], state);
    }

    public override void RemoveData(BaseEvent evt)
    {
        var container = stateChunksContainerMap[evt.Type];
        var state = HandleRemoveState(container, evt);
        if (container.CurrentState != state) return;
        container.SetStateAt(evt.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void Reset()
    {
        foreach (var container in stateChunksContainerMap.Values) UpdateObject(container.CurrentState);
    }
}

public class PlatformEventStateData : BasicEventStateData
{
    public PlatformEventStateData(BaseEvent evt) : base(evt)
    {
    }
}
