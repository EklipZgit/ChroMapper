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

    protected void Awake()
    {
        LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += HandleLevelLoaded;
        context.OnEnvironmentChanged += HandleEnvironmentChanged;
        context.Atsc.OnTimeChanged += UpdateTime;
    }

    protected void OnDestroy()
    {
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= HandleLevelLoaded;
        context.OnEnvironmentChanged -= HandleEnvironmentChanged;
        context.Atsc.OnTimeChanged -= UpdateTime;
    }

    private void HandleEnvironmentChanged(EnvironmentDescriptor desc)
    {
        activeEffects = new List<IBeatmapUpdate>()
            .Concat(
                context
                    .Descriptor.BasicEventEffectManager.EventTypeToEffects.Values.SelectMany(x => x)
                    .Distinct())
            .Concat(context.Descriptor.LightColorGroupEffectManager.IdToEffect.Values)
            .Concat(context.Descriptor.LightRotationGroupEffectManager.IdToEffect.Values)
            .Concat(context.Descriptor.LightTranslationGroupEffectManager.IdToEffect.Values)
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
        UpdateTime(context.Atsc.CurrentSongBpmTime);
    }

    public void UpdateTime(float time)
    {
        for (var i = 0; i < activeSize; i++) activeEffects[i].UpdateTime(time);
    }

    private void UpdateLightshow(int type, IEnumerable<int> id)
    {
    }

    public void PopulateLightshow()
    {
        context.Descriptor.Refresh();

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

        foreach (var (type, effect) in context.Descriptor.BasicEventEffectManager.GetAllManagers().Distinct())
            effect.BuildFromData(events.Where(e => e.Type == type));

        foreach (var (id, effect) in context.Descriptor.LightColorGroupEffectManager.IdToEffect)
        {
            effect.BuildFromData(
                BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups.Where(g => g.ID == id));
        }

        foreach (var (id, effect) in context.Descriptor.LightRotationGroupEffectManager.IdToEffect)
        {
            effect.BuildFromData(
                BeatSaberSongContainer.Instance.Map.LightRotationEventBoxGroups.Where(g => g.ID == id));
        }

        foreach (var (id, effect) in context.Descriptor.LightTranslationGroupEffectManager.IdToEffect)
        {
            effect.BuildFromData(
                BeatSaberSongContainer.Instance.Map.LightTranslationEventBoxGroups.Where(g => g.ID == id));
        }

        foreach (var (id, effect) in context.Descriptor.FloatFxGroupEffectManager.IdToEffect)
        {
            effect.BuildFromData(
                BeatSaberSongContainer.Instance.Map.VfxEventBoxGroups.Where(g => g.ID == id));
        }

        foreach (var effect in activeEffects) effect.UpdateTime(context.Atsc.CurrentSongBpmTime);
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

        UpdateTimeByMode();
    }
}

public enum LightshowMode
{
    Full,
    Static,
    None,
}
