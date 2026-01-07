using Beatmap.Base;

public class BasicEventStateChunksContainer<T> : StateChunksContainer<T, BaseEvent> where T : BasicEventStateData
{
}

public class BasicEventStateData : StateData<BaseEvent>
{
    public BasicEventStateData(BaseEvent data) : base(data)
    {
    }
}
