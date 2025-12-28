using Newtonsoft.Json;

public class MaterialLightWithIdComponent : EnvDataComponent<LightController>
{
    public float AlphaIntensity;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public bool MultiplyColorWithAlpha;
    public bool MultiplyColor;
    public float ColorMultiplier;
    public float Alpha;
    public float LightId;

    public override void CopyTo(LightController target)
    {
    }
}
