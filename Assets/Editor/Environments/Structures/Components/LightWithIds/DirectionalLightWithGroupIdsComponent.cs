using System.Linq;

public class DirectionalLightWithGroupIdsComponent : EnvDataComponent<DirectionalLightsGroupController>
{
    public bool IsEnabled;

    public LightIntensityIdComponent[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public string DirectionalLight;

    public override void CopyTo(DirectionalLightsGroupController target)
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
    }
}
