using Newtonsoft.Json;

public class MaterialLightWithIdComponent : EnvDataComponent<MaterialLightController>
{
    public bool IsEnabled;

    [JsonProperty("instanceId")] public int InstanceId;
    [JsonProperty("lightId")] public int Id;

    public float AlphaIntensity;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public bool MultiplyColorWithAlpha;
    public bool MultiplyColor;
    public float ColorMultiplier;
    public float Alpha;
    public string ColorProperty;

    public override void CopyTo(MaterialLightController target)
    {
        target.AlphaIntensity = AlphaIntensity;
        target.AlphaIntoColor = AlphaIntoColor;
        target.SetColorOnly = SetColorOnly;
        target.MultiplyColorWithAlpha = MultiplyColorWithAlpha;
        target.MultiplyColor = MultiplyColor;
        target.ColorMultiplier = ColorMultiplier;
        target.Alpha = Alpha;
        target.Property = ColorProperty;
    }
}
