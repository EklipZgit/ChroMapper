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

    [JsonProperty("meshName")] public string? MeshName;

    [JsonProperty("layer")] public string Layer = "Default";

    [JsonProperty("components")] public EnvironmentComponents Components = new();

    // We can leave this to standard Newtonsoft.Json serialization.
    public class EnvironmentComponents
    {
        // Unity Components
        [JsonProperty("transform")] public TransformComponent[]? Transform;

        [JsonProperty("meshFilter")] public MeshFilterComponent[]? MeshFilter;

        [JsonProperty("meshRenderer")] public MeshRendererComponent[]? MeshRenderer;

        // Lighting Components
        [JsonProperty("instancedMaterialLightWithId")]
        public InstancedMaterialLightWithIdComponent[]? InstancedMaterialLightWithId;

        [JsonProperty("materialLightWithId")] public MaterialLightWithIdComponent[]? MaterialLightWithId;

        [JsonProperty("particleSystemLightWithId")]
        public ParticleSystemLightWithIdComponent[]? ParticleSystemLightWithID;

        [JsonProperty("spriteLightWithId")] public SpriteLightWithIdComponent[]? SpriteLightWithId;

        [JsonProperty("tubeBloomPrePassLightWithId")]
        public TubeBloomPrePassLightWithIdComponent[]? TubeBloomPrePassLightWithId;

        // Controller Components
        [JsonProperty("parametric3SliceSpriteController")]
        public Parametric3SliceSpriteControllerComponent[]? Parametric3SliceSpriteController;

        // Basic
        [JsonProperty("lightRotationEventEffect")]
        public LightRotationEventEffectComponent[]? LightRotationEventEffect;

        [JsonProperty("lightPairRotationEventEffect")]
        public LightPairRotationEventEffectComponent[]? LightPairRotationEventEffect;

        [JsonProperty("lightPairSinMoveEventEffect")]
        public LightPairSinMoveEventEffectComponent[]? LightPairSinMoveEventEffect;

        [JsonProperty("lightSwitchEventEffect")]
        public LightSwitchEventEffectComponent[]? LightSwitchEventEffect;

        [JsonProperty("particleSystemContinuousEventEffect")]
        public ParticleSystemContinuousEventEffectComponent[]? ParticleSystemContinuousEventEffect;

        [JsonProperty("particleSystemEventEffect")]
        public ParticleSystemEventEffectComponent[]? ParticleSystemEventEffect;

        [JsonProperty("trackLaneRing")] public TrackLaneRingComponent[]? TrackLaneRing;

        [JsonProperty("trackLaneRingsManager")]
        public TrackLaneRingsManagerComponent[]? TrackLaneRingsManager;

        [JsonProperty("trackLaneRingsPositionStepEffectSpawner")]
        public TrackLaneRingsPositionStepEffectSpawnerComponent[]? TrackLaneRingsPositionStepEffectSpawner;

        [JsonProperty("trackLaneRingsRotationEffect")]
        public TrackLaneRingsRotationEffectComponent[]? TrackLaneRingsRotationEffect;

        [JsonProperty("trackLaneRingsRotationEffectSpawner")]
        public TrackLaneRingsRotationEffectSpawnerComponent[]? TrackLaneRingsRotationEffectSpawner;

        // Others
        [JsonProperty("lightWithIdManager")] public LightWithIdManagerComponent[]? LightWithIdManager;

        public LightColorGroupComponent[]? LightColorGroup;
        public LightColorGroupEffectManagerComponent[]? LightColorGroupEffectManager;

        public LightRotationGroupComponent[]? LightRotationGroup;
        public LightRotationGroupEffectManagerComponent[]? LightRotationGroupEffectManager;

        public LightTranslationGroupComponent[]? LightTranslationGroup;
        public LightTranslationGroupEffectManagerComponent[]? LightTranslationGroupEffectManager;

        public FloatFxGroupComponent[]? FloatFxGroup;
        public FloatFxGroupEffectComponent[]? FloatFxGroupEffect;
        public FloatFxGroupEffectManagerComponent[]? FloatFxGroupEffectManager;
    }
}
#nullable restore
