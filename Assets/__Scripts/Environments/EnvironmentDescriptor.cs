using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
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

    public void PrepareForUnload()
    {
        // Mirrors can reference planes on other environment objects. Stop their callbacks before track
        // cleanup destroys those planes, while leaving animators registered until cleanup releases them.
        foreach (var root in gameObject.scene.GetRootGameObjects())
        {
            foreach (var mirror in root.GetComponentsInChildren<PlanarReflection>(true))
                mirror.enabled = false;
        }
    }

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

        // Scene builds skip lights under objects that start inactive, but GameObjectSwitch enables
        // such hierarchies at runtime (Billie's DayAndNight.Night). Register them before the effects
        // initialize so adoption, lane tables, and the event insert cover them like every other
        // light; toggles then only need SetActive while the lights keep receiving states.
        RegisterSwitchToggledLights();
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

    // Each event-box type can have a different number of targets for the same group ID.
    public int GetGlsGroupSize(BaseEventBoxGroup group) => group switch
    {
        BaseLightColorEventBoxGroup => LightColorGroupEffectManager.IdToEffect
            .TryGetValue(group.ID, out var colorEffect) ? colorEffect.Count : 0,
        BaseLightRotationEventBoxGroup => LightRotationGroupEffectManager.IdToEffect
            .TryGetValue(group.ID, out var rotationEffect) ? rotationEffect.Count : 0,
        BaseLightTranslationEventBoxGroup => LightTranslationGroupEffectManager.IdToEffect
            .TryGetValue(group.ID, out var translationEffect) ? translationEffect.Count : 0,
        BaseVfxEventEventBoxGroup => FloatFxGroupEffectManager.IdToEffect
            .TryGetValue(group.ID, out var floatFxEffect) ? floatFxEffect.Count : 0,
        _ => 0
    };

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

    public bool IsRegistered(LightController controller)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                return BasicEventEffectManager.IsRegistered(controller);
            case LightController.LightKind.Group:
                return LightColorGroupEffectManager.IsRegistered(controller);
            default:
                return false;
        }
    }

    // Scene builds skip lights under objects that start inactive, but runtime activators enable
    // such hierarchies: GameObjectSwitch boost/deactivate lists (Billie's DayAndNight.Night) and
    // Chroma _active enhancements (GeometryContainer calls this same helper). Registering them
    // before adoption/lane-table rebuilds keeps the event insert covering them like every other
    // light; toggles then only need SetActive while the lights keep receiving states. Group-kind
    // controllers only register when their group id exists in this environment.
    private void RegisterSwitchToggledLights()
    {
        foreach (var gameObjectSwitch in GetComponentsInChildren<GameObjectSwitch>(true))
        {
            foreach (var target in gameObjectSwitch.NormalGameObjects.Concat(gameObjectSwitch.BoostGameObjects))
                RegisterUnregisteredLights(target);
        }
    }

    internal void RegisterUnregisteredLights(GameObject root)
    {
        if (root == null) return;
        foreach (var controller in root.GetComponentsInChildren<LightController>(true))
        {
            if (IsRegistered(controller)) continue;
            switch (controller.Kind)
            {
                case LightController.LightKind.Basic:
                    // The export's catch-all serializes skipped lights as ID -1; a valid ID on an
                    // unregistered controller belongs to a clone nested inside the swept target,
                    // and strict registration would steal its source's occupied index.
                    if (controller.ID == -1)
                        BasicEventEffectManager.Register(controller);
                    break;
                case LightController.LightKind.Group:
                    if (LightColorGroupEffectManager.IdToEffect.ContainsKey(controller.Type))
                        LightColorGroupEffectManager.Register(controller);
                    break;
            }
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
