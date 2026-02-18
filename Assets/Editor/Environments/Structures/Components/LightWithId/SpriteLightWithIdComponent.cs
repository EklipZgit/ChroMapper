using Newtonsoft.Json;

public class SpriteLightWithIdComponent : EnvDataComponent<SpriteLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    [JsonProperty("sprite")] public SpriteData Sprite;

    public bool HideIfAlphaOutOfRange;
    public float HideAlphaRangeMin;
    public float HideAlphaRangeMax;
    [JsonProperty("lightIntensity")] public float Intensity;
    public float MinAlpha;
    public MultiplyColorByAlphaType MultiplyColorByAlpha;
    public bool SetColorOnly;
    public bool SetAlphaOnly;
    public bool SetOnlyOnce;

    public class SpriteData
    {
        public string Name;
        public string TextureName;
        public float[] Size;
        public string[] Materials;
    }

    public override void CopyTo(SpriteLightController target)
    {
        target.HideIfAlphaOutOfRange = HideIfAlphaOutOfRange;
        target.HideAlphaRangeMin = HideAlphaRangeMin;
        target.HideAlphaRangeMax = HideAlphaRangeMax;
        target.Intensity = Intensity;
        target.MinAlpha = MinAlpha;
        target.MultiplyColorByAlpha = MultiplyColorByAlpha;
        target.SetColorOnly = SetColorOnly;
        target.SetAlphaOnly = SetAlphaOnly;
        target.SetOnlyOnce = SetOnlyOnce;
    }
}
