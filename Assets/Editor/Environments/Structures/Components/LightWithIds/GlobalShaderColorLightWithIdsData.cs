using System.Linq;
using UnityEngine;

public class GlobalShaderColorLightWithIdsData : EnvironmentComponentData<GlobalShaderColorLightsController>
{
    public LightIntensityIdData[] LightIntensityData;
    public bool OverrideSaturation;
    public float Saturation = 0.5f;

    public override void SearchAndFillComponents(
        GameObject self,
        GlobalShaderColorLightsController comp,
        CreateContainer container)
    {
    }

    public override void CopyTo(GlobalShaderColorLightsController comp)
    {
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.OverrideSaturation = OverrideSaturation;
        comp.Saturation = Saturation;
    }
}
