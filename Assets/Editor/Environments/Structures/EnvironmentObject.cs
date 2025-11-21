using System.Collections.Generic;
using Editor.Environments.Structures.Components;
using Newtonsoft.Json;

#nullable enable
/// <summary>
/// A single object in an environment, with its properties and components.
/// </summary>
public class EnvironmentObject
{
    [JsonProperty("name")]
    public string GameObjectName = string.Empty;

    [JsonProperty("id")]
    public string ChromaID = string.Empty;

    [JsonProperty("meshName")]
    public string? MeshName;

    [JsonProperty("layer")] 
    public string? Layer;

    [JsonProperty("components")]
    public EnvironmentComponents Components = new();

    // We can leave this to standard Newtonsoft.Json serialization.
    public class EnvironmentComponents
    {
        // Basic Components
        [JsonProperty("transform")]
        public TransformComponent? Transform;
        
        // Lighting Components
        [JsonProperty("tubeBloomPrePassLightWithId")]
        public List<TubeBloomPrePassLightWithIdComponent>? TubeBloomPrePassLightWithId;
        [JsonProperty("spriteLightWithId")]
        public SpriteLightWithIdComponent? SpriteLightWithId;

        [JsonProperty("trackLaneRingsManager")]
        public TrackLaneRingsManagerComponent? TrackLaneRingsManager;

        /*
        public FloatFxGroupComponent? FloatFxGroup;
        public FloatFxGroupEffectComponent? FloatFxGroupEffect;
        public FloatFxGroupEffectManagerComponent? FloatFxGroupEffectManager;

        public LightColorGroupComponent? LightColorGroup;
        public LightColorGroupEffectComponent? LightColorGroupEffect;
        public LightColorGroupEffectManagerComponent? LightColorGroupEffectManager;

        public LightRotationGroupComponent? LightRotationGroup;
        public LightRotationGroupEffectComponent? LightRotationGroupEffect;
        public LightRotationGroupEffectManagerComponent? LightRotationGroupEffectManager;

        public LightTranslationGroupComponent? LightTranslationGroup;
        public LightTranslationGroupEffectManagerComponent? LightTranslationGroupEffectManager;
        */
    }
}
#nullable restore
