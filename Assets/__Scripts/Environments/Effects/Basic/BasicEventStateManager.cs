using Beatmap.Base;

public abstract class BasicEventStateManager<TData> : StateManager<TData, BaseEvent>
    where TData : BasicEventStateData
{
    protected BasicEventStateChunksContainer<TData> InitializeStates(
        BasicEventStateChunksContainer<TData> container) =>
        base.InitializeStates(container, CreateState(new BaseEvent()), CreateState(new BaseEvent())) as
            BasicEventStateChunksContainer<TData>;
}
