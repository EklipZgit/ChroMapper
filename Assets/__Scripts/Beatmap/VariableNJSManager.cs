using System.Collections.Generic;
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
        Atsc.OnTimeChangedEarly -= UpdateTime;
    }

    public override void UpdateTime() => UpdateTime(Atsc.CurrentSongBpmTime);
    public override void UpdateTime(float beatTime) => provider.UpdateTime(beatTime);

    private void Refresh()
    {
        provider.Initialize();
        provider.BaseNjs = BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        provider.CurrentNjs = provider.BaseNjs;
        provider.BuildFromData(BeatSaberSongContainer.Instance.Map.NJSEvents);

        Atsc.OnTimeChangedEarly += UpdateTime;
    }

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
        foreach (var (reference, _) in data)
        {
            provider.RemoveData(reference);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseNJSEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            provider.RemoveData(d);
            mark = true;
        }

        return mark;
    }
}
