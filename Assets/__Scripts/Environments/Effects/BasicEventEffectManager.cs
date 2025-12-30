using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectManager : MonoBehaviour
{
    [SerializeField] private List<BasicEventStateManagerEntry> effectEntries = new();

    public readonly Dictionary<int, StateManager<BaseEvent>[]> EventTypeToEffects = new();

    private void Awake()
    {
        foreach (var managers in effectEntries.OrderBy(x => x.Type).GroupBy(x => x.Type))
            EventTypeToEffects.Add(managers.First().Type, managers.Select(x => x.Manager).ToArray());
    }

    public void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var manager in EventTypeToEffects.Values.SelectMany(x => x))
        {
            manager.Atsc = atsc;
            manager.Initialize();
            switch (manager)
            {
                case BasicLightEffect blm:
                    blm.ColorScheme = colorScheme;
                    break;
                case ColorBoostEffect cbm:
                    cbm.ColorScheme = colorScheme;
                    break;
                case TrackLaneRingsPositionEffect tlrpe:
                    tlrpe.Manager.Atsc = atsc;
                    break;
                case TrackLaneRingsRotationEffect tlrre:
                    tlrre.Effect.Manager.Atsc = atsc;
                    break;
            }
        }
    }

    public IEnumerable<(int type, StateManager<BaseEvent> manager)> GetAllManagers() =>
        EventTypeToEffects.SelectMany(managers => managers.Value.Select(m => (managers.Key, m)));

    public IEnumerable<(int type, T manager)> GetAllManagers<T>() where T : StateManager<BaseEvent> =>
        GetAllManagers().Where(m => m.manager is T).Select(m => (m.type, m.manager as T));

    public T Register<T>(int type) where T : StateManager<BaseEvent>
    {
        var comp = gameObject.AddComponent<T>();
        effectEntries.Add(new() { Type = type, Manager = comp });
        return comp;
    }

    public void Register<T>(int type, T comp) where T : StateManager<BaseEvent>
    {
        effectEntries.Add(new() { Type = type, Manager = comp });
        if (comp.Types.Contains(type)) return;
        comp.AutoRegister = true;
        comp.Types.Add(type);
    }

    public void Register(int type, int id, BaseLightController controller)
    {
        if (effectEntries.Exists(entry => entry.Type == type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Register(controller, id);
        }
        else
            throw new Exception("Could not find manager for type " + type);
    }
}

[Serializable]
public class BasicEventStateManagerEntry
{
    public int Type;
    public StateManager<BaseEvent> Manager;
}
