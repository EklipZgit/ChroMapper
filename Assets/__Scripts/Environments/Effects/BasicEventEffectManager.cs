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
            }
        }
    }

    public IEnumerable<(int type, StateManager<BaseEvent> manager)> GetAllManagers() =>
        EventTypeToEffects.SelectMany((
            managers) => managers.Value.Select(m => (managers.Key, m)));

    public IEnumerable<(int type, T manager)> GetAllManagers<T>() where T : StateManager<BaseEvent> =>
        GetAllManagers().Where(m => m.manager is T).Select(m => (m.type, m.manager as T));

    public void Register<T>(int type) where T : StateManager<BaseEvent>
    {
        if (effectEntries.Exists(entry => entry.Type == type && entry.Manager is T)) return;
        var comp = gameObject.AddComponent<T>();
        effectEntries.Add(new() { Type = type, Manager = comp });
    }

    public void Register(int type, int id, LightController controllable)
    {
        if (effectEntries.Exists(entry => entry.Type == type && entry.Manager is BasicLightEffect))
        {
            var manager = effectEntries
                .First(entry => entry.Type == type && entry.Manager is BasicLightEffect)
                .Manager as BasicLightEffect;
            manager!.Register(controllable, id);
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
