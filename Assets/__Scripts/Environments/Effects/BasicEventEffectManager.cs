using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectManager : MonoBehaviour
{
    [SerializeField] public List<StateManagerEntry> managerEntries = new();

    public readonly Dictionary<int, StateManager<BaseEvent>> EventTypeManagerMap = new();
    private int size;

    private void Awake()
    {
        foreach (var managerEntry in managerEntries) EventTypeManagerMap.Add(managerEntry.Type, managerEntry.Manager);
    }

    public void Initialize(AudioTimeSyncController atsc, PlatformColorScheme colorScheme)
    {
        foreach (var manager in EventTypeManagerMap.Values)
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

    public bool TryInit<T>(int type) where T : StateManager<BaseEvent>
    {
        if (managerEntries.Exists(entry => entry.Type == type)) return false;
        var comp = gameObject.AddComponent<T>();
        managerEntries.Add(new() { Type = type, Manager = comp });
        return true;
    }

    public void Add(int type, LightController controllable)
    {
        if (managerEntries.Exists(entry => entry.Type == type && entry.Manager is BasicLightManager))
        {
            var manager = managerEntries.First(entry => entry.Type == type).Manager as BasicLightManager;
            manager.ControllableLights.Add(controllable);
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
