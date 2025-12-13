using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class LightshowController : MonoBehaviour, IBeatmapUpdate
{
    public AudioTimeSyncController Atsc;
    public LightshowMode Mode;

    private PlatformDescriptor descriptor;
    private IBeatmapUpdate[] activeEffects = Array.Empty<IBeatmapUpdate>();
    private int activeSize;

    protected void Awake()
    {
        LoadInitialMap.OnPlatformLoaded += HandlePlatformLoaded;
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        Atsc.OnTimeChanged += UpdateTime;
    }

    protected void OnDestroy()
    {
        LoadInitialMap.OnPlatformLoaded -= HandlePlatformLoaded;
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        Atsc.OnTimeChanged -= UpdateTime;
    }

    private void HandlePlatformLoaded(PlatformDescriptor desc)
    {
        descriptor = desc;
        activeEffects = (new List<IBeatmapUpdate> { descriptor.basicEventEffectController })
            .Where(x => x != null)
            .ToArray();
        activeSize = activeEffects.Length;
    }

    private void HandleLevelLoaded()
    {
        PopulateLightshow();
        UpdateTimeByMode();
    }

    private void UpdateTime()
    {
        if (Mode != LightshowMode.Full) return;
        UpdateTime(Atsc.CurrentSongBpmTime);
    }

    public void UpdateTime(float time)
    {
        for (var i = 0; i < activeSize; i++) activeEffects[i].UpdateTime(time);
    }

    private void PopulateLightshow()
    {
        // descriptor.Initialize();

        var events = Mode == LightshowMode.Static
            ? descriptor
                .basicEventEffectController
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

        foreach (var (type, manager) in descriptor.basicEventEffectController.EventTypeManagerMap)
            manager.BuildFromData(events.Where(e => e.Type == type));

        foreach (var manager in descriptor.basicEventEffectController.Managers) manager.UpdateDirty();
    }

    private void UpdateTimeByMode()
    {
        switch (Mode)
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

    public void SetMode(LightshowMode mode)
    {
        // in the future, it should be possible to toggle during playback
        // but as of now, it causes race condition
        if (Atsc.IsPlaying || mode == Mode) return;
        var previousMode = Mode;
        Mode = mode;

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
}

public enum LightshowMode
{
    Full,
    Static,
    None,
}
