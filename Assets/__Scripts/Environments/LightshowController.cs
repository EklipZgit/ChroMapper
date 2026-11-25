using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class LightshowController : MonoBehaviour, IBeatmapUpdate
{
    public LightshowMode Mode;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private CameraManager cameraManager;

    private IBeatmapUpdate[] activeEffects = Array.Empty<IBeatmapUpdate>();
    private IEnvironmentComponentUpdate[] componentUpdates = Array.Empty<IEnvironmentComponentUpdate>();
    private LightCollision[] lightCollisions = Array.Empty<LightCollision>();

    private int activeSize;
    private int componentSize;

    private void Awake()
    {
        LoadInitialMap.OnLevelLoaded += RebuildLightshow;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged += RebuildLightshow;
        context.OnEnvironmentUnloaded += ClearEffectBindings;
    }

    private void Start()
    {
        Camera.onPreCull += HandleCameraPreCull;
        context.Atsc.OnTimeChanged += UpdateTime;
    }

    protected void OnDestroy()
    {
        Camera.onPreCull -= HandleCameraPreCull;
        LoadInitialMap.OnLevelLoaded -= RebuildLightshow;
        LoadedDifficultySelectController.OnLoadedDifficultyChanged -= RebuildLightshow;
        context.OnEnvironmentUnloaded -= ClearEffectBindings;
        context.Atsc.OnTimeChanged -= UpdateTime;
    }

    internal void ClearEffectBindings()
    {
        activeSize = 0;
        componentSize = 0;
        activeEffects = Array.Empty<IBeatmapUpdate>();
        componentUpdates = Array.Empty<IEnvironmentComponentUpdate>();
        lightCollisions = Array.Empty<LightCollision>();
    }

    private void UpdateTime()
    {
        if (Mode != LightshowMode.Full) return;
        UpdateTime(context.Atsc.IsPlaying, context.Atsc.CurrentSongBpmTime);
    }

    public void UpdateTime(bool isPlaying, float time)
    {
        for (var i = 0; i < activeSize; i++) activeEffects[i].UpdateTime(isPlaying, time);
    }

    public void LateUpdate()
    {
        for (var i = 0; i < componentSize; i++)
            if (componentUpdates[i].ShouldRefresh)
                componentUpdates[i].Refresh();
    }

    private void HandleCameraPreCull(Camera renderingCamera)
    {
        if (renderingCamera != cameraManager.SelectedCameraController.Camera)
            return;

        var physicsTransformsSynced = false;
        foreach (var collision in lightCollisions)
        {
            if (!collision.isActiveAndEnabled || !collision.ParametricLight.EnabledRenderers)
                continue;

            if (!physicsTransformsSynced)
            {
                Physics.SyncTransforms();
                physicsTransformsSynced = true;
            }

            collision.RefreshCollision();
        }
    }

    public void Refresh()
    {
        foreach (var effect in activeEffects) effect.Refresh();
    }

    internal void RebuildLightshow()
    {
        PopulateLightshow();
        PopulateEffects();
        UpdateTimeByMode();
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

        componentUpdates = context.Descriptor.GetComponentUpdates();
        componentSize = componentUpdates.Length;
        lightCollisions = context.Descriptor.GetComponentsInChildren<LightCollision>(true);
    }

    public void PopulateLightshow()
    {
        context.Descriptor.Reinitialize();

        var events = Mode == LightshowMode.Static
            ? context
                .TrackDefinitions.Basic.Where(track => track.Value.Kind == BasicEventKind.Lights)
                .Select(track =>
                {
                    var evt = new BaseEvent { Type = track.Key, songBpmTime = 0f, Value = 1 };
                    return evt;
                })
                .ToList()
            : Settings.Instance.Load_Events
                ? BeatSaberSongContainer.Instance.Map.Events
                : new List<BaseEvent>();

        context.Descriptor.BasicEventEffectManager.InsertData(events);
        context.Descriptor.LightColorGroupEffectManager.InsertData(
            Mode == LightshowMode.Static
                ? Enumerable.Empty<BaseLightColorEventBoxGroup>()
                : BeatSaberSongContainer.Instance.Map.LightColorEventBoxGroups);
        context.Descriptor.LightRotationGroupEffectManager.InsertData(
            Mode == LightshowMode.Static
                ? Enumerable.Empty<BaseLightRotationEventBoxGroup>()
                : BeatSaberSongContainer.Instance.Map.LightRotationEventBoxGroups);
        context.Descriptor.LightTranslationGroupEffectManager.InsertData(
            Mode == LightshowMode.Static
                ? Enumerable.Empty<BaseLightTranslationEventBoxGroup>()
                : BeatSaberSongContainer.Instance.Map.LightTranslationEventBoxGroups);
        context.Descriptor.FloatFxGroupEffectManager.InsertData(
            Mode == LightshowMode.Static
                ? Enumerable.Empty<BaseVfxEventEventBoxGroup>()
                : BeatSaberSongContainer.Instance.Map.VfxEventBoxGroups);
    }

    private void UpdateTimeByMode()
    {
        switch (Mode)
        {
            case LightshowMode.Full:
                UpdateTime();
                break;
            case LightshowMode.Static:
                UpdateTime(false, 0f);
                break;
            case LightshowMode.None:
                UpdateTime(false, -1f);
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

        Refresh();
        UpdateTimeByMode();
    }
}

public enum LightshowMode : byte
{
    Full,
    Static,
    None
}
