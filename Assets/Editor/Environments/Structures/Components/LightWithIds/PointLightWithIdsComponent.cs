using System.Linq;

public class PointLightWithIdsComponent : EnvDataComponent<PointLightsController>
{
    public bool IsEnabled;

    public LightIntensityIdComponent[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;
    public string PointLight;

    public override void CopyTo(PointLightsController target)
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
    }
}
