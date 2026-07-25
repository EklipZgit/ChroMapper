using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class VariableNJSManager : BeatmapObjectManager<BaseNJSEvent>
{
    [SerializeField] private VariableNJSProvider provider;

    protected override void Awake()
    {
        base.Awake();
        LoadInitialMap.OnLevelLoaded += Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += Refresh;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= Refresh;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= Refresh;
        Context.Atsc.OnTimeChangedEarly -= UpdateTime;
    }

    public override void Refresh()
    {
        provider.Initialize();
        BeatSaberSongContainer.Instance.Map.NJSEvents.ForEach(provider.InsertData);

        Context.Atsc.OnTimeChangedEarly += UpdateTime;
    }

    public override void UpdateTime() => UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    public override void UpdateTime(bool isPlaying, float beatTime) => provider.UpdateTime(isPlaying, beatTime);

    protected override bool AddData(IEnumerable<BaseNJSEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            provider.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseNJSEvent reference, BaseNJSEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            provider.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseNJSEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            provider.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }

    /// <summary>
    /// Default implementation of UpdateData for NJS events.
    /// NJS events don't have time-based caching like GLS groups, so this uses the
    /// RemoveData/AddData pattern which is sufficient for NJS event updates.
    /// </summary>
    protected override bool UpdateData(IEnumerable<(BaseNJSEvent reference, BaseNJSEvent original)> data)
    {
        var b = RemoveData(data);
        return AddData(data.Select(d => d.Item1)) || b;
    }
}
