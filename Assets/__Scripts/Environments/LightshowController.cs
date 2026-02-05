using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class LightshowController : MonoBehaviour, IBeatmapUpdate
{
    public LightshowMode Mode;
    [SerializeField] private BeatmapRuntimeContext context;

    private IBeatmapUpdate[] activeEffects = Array.Empty<IBeatmapUpdate>();
    private int activeSize;

    private void Awake()
    {
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += HandleLevelLoaded;
        context.OnEnvironmentLoaded += HandleEnvironmentLoaded;
        context.OnEnvironmentUnloaded += HandleEnvironmentUnloaded;
    }

    protected void OnDestroy()
    {
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= HandleLevelLoaded;
        context.OnEnvironmentLoaded -= HandleEnvironmentLoaded;
        context.OnEnvironmentUnloaded -= HandleEnvironmentUnloaded;
    }

    private void HandleEnvironmentUnloaded()
    {
        context.Atsc.OnTimeChanged -= UpdateTime;
        activeEffects = Array.Empty<IBeatmapUpdate>();
    }

    private void HandleEnvironmentLoaded(EnvironmentDescriptor _)
    {
        PopulateEffects();
        context.Atsc.OnTimeChanged += UpdateTime;
    }

    private void HandleLevelLoaded()
    {
        PopulateEffects();
        PopulateLightshow();
    }

    private void UpdateTime()
    {
        if (Mode != LightshowMode.Full) return;
        UpdateTime(context.Atsc.CurrentSongBpmTime);
    }

    public void UpdateTime(float time)
    {
        for (var i = 0; i < activeSize; i++) activeEffects[i].UpdateTime(time);
    }

    public void Refresh()
    {
        foreach (var effect in activeEffects) effect.Refresh();
    }

    private void PopulateEffects()
    {
        activeEffects = new List<IBeatmapUpdate>()
            .Concat(
                context
                    .Descriptor.BasicEventEffectManager.EventTypeToEffects.Values.SelectMany(x => x)
                    .Distinct())
            .Concat(context.Descriptor.LightColorGroupEffectManager.IdToEffect.Values)
            .Concat(context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Values)
            .Concat(context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Values)
            .Concat(context.Descriptor.FloatFxGroupEffectManager.IdToEffect.Values)
            .Where(x => x != null)
            .ToArray();
        activeSize = activeEffects.Length;
    }

    public void PopulateLightshow()
    {
        context.Descriptor.Reinitialize();

        var events = Mode == LightshowMode.Static
            ? context
                .TracksDefinition.Basic.Where(track => track.Value.Kind == BasicEventKind.Lights)
                .Select(track =>
                {
                    var evt = new BaseEvent { Type = track.Key, songBpmTime = 0f, Value = 1 };
                    return evt;
                })
                .ToList()
            : Settings.Instance.Load_Events
                ? BeatSaberSongContainer.Instance.Map.Events
                : new();

        context.Descriptor.BasicEventEffectManager.InsertData(events);
        context.Descriptor.LightColorGroupEffectManager.InsertData(
            BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups);
        context.Descriptor.LightRotationGroupEffectManager.InsertData(
            BeatSaberSongContainer.Instance.Map.LightRotationEventBoxGroups);
        context.Descriptor.LightTranslationGroupEffectManager.InsertData(
            BeatSaberSongContainer.Instance.Map.LightTranslationEventBoxGroups);
        context.Descriptor.FloatFxGroupEffectManager.InsertData(
            BeatSaberSongContainer.Instance.Map.VfxEventBoxGroups);

        UpdateTimeByMode();
        context.Descriptor.Refresh();
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
        if (context.Atsc.IsPlaying || mode == Mode) return;
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
    }
}

public enum LightshowMode : byte
{
    Full,
    Static,
    None,
}
