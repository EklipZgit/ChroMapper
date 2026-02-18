using System.Collections.Generic;
using Editor.Environments.Structures.Components;
using Newtonsoft.Json;

#nullable enable
/// <summary>
/// A single object in an environment, with its properties and components.
/// </summary>
public class EnvDataObject
{
    [JsonProperty("name")] public string GameObjectName = string.Empty;
    [JsonProperty("id")] public string ChromaID = string.Empty;
    public bool ActiveSelf;
    public string Layer = "Default";
    public EnvironmentComponents Components = new();

    // We can leave this to standard Newtonsoft.Json serialization.
    public class EnvironmentComponents
    {
        // Unity Components
        public TransformComponent[]? Transform;
        public BoxColliderComponent[]? BoxCollider;
        public SphereColliderComponent[]? SphereCollider;
        public MeshColliderComponent[]? MeshCollider;
        public MeshFilterComponent[]? MeshFilter;
        public MeshRendererComponent[]? MeshRenderer;

        // Lighting
        public DirectionalLightWithIdComponent[]? DirectionalLightWithId;
        public InstancedMaterialLightWithIdComponent[]? InstancedMaterialLightWithId;
        public MaterialLightWithIdComponent[]? MaterialLightWithId;
        public ParticleSystemLightWithIdComponent[]? ParticleSystemLightWithId;
        public RectangleFakeGlowLightWithIdComponent[]? RectangleFakeGlowLightWithLightId;
        public SpriteLightWithIdComponent[]? SpriteLightWithId;
        public TubeBloomPrePassLightWithIdComponent[]? TubeBloomPrePassLightWithId;

        // Runtime Lighting
        public DirectionalLightWithGroupIdsComponent[]? DirectionalLightWithGroupIds;
        public DirectionalLightWithIdsComponent[]? DirectionalLightWithIds;
        public MaterialLightWithIdsComponent[]? MaterialLightWithIds;
        public MixedLightsColorSetterRuntimeLightWithIdsComponent[]? MixedLightsColorSetterRuntimeLightWithIds;
        public PointLightWithIdsComponent[]? PointLightWithIds;

        // Controller
        public ParametricBoxControllerComponent[]? ParametricBoxController;
        public Parametric3SliceSpriteControllerComponent[]? Parametric3SliceSpriteController;

        // Basic
        public GameObjectIntSwitchEventEffectComponent[]? GameObjectIntSwitchEventEffect;
        public GameObjectSwitchEventEffectComponent[]? GameObjectSwitchEventEffect;
        public LightRotationEventEffectComponent[]? LightRotationEventEffect;
        public LightPairRotationEventEffectComponent[]? LightPairRotationEventEffect;
        public LightPairSinMoveEventEffectComponent[]? LightPairSinMoveEventEffect;
        public LightSwitchEventEffectComponent[]? LightSwitchEventEffect;
        public MeshRendererSwitchEventEffectComponent[]? MeshRendererSwitchEventEffect;
        public MovementBeatmapEventEffectComponent[]? MovementBeatmapEventEffect;
        public ParticleSystemContinuousEventEffectComponent[]? ParticleSystemContinuousEventEffect;
        public ParticleSystemEventEffectComponent[]? ParticleSystemEventEffect;
        public SmoothStepPositionEventEffectComponent[]? SmoothStepPositionEventEffect;
        public TrackLaneRingComponent[]? TrackLaneRing;
        public TrackLaneRingsManagerComponent[]? TrackLaneRingsManager;
        public TrackLaneRingsPositionStepEffectSpawnerComponent[]? TrackLaneRingsPositionStepEffectSpawner;
        public TrackLaneRingsRotationEffectComponent[]? TrackLaneRingsRotationEffect;
        public TrackLaneRingsRotationEffectSpawnerComponent[]? TrackLaneRingsRotationEffectSpawner;

        // MPB
        public MaterialPropertyBlockControllerComponent[]? MaterialPropertyBlockController;
        public MaterialPropertyBlockColorSetterComponent[]? MaterialPropertyBlockColorSetter;
        public MaterialPropertyBlockPositionUpdaterComponent[]? MaterialPropertyBlockPositionUpdater;

        // FX
        public AlphaFloatFxGroupEffectTargetComponent[]? AlphaFloatFxGroupEffectTarget;
        public ColliderEventEffectComponent[]? ColliderEventEffect;
        public FloatArrayMaterialPropertyEffectTargetComponent[]? FloatArrayMaterialPropertyEffectTarget;
        public FloatFxGroupEffectCollectionTargetComponent[]? FloatFxGroupEffectCollectionTarget;
        public FloatLocalScaleEffectComponent[]? FloatLocalScaleEffect;
        public FloatMaterialPropertyEffectTargetComponent[]? FloatMaterialPropertyEffectTarget;
        public MoveInDirectionEffectComponent[]? MoveInDirectionEffect;

        public Parametric3SliceSpriteWidthEndFloatFxEffectTargetComponent[]?
            Parametric3SliceSpriteWidthEndFloatFxEffectTarget;

        public StepFloatMaterialEffectTargetComponent[]? StepFloatMaterialEffectTarget;
        public SwitchGameObjectArrayEffectTargetComponent[]? SwitchGameObjectArrayEffectTarget;
        public SwitchGameObjectEffectTargetComponent[]? SwitchGameObjectEffectTarget;

        // Others
        public LightWithIdManagerComponent[]? LightWithIdManager;
        public RectangleFakeGlowComponent[]? RectangleFakeGlow;
        public TubeBloomPrePassLightCollisionComponent[]? TubeBloomPrePassLightCollisionEffect;
        public TubeBloomPrePassLightReflectionComponent[]? TubeBloomPrePassLightReflectionEffect;
        public CopyPositionComponent[]? CopyPosition;
        public PointLightComponent[]? PointLight;
        public DirectionalLightComponent[]? DirectionalLight;
        public LightManagerComponent[]? LightManager;

        // GLS
        public LightColorGroupComponent[]? LightColorGroup;
        public LightColorGroupEffectManagerComponent[]? LightColorGroupEffectManager;

        public LightRotationGroupComponent[]? LightRotationGroup;
        public LightRotationGroupEffectManagerComponent[]? LightRotationGroupEffectManager;

        public LightTranslationGroupComponent[]? LightTranslationGroup;
        public LightTranslationGroupEffectManagerComponent[]? LightTranslationGroupEffectManager;

        public FloatFxGroupComponent[]? FloatFxGroup;
        public FloatFxGroupEffectComponent[]? FloatFxGroupEffect;
        public FloatFxGroupEffectManagerComponent[]? FloatFxGroupEffectManager;
        public FloatFxGroupEffectManagerComponent[]? TriggerFloatFxGroupEffectManager;
    }
}
#nullable restore
