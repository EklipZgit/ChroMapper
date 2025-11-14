using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Editor.Environments.Structures.Components
{
    public class TubeBloomPrePassLightWithIdComponent : EnvironmentComponent<LightingObject>
    {
        [JsonProperty("tubeBloomPrePassLight")] [CanBeNull]
        public TubeBloomPrePassLightComponent TubeBloomPrePassLight;
        [JsonProperty("chromaLight")] [CanBeNull] 
        public ChromaLightComponent ChromaLight;

        public override void CopyTo(LightingObject target)
        {
        }
    }

    public class TubeBloomPrePassLightComponent
    {
        [JsonProperty("colorAlphaMultiplier")]
        public float ColorAlphaMultiplier = 1f;
        [JsonProperty("bloomFogIntensityMultiplier")]
        public float BloomFogIntensityMultiplier = 1f;
        [JsonProperty("tubeLength")]
        public float TubeLength = 1f;
        [JsonProperty("tubeWidth")]
        public float TubeWidth = 1f;
        [JsonProperty("center")]
        public float Center = 1f;
        [JsonProperty("height")]
        public float Height = 1f;
        [JsonProperty("startAlpha")]
        public float StartAlpha = 0f;
        [JsonProperty("endAlpha")]
        public float EndAlpha = 1f;
        [JsonProperty("lightWidthMultiplier")]
        public float LightWidthMultiplier = 1f;
        [JsonProperty("useCollision")]
        public bool UseCollision = false;
        [JsonProperty("parametricBoxId")]
        public string ParametricBoxId = "";
    }
}
