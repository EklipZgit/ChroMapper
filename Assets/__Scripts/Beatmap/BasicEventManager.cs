using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    [SerializeField] private LightshowController lightshowController;

    public override void Refresh()
    {
    }

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(float time)
    {
        foreach (var manager in
            Context.Descriptor.BasicEventEffectManager.EventTypeToEffects.Values.SelectMany(managers =>
                managers))
            manager.UpdateTime(time);
    }

    protected override bool AddData(IEnumerable<BaseEvent> data) =>
        Context.Descriptor.BasicEventEffectManager.InsertData(data);

    protected override bool RemoveData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
            mark |= Context.Descriptor.BasicEventEffectManager.RemoveData(reference, original);
        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEvent> data) =>
        data.Aggregate(false, (current, d) => current | Context.Descriptor.BasicEventEffectManager.RemoveData(d, d));
}
