using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public class GagaDiskManager : BasicEventStateManager<GagaDiskStateData>
{
    public List<GagaDisk> Disks = new();

    private readonly Dictionary<int, BasicEventStateChunksContainer<GagaDiskStateData>> typeToStateChunksContainer =
        new();

    private const int minEventValue = 0;
    private const int maxEventValue = 8;

    public void Start()
    {
        foreach (var disk in Disks)
            // Start at Y 20 (default).
            disk.Init();
    }

    private void LateUpdate()
    {
        foreach (var disk in Disks) disk.LateUpdateDisk(Atsc.CurrentJsonTime);
    }

    public override void Initialize()
    {
        typeToStateChunksContainer.Clear();
        foreach (var type in new List<int>
            {
                12,
                13,
                16,
                17,
                18,
                19
            }.Where(type => !typeToStateChunksContainer.ContainsKey(type)))
        {
            typeToStateChunksContainer[type] =
                InitializeStates(new BasicEventStateChunksContainer<GagaDiskStateData>());
            foreach (var state in typeToStateChunksContainer[type].Chunks.SelectMany(state => state))
                state.Base.Type = type;
        }
    }

    public override void UpdateTime(float currentTime)
    {
        foreach (var container in typeToStateChunksContainer.Values.Where(container =>
            !container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)))
            UpdateObject(container.CurrentState);
    }

    private void UpdateObject(GagaDiskStateData state)
    {
        foreach (var d in Disks.Where(d => d.HeightEventType == state.Base.Type))
        {
            d.SetPosition(
                ClampEventValue(state.StartValue),
                ClampEventValue(state.EndValue),
                state.StartTime,
                state.EndTime);
        }
    }

    private static int ClampEventValue(int value) => Math.Clamp(value, minEventValue, maxEventValue);

    protected override GagaDiskStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var evt in dataList) InsertData(evt);
    }

    protected override void OnInsertUpdateFromNextState(GagaDiskStateData newState, GagaDiskStateData nextState)
    {
        base.OnInsertUpdateFromNextState(newState, nextState);
        newState.EndValue = nextState.EndValue;
    }

    public override void InsertData(BaseEvent evt)
    {
        var state = CreateState(evt);
        state.StartTime = evt.SongBpmTime;
        state.StartValue = evt.Value;
        HandleInsertState(typeToStateChunksContainer[evt.Type], state);
    }

    public override void RemoveData(BaseEvent evt, BaseEvent original)
    {
        var container = typeToStateChunksContainer[original.Type];
        var state = HandleRemoveState(container, evt, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(evt.SongBpmTime);
        UpdateObject(container.CurrentState);
    }

    public override void UpdateDirty()
    {
        foreach (var container in typeToStateChunksContainer.Values) UpdateObject(container.CurrentState);
    }
}

public class GagaDiskStateData : BasicEventStateData
{
    public int StartValue;
    public int EndValue;

    public GagaDiskStateData(BaseEvent evt) : base(evt)
    {
    }
}
