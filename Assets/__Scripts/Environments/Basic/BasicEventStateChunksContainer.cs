using Beatmap.Base;

public class BasicEventStateChunksContainer<T> : StateChunksContainer<T, BaseEvent> where T : BasicEventStateData
{
}

public abstract class BasicEventStateData : StateData<BaseEvent>
{
    protected BasicEventStateData(BaseEvent @base) : base(@base)
    {
    }
}
