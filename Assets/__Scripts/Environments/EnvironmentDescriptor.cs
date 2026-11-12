using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentDescriptor : MonoBehaviour
{
    public string ID;

    [SerializeField] public BasicEventEffectManager BasicEventEffectManager;
    [SerializeField] public LightColorGroupEffectManager LightColorGroupEffectManager;
    [SerializeField] public LightRotationGroupEffectManager LightRotationGroupEffectManager;
    [SerializeField] public LightTranslationGroupEffectManager LightTranslationGroupEffectManager;
    [SerializeField] public FloatFxGroupEffectManager FloatFxGroupEffectManager;

    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;
    [SerializeField] public SpectrogramDataProvider SpectrogramDataProvider;

    [SerializeField] public EnvironmentSizeData SizeData = new();
    [SerializeField] public BloomFogParams BloomFogParams = new();

    public List<ChromaIDMarker> ChromaIDMarkers = new();

    [Serializable]
    public struct NativeSiblingCount
    {
        public string ParentId;
        public int ChildCount;
    }

    public List<NativeSiblingCount> NativeSiblingCounts = new();
    private readonly Dictionary<string, int> nextNativeSiblingIndices = new(StringComparer.Ordinal);

    private bool hasInitialized;
    private IEnvironmentComponentUpdate[] componentUpdates;
    private AudioTimeSyncController timelineClock;

    internal EnvironmentEnhancementState EnhancementState;

    public void Initialize(BeatmapRuntimeContext context)
    {
        timelineClock = context.Atsc;
        InitializeTimelineParticles();
        InitializeTimelineAnimations();
        ResetCloneSiblingIndices();

        // TODO: do proper batch update
        if (hasInitialized)
        {
            componentUpdates = componentUpdates
                .Concat(GetComponentsInChildren<IEnvironmentComponentUpdate>(true))
                .Distinct()
                .ToArray();
        }
        else
            componentUpdates = GetComponentsInChildren<IEnvironmentComponentUpdate>(true);

        hasInitialized = true;
        Shader.SetGlobalFloat("_TrackLaneYPosition", SizeData.TrackLaneType == TrackLaneType.None ? -100f : 0f);

        BasicEventEffectManager.Initialize(context.Atsc);
        LightColorGroupEffectManager.Initialize(context.Atsc);
        LightRotationGroupEffectManager.Initialize(context.Atsc);
        LightTranslationGroupEffectManager.Initialize(context.Atsc);
        FloatFxGroupEffectManager.Initialize(context.Atsc);

        ColorSchemeProvider.Initialize(context);
        foreach (var controller in componentUpdates.OfType<RectangleFakeGlowLightController>())
            controller.ApplyInitialColorScheme(ColorSchemeProvider);
        SpectrogramDataProvider.AudioLink = context.AudioLink;
        // Environments without audio-driven meshes must not pay for spectrum preparation.
        if (GetComponentInChildren<Spectrogram>(true) != null
            || GetComponentInChildren<TransformSpectrogram>(true) != null
            || GetComponentInChildren<SpectrogramRowPropertyAnimator>(true) != null)
        {
            SpectrogramDataProvider.Initialize(context.Atsc);
        }

        BasicLightEffect.FlashTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FlashTimeSecond);
        BasicLightEffect.FadeTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FadeTimeSecond);
    }

    public void Reinitialize()
    {
        BasicEventEffectManager.Reinitialize();
        LightColorGroupEffectManager.Reinitialize();
        LightRotationGroupEffectManager.Reinitialize();
        LightTranslationGroupEffectManager.Reinitialize();
        FloatFxGroupEffectManager.Reinitialize();
    }

    public void Refresh()
    {
        BasicEventEffectManager.Refresh();
        LightColorGroupEffectManager.Refresh();
        LightRotationGroupEffectManager.Refresh();
        LightTranslationGroupEffectManager.Refresh();
        FloatFxGroupEffectManager.Refresh();
    }


    // CM removes unsupported objects and adds effect managers, so Unity sibling indices no longer match Chroma IDs.
    internal int AllocateCloneSiblingIndex(string parentId)
    {
        var index = nextNativeSiblingIndices.GetValueOrDefault(parentId);
        nextNativeSiblingIndices[parentId] = index + 1;
        return index;
    }

    internal void ResetCloneSiblingIndices()
    {
        nextNativeSiblingIndices.Clear();
        foreach (var entry in NativeSiblingCounts)
            nextNativeSiblingIndices.Add(entry.ParentId, entry.ChildCount);

    }

    internal void CopyCloneChildCount(string sourceId, string cloneId)
    {
        if (nextNativeSiblingIndices.TryGetValue(sourceId + ".", out var count))
        {
            nextNativeSiblingIndices[cloneId + "."] = count;
        }
    }

    public void Register(LightController controller, bool strict = true)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                BasicEventEffectManager.Register(controller, strict);
                break;
            case LightController.LightKind.Group:
                LightColorGroupEffectManager.Register(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Register(LightController controller, int? requestedKey)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                BasicEventEffectManager.Register(controller, requestedKey);
                break;
            case LightController.LightKind.Group:
                LightColorGroupEffectManager.Register(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Unregister(LightController controller)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                BasicEventEffectManager.Unregister(controller);
                break;
            case LightController.LightKind.Group:
                LightColorGroupEffectManager.Unregister(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public IEnvironmentComponentUpdate[] GetComponentUpdates()
    {
        InitializeTimelineParticles();
        InitializeTimelineAnimations();
        // AssignTrackParent moves objects outside this hierarchy, so retain scene-owned originals and collect enhancement clones separately.
        return componentUpdates
            .Concat(EnhancementState?.ComponentUpdates ?? Array.Empty<IEnvironmentComponentUpdate>())
            .Distinct()
            .Where(component => component.ShouldInclude)
            .ToArray();
    }

    private void InitializeTimelineParticles()
    {
        // Emission callbacks own event-triggered systems, so they must not also run as continuous ambient effects.
        var eventParticles = new HashSet<ParticleSystem>();
        foreach (var controller in GetComponentsInChildren<ParticleSystemContinuous>(true))
            foreach (var particles in controller.ParticleSystems)
                eventParticles.Add(particles);
        foreach (var visibility in GetComponentsInChildren<WorldParticleVisibility>(true))
        {
            var particles = visibility.GetComponent<ParticleSystem>();
            var main = particles.main;
            if (!eventParticles.Contains(particles) && (visibility.IsSmoke || (main.loop && main.playOnAwake)))
            {
                // World-space particles bake their emitter pose, so update camera followers before their clock callback.
                if (particles.TryGetComponent<FollowCamera>(out var follower))
                    follower.Initialize(timelineClock);
                visibility.gameObject.GetOrAddComponent<TimelineParticleSystem>().Initialize(timelineClock);
            }
        }
    }

    private void InitializeTimelineAnimations()
    {
        foreach (var animation in GetComponentsInChildren<TimelineLegacyAnimation>(true))
        {
            if (animation.enabled)
                animation.Initialize(timelineClock);
        }
    }
}
