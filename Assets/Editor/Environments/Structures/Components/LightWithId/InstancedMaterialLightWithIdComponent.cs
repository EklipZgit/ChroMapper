using System;
using Newtonsoft.Json;

public class InstancedMaterialLightWithIdComponent : EnvDataComponent<InstancedMaterialLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    [JsonProperty("lightIntensity")] public float Intensity;
    public bool HDR;
    public float MinAlpha;
    public bool SetColorOnly;
    public string MultiplyColorByAlpha;
    public bool SaturateIntensity;

    public override void CopyTo(InstancedMaterialLightController target)
    {
        target.Intensity = Intensity;
        target.HDR = HDR;
        target.MinAlpha = MinAlpha;
        target.SetColorOnly = SetColorOnly;
        // TODO: fix
        // target.MultiplyColorByAlpha = Enum.Parse<MultiplyColorByAlphaType>(MultiplyColorByAlpha);
        target.SaturateIntensity = SaturateIntensity;
    }
}
