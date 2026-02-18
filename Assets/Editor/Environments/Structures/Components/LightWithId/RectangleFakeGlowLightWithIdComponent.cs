using Newtonsoft.Json;

public class RectangleFakeGlowLightWithIdComponent : EnvDataComponent<RectangleFakeGlowLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    public float MinAlpha;
    public float AlphaMultiplier = 1f;

    public override void CopyTo(RectangleFakeGlowLightController target)
    {
        target.MinAlpha = MinAlpha;
        target.AlphaMultiplier = AlphaMultiplier;
    }
}
