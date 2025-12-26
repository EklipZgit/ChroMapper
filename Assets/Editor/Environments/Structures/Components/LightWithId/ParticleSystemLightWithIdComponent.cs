using Newtonsoft.Json;

public class ParticleSystemLightWithIdComponent : EnvDataComponent<LightController>
{
    [JsonProperty("lightId")] public int ID;
    [JsonProperty("intensity")] public float Intensity;

    public override void CopyTo(LightController target)
    {
    }
}
