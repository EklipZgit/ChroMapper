using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using ZLinq;

public class RotationObjectManager : BeatmapObjectManager<BaseObject>
{
    [SerializeField] private LaneRotationProvider provider;

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
        BeatSaberSongContainer.Instance.Map.RotationEvents.ForEach(provider.InsertData);

        Context.Atsc.OnTimeChangedEarly += UpdateTime;
    }

    public override void UpdateTime() => UpdateTime(Context.Atsc.IsPlaying, Context.Atsc.CurrentSongBpmTime);
    public override void UpdateTime(bool isPlaying, float beatTime) => provider.UpdateTime(isPlaying, beatTime);


    private static bool FilterObjectRotation(BaseObject data)
    {
        switch (BeatSaberSongContainer.Instance.Map.MajorVersion)
        {
            case >= 4 when data is not BaseGrid:
            case < 4 when data is not BaseRotationEvent:
                return false;
        }

        return true;
    }

    private static bool FilterObjectRotationPair((BaseObject reference, BaseObject original) data) =>
        FilterObjectRotation(data.reference);

    protected override bool AddData(IEnumerable<BaseObject> data)
    {
        var mark = false;
        foreach (var d in data.AsValueEnumerable().Where(FilterObjectRotation))
        {
            provider.InsertData(d);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseObject reference, BaseObject original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data.AsValueEnumerable().Where(FilterObjectRotationPair))
        {
            provider.RemoveData(reference, original);
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseObject> data)
    {
        var mark = false;
        foreach (var d in data.AsValueEnumerable().Where(FilterObjectRotation))
        {
            provider.RemoveData(d, d);
            mark = true;
        }

        return mark;
    }
}
