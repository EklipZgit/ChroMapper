using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;

public abstract class BasicEventStateManager<TData> : StateManager<TData, BaseEvent>
    where TData : BasicEventStateData
{
    protected virtual void Start()
    {
        var beec = GetComponent<BasicEventEffectManager>();
        if (!AutoRegister || beec == null) return;

        foreach (var type in Types)
        {
            if (beec.EventTypeToEffects[type].Any(x => x == this)) return;
            beec.Register(type, this);
        }
    }

    protected BasicEventStateChunksContainer<TData> InitializeStates(
        BasicEventStateChunksContainer<TData> container) =>
        base.InitializeStates(container, CreateState(new BaseEvent()), CreateState(new BaseEvent())) as
            BasicEventStateChunksContainer<TData>;
}
