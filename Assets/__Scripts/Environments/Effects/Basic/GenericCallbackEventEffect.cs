using System;
using Beatmap.Base;

public class GenericCallbackEventEffect : BasicEventEffect<BasicEventStateData>,
                                          IEffectStateSignal<(int index, BasicEventStateData state)>
{
    public event Action<(int index, BasicEventStateData state)> OnStateChanged;
    private readonly BasicEventStateChunksContainer<BasicEventStateData> container = new();

    public override void Initialize() => InitializeStates(container);

    public override void Refresh() => UpdateObject(container.CurrentState);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateObject(container.CurrentState);
    }

    private void UpdateObject(BasicEventStateData state)
    {
        var index = container.Collection.IndexOf(state);
        OnStateChanged?.Invoke((index, state));
    }

    protected override BasicEventStateData CreateState(BaseEvent data) => new(data);

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
        UpdateObject(container.CurrentState);
    }
}
