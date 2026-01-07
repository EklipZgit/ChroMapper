using JetBrains.Annotations;
using Newtonsoft.Json;

public class TubeBloomPrePassLightWithIdComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    [JsonProperty("tubeBloomPrePassLight")] [CanBeNull]
    public TubeBloomPrePassLightComponent TubeBloomPrePassLight;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
    }
}

public class TubeBloomPrePassLightComponent
{
    [JsonProperty("colorAlphaMultiplier")] public float ColorAlphaMultiplier = 1f;

    [JsonProperty("bloomFogIntensityMultiplier")]
    public float BloomFogIntensityMultiplier = 1f;

    [JsonProperty("tubeLength")] public float TubeLength = 1f;
    [JsonProperty("tubeWidth")] public float TubeWidth = 1f;
    [JsonProperty("center")] public float Center = 1f;
    [JsonProperty("height")] public float Height = 1f;
    [JsonProperty("startAlpha")] public float StartAlpha = 0f;
    [JsonProperty("endAlpha")] public float EndAlpha = 1f;
    [JsonProperty("lightWidthMultiplier")] public float LightWidthMultiplier = 1f;
    [JsonProperty("useCollision")] public bool UseCollision = false;

    [JsonProperty("startWidth")] public float StartWidth;
    [JsonProperty("endWidth")] public float EndWidth;
    [JsonProperty("boostToWhite")] public float BoostToWhite;
    [JsonProperty("limitAlpha")] public float LimitAlpha;
    [JsonProperty("minAlpha")] public float MinAlpha;
    [JsonProperty("maxAlpha")] public float MaxAlpha;

    [JsonProperty("parametricBoxId")] public string ParametricBoxId = "";

    [JsonProperty("sliceSpriteControllerId")]
    public string SliceSpriteControllerId = "";
}
