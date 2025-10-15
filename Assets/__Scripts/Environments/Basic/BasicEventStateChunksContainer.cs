using System.Collections;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class BasicEventStateChunksContainer<T> : StateChunksContainer<T, BaseEvent> where T : BasicEventStateData
{
}

public abstract class BasicEventStateData : StateData<BaseEvent>
{
    protected BasicEventStateData(BaseEvent @base) : base(@base)
    {
    }
}
