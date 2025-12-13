using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventEffectController : MonoBehaviour, IBeatmapUpdate
{
    [SerializeField] private List<StateManagerEntry> managerEntries = new();

    public readonly Dictionary<int, StateManager<BaseEvent>> EventTypeManagerMap = new();
    public StateManager<BaseEvent>[] Managers;
    private int size;

    private void Awake()
    {
        foreach (var managerEntry in managerEntries) EventTypeManagerMap.Add(managerEntry.Type, managerEntry.Manager);
        Managers = EventTypeManagerMap.Values.ToArray();
        size = Managers.Length;
    }

    private void Start()
    {
        var atsc = FindAnyObjectByType<AudioTimeSyncController>();
        foreach (var manager in Managers)
        {
            manager.Atsc = atsc;
            manager.Initialize();
        }
    }

    public void UpdateTime(float time)
    {
        for (var i = 0; i < size; i++) Managers[i].UpdateTime(time);
    }

    public bool TryInit<T>(int type) where T : StateManager<BaseEvent>
    {
        if (managerEntries.Exists(entry => entry.Type == type)) return false;
        var comp = gameObject.AddComponent<T>();
        managerEntries.Add(new() { Type = type, Manager = comp });
        return true;
    }

    public void Add(int type, BasicLightController controllable)
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
