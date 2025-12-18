using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    [SerializeField] private LightshowController lightshowController;
    private PlatformDescriptor descriptor;

    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnPlatformLoaded += HandlePlatformLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnPlatformLoaded -= HandlePlatformLoaded;
    }

    private void HandlePlatformLoaded(PlatformDescriptor desc) => descriptor = desc;

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(float time)
    {
        foreach (var manager in descriptor.BasicEventEffectManager.EventTypeToManagers.Values.SelectMany(managers =>
            managers))
            manager.UpdateTime(time);
    }

    protected override bool AddData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeToManagers.TryGetValue(d.Type, out var managers)) continue;
            foreach (var manager in managers) manager.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeToManagers.TryGetValue(original.Type, out var managers))
                continue;
            foreach (var manager in managers) manager.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeToManagers.TryGetValue(d.Type, out var managers)) continue;
            foreach (var manager in managers) manager.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }
}
