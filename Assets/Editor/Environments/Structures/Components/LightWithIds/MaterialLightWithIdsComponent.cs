using System.Linq;

public class MaterialLightWithIdsComponent : EnvDataComponent<MaterialLightsController>
{
    public int InstanceId;
    public bool IsEnabled;

    public LightIntensityIdComponent[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public string MeshRenderer;
    public bool SetAlphaOnly;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public string ColorProperty = "_Color";

    public override void CopyTo(MaterialLightsController target)
    {
        target.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = target.gameObject.AddComponent<LightIntensityController>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        target.Intensity = Intensity;
        target.MaxIntensity = MaxIntensity;
        target.MultiplyColorByAlpha = MultiplyColorByAlpha;
        target.MixType = (ColorMixAndWeightingApproach)MixType;

        target.SetAlphaOnly = SetAlphaOnly;
        target.AlphaIntoColor = AlphaIntoColor;
        target.SetColorOnly = SetColorOnly;
        target.ColorProperty = ColorProperty;
    }
}
