using Newtonsoft.Json;

public class InstancedMaterialLightWithIdComponent : EnvDataComponent<LightController>
{
    [JsonProperty("materialLightId")] public int ID = -1;

    [JsonProperty("materialLightIntensity")]
    public float Intensity = -1;

    public override void CopyTo(LightController target)
    {
    }
}
