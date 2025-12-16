using System.Collections.Generic;
using Beatmap.Base;

public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    private LightshowController lightshowController;
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
        foreach (var manager in descriptor.BasicEventEffectManager.EventTypeManagerMap.Values) manager.UpdateTime(time);
    }

    protected override bool AddData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeManagerMap.TryGetValue(d.Type, out var manager)) continue;
            manager.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeManagerMap.TryGetValue(original.Type, out var manager))
                continue;
            manager.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.BasicEventEffectManager.EventTypeManagerMap.TryGetValue(d.Type, out var manager)) continue;
            manager.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }
}
