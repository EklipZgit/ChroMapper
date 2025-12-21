using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectManager : MonoBehaviour
{
    [SerializeField] private List<StateManagerEntry> managerEntries = new();

    public readonly Dictionary<int, StateManager<BaseEvent>[]> EventTypeToManagers = new();

    private void Awake()
    {
        foreach (var managers in managerEntries.OrderBy(x => x.Type).GroupBy(x => x.Type))
            EventTypeToManagers.Add(managers.First().Type, managers.Select(x => x.Manager).ToArray());
    }

    public void Initialize(AudioTimeSyncController atsc, ColorSchemeSO colorScheme)
    {
        foreach (var manager in EventTypeToManagers.Values.SelectMany(x => x))
        {
            manager.Atsc = atsc;
            manager.Initialize();
            switch (manager)
            {
                case BasicLightManager blm:
                    blm.ColorScheme = colorScheme;
                    break;
                case ColorBoostManager cbm:
                    cbm.ColorScheme = colorScheme;
                    break;
            }
        }
    }

    public IEnumerable<(int type, StateManager<BaseEvent> manager)> GetAllManagers() =>
        EventTypeToManagers.SelectMany((
            managers) => managers.Value.Select(m => (managers.Key, m)));

    public IEnumerable<(int type, T manager)> GetAllManagers<T>() where T : StateManager<BaseEvent> =>
        GetAllManagers().Where(m => m.manager is T).Select(m => (m.type, m.manager as T));

    public void Register<T>(int type) where T : StateManager<BaseEvent>
    {
        if (managerEntries.Exists(entry => entry.Type == type && entry.Manager is T)) return;
        var comp = gameObject.AddComponent<T>();
        managerEntries.Add(new() { Type = type, Manager = comp });
    }

    public void Register(int type, int id, LightController controllable)
    {
        if (managerEntries.Exists(entry => entry.Type == type && entry.Manager is BasicLightManager))
        {
            var manager = managerEntries
                .First(entry => entry.Type == type && entry.Manager is BasicLightManager)
                .Manager as BasicLightManager;
            manager!.Register(controllable, id);
        }
        else
            throw new Exception("Could not find manager for type " + type);
    }
}

[Serializable]
public class StateManagerEntry
{
    public int Type;
    public StateManager<BaseEvent> Manager;
}
