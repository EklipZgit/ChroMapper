using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicEventManager : BeatmapObjectManager<BaseEvent>
{
    protected override bool AllowAction => localMode != LightshowMode.Static && Settings.Instance.Load_Events;
    private LightshowMode localMode;
    private PlatformDescriptor descriptor;

    protected override void Awake()
    {
        base.Awake();
        PlatformToggleLightshowMode.OnLightshowModeChanged += HandleLightshowModeChanged;
        localMode = PlatformToggleLightshowMode.Mode;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
            LoadInitialMap.OnPlatformLoaded += HandlePlatformLoaded;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        PlatformToggleLightshowMode.OnLightshowModeChanged -= HandleLightshowModeChanged;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
            LoadInitialMap.OnPlatformLoaded -= HandlePlatformLoaded;
    }

    private void HandlePlatformLoaded(PlatformDescriptor desc)
    {
        descriptor = desc;
        descriptor.OnRefreshed += RefreshLighting;
    }

    private void RefreshLighting()
    {
        PopulateLightshow();
        UpdateTimeByMode();

        Atsc.OnTimeChanged += UpdateTime;
    }

    public override void UpdateTime()
    {
        if (localMode != LightshowMode.Full) return;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(float time)
    {
        foreach (var manager in descriptor.SortedPriorityManagers) manager.UpdateTime(time);
    }

    private void PopulateLightshow()
    {
        foreach (var manager in descriptor.SortedPriorityManagers) manager.Initialize();

        var events = localMode == LightshowMode.Static
            ? descriptor
                .EventTypeManagerMap
                .Keys.Select(type =>
                {
                    var evt = new BaseEvent { Type = type, songBpmTime = 0f };
                    if (evt.IsLightEvent()) evt.Value = 1;
                    return evt;
                })
                .ToList()
            : Settings.Instance.Load_Events
                ? BeatSaberSongContainer.Instance.Map.Events
                : new();

        foreach (var (type, managers) in descriptor.EventTypeManagerMap)
            managers.ForEach(manager => manager.BuildFromData(events.Where(e => e.Type == type)));

        foreach (var manager in descriptor.SortedPriorityManagers) manager.Reset();
    }

    private void UpdateTimeByMode()
    {
        switch (localMode)
        {
            case LightshowMode.Full:
                UpdateTime();
                break;
            case LightshowMode.Static:
                UpdateTime(0f);
                break;
            case LightshowMode.None:
                UpdateTime(-1f);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void HandleLightshowModeChanged(LightshowMode mode)
    {
        // in the future, it should be possible to toggle during playback
        // but as of now, it causes race condition
        if (Atsc.IsPlaying || mode == localMode) return;
        var previousMode = localMode;
        localMode = mode;

        switch (mode)
        {
            case LightshowMode.Full:
                if (previousMode == LightshowMode.Static) PopulateLightshow();
                break;
            case LightshowMode.Static:
                if (previousMode != LightshowMode.Static) PopulateLightshow();
                break;
            case LightshowMode.None:
                if (previousMode == LightshowMode.Static) PopulateLightshow();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
        }

        UpdateTimeByMode();
    }

    protected override bool AddData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.EventTypeManagerMap.TryGetValue(d.Type, out var managers)) continue;
            managers.ForEach(manager => manager.InsertData(d));
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<(BaseEvent reference, BaseEvent original)> data)
    {
        var mark = false;
        foreach (var (reference, original) in data)
        {
            if (!descriptor.EventTypeManagerMap.TryGetValue(original.Type, out var managers)) continue;
            managers.ForEach(manager => manager.RemoveData(reference, original));
            mark = true;
        }

        return mark;
    }

    protected override bool RemoveData(IEnumerable<BaseEvent> data)
    {
        var mark = false;
        foreach (var d in data)
        {
            if (!descriptor.EventTypeManagerMap.TryGetValue(d.Type, out var managers)) continue;
            managers.ForEach(manager => manager.RemoveData(d, d));
            mark = true;
        }

        return mark;
    }
}
