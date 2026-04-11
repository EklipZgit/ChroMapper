using System.Linq;
using UnityEngine;

public class BloomPrePassBackgroundColorsGradientTintColorWithLightIdData : EnvironmentComponentData<
    BloomPrePassBackgroundColorsGradientTintColorLightsController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public string BloomPrePassBackgroundColorsGradient;
    public bool UseGrayscale;
    public float GrayscaleFactor;

    public override void SearchAndFillComponents(
        GameObject self,
        BloomPrePassBackgroundColorsGradientTintColorLightsController comp,
        CreateContainer container)
    {
        comp.BloomPrePassBackgroundColorsGradient = container
            .GetGameObjectOrNull(
                BloomPrePassBackgroundColorsGradient,
                self)
            .GetComponent<BloomPrePassBackgroundColorsGradient>();
    }

    public override void CopyTo(BloomPrePassBackgroundColorsGradientTintColorLightsController comp)
    {
        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;

        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.UseGrayscale = UseGrayscale;
        comp.GrayscaleFactor = GrayscaleFactor;
    }
}
