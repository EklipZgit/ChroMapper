using Newtonsoft.Json;

public class ParticleSystemLightWithIdComponent : EnvDataComponent<ParametricBloomFogLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;
    public float Intensity;

    public override void CopyTo(ParametricBloomFogLightController target)
    {
    }
}
