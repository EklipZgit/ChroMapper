using Newtonsoft.Json;

public class InstancedMaterialLightWithIdComponent : EnvDataComponent<LightController>
{
    [JsonProperty("lightId")] public int ID;
    [JsonProperty("materialLightIntensity")] public float Intensity;

    public override void CopyTo(LightController target)
    {
    }
}
