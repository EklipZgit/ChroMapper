using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction =>
        lightshowController.Mode != LightshowMode.Static && Settings.Instance.Load_Events;

    // Basic-light states depend on neighboring events, so collection edits need a final full-cache rebuild.
    protected override bool RefreshAfterModifiedCollection => true;

    // Rebuilding avoids transient state removals against light IDs that have just been mirrored to another lane.
    protected override bool RebuildOnlyForModifiedCollection => true;

    [SerializeField] private LightshowController lightshowController;

    public override void Refresh()
    {
        // Rebuild from the final map data so a bulk metadata edit cannot retain intermediate event states.
        var map = BeatSaberSongContainer.Instance?.Map;
        if (map == null) return;
        Context.Descriptor.BasicEventEffectManager.Reinitialize();
        Context.Descriptor.BasicEventEffectManager.InsertData(map.Events);
    }

    public override void UpdateTime()
    {
        if (lightshowController.Mode != LightshowMode.Full) return;
        UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var manager in
            Context.Descriptor.BasicEventEffectManager.EventTypeToEffects.Values.SelectMany(managers =>
                managers))
            manager.UpdateTime(isPlaying, time);
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

    /// <summary>
    /// Default implementation of UpdateData for basic events.
    /// Basic events don't have time-based caching like GLS groups, so this uses the
    /// RemoveData/AddData pattern which is sufficient for basic event updates.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        var b = RemoveData(data);
        return AddData(data.Select(d => d.Item1)) || b;
    }
}
