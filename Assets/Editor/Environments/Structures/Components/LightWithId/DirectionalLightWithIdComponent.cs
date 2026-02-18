using System;
using Newtonsoft.Json;

public class DirectionalLightWithIdComponent : EnvDataComponent<DirectionalLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    public float Intensity;
    public float MinIntensity;
    public string Light;

    public override void CopyTo(DirectionalLightController target)
    {
        target.Intensity = Intensity;
        target.MinIntensity = MinIntensity;
    }
}
