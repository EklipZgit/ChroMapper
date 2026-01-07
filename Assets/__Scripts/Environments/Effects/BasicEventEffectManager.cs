using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectManager : MonoBehaviour
{
    [SerializeField] private List<BasicEventStateManagerEntry> effectEntries = new();

    public readonly Dictionary<int, List<StateManager<BaseEvent>>> EventTypeToEffects = new();

    private void Awake()
    {
        foreach (var managers in effectEntries.OrderBy(x => x.Type).GroupBy(x => x.Type))
            EventTypeToEffects.Add(managers.First().Type, managers.Select(x => x.Manager).ToList());
    }

    public void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var manager in EventTypeToEffects.Values.SelectMany(x => x).Distinct())
        {
            manager.Atsc = atsc;
            switch (manager)
            {
                case BasicLightEffect blm:
                    blm.ColorScheme = colorScheme;
                    break;
                case ColorBoostEffect cbm:
                    cbm.ColorScheme = colorScheme;
                    break;
                case TrackLaneRingsRotationEffect tlrre:
                    tlrre.Effect.Manager.Atsc = atsc;
                    break;
            }

            manager.Initialize();
        }
    }

    public void Reinitialize()
    {
        foreach (var manager in EventTypeToEffects.Values.SelectMany(x => x).Distinct()) manager.Initialize();
    }

    public void Refresh()
    {
        foreach (var manager in EventTypeToEffects.Values.SelectMany(x => x).Distinct()) manager.Refresh();
    }

    public bool InsertData(BaseEvent data)
    {
        var marked = false;
        foreach (var effect in EventTypeToEffects.TryGetValue(data.Type, out var list) ? list : new())
        {
            effect.InsertData(data);
            marked = true;
        }

        return marked;
    }

    public bool InsertData(IEnumerable<BaseEvent> data) =>
        data.GroupBy(x => x.Type).Aggregate(false, (current, d) => current | InsertData(d.Key, d));

    public bool InsertData(int type, IEnumerable<BaseEvent> data)
    {
        var marked = false;
        data = data.ToList();
        foreach (var effect in (EventTypeToEffects.TryGetValue(type, out var list) ? list : new()))
        foreach (var evt in data)
        {
            effect.InsertData(evt);
            marked = true;
        }

        return marked;
    }

    public bool RemoveData(BaseEvent reference, BaseEvent original)
    {
        var marked = false;
        foreach (var effect in EventTypeToEffects.TryGetValue(original.Type, out var list) ? list : new())
        {
            effect.RemoveData(reference, original);
            marked = true;
        }

        return marked;
    }

    public T GetEffect<T>(int type) where T : StateManager<BaseEvent> =>
        EventTypeToEffects.TryGetValue(type, out var list) ? list.FirstOrDefault(x => x is T) as T : null;

    public IEnumerable<(int type, StateManager<BaseEvent> effect)> GetEffects() =>
        EventTypeToEffects.SelectMany(effects => effects.Value.Select(m => (effects.Key, m)));

    public IEnumerable<(int type, T effect)> GetEffects<T>() where T : StateManager<BaseEvent> =>
        GetEffects().Where(m => m.effect is T).Select(m => (m.type, m.effect as T));

    public T Register<T>(int type) where T : StateManager<BaseEvent>
    {
        var comp = gameObject.AddComponent<T>();
        return Register(type, comp);
    }

    public T GetOrRegister<T>(int type) where T : StateManager<BaseEvent>
    {
        var comp = GetEffect<T>(type);
        return comp == null ? Register<T>(type) : comp;
    }

    public T Register<T>(int type, T comp) where T : StateManager<BaseEvent>
    {
        AddToEntry(type, comp);
        comp.ID = type;
        return comp;
    }

    private void AddToEntry(int type, StateManager<BaseEvent> comp)
    {
        if (EventTypeToEffects.ContainsKey(type) && EventTypeToEffects[type].Contains(comp)) return;
        effectEntries.Add(new() { Type = type, Manager = comp });
        EventTypeToEffects.TryAdd(type, new List<StateManager<BaseEvent>>());
        EventTypeToEffects[type].Add(comp);
    }

    public void Register(LightController controller, bool strict = true)
    {
        if (effectEntries.Exists(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Register(controller, strict);
        }
        else
            throw new Exception("Could not find manager for type " + controller.Type);
    }

    public void Unregister(LightController controller)
    {
        if (effectEntries.Exists(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == controller.Type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Unregister(controller);
        }
        else
            throw new Exception("Could not find manager for type " + controller.Type);
    }
}

[Serializable]
public class BasicEventStateManagerEntry
{
    public int Type;
    public StateManager<BaseEvent> Manager;
}
