using System.Linq;
using UnityEngine;

public class PointLightWithIdsData : EnvironmentComponentData<PointLightsController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;
    public string PointLight;

    public override void
        SearchAndFillComponents(GameObject self, PointLightsController comp, CreateContainer container) =>
        comp.Light = container.GetGameObjectOrNull(PointLight, self).GetComponent<PointLight>();

    public override void CopyTo(PointLightsController comp)
    {
        comp.LightIntensityData = LightIntensityData
            .Select(data =>
            {
                var lic = comp.gameObject.AddComponent<LightIntensityData>();
                data.CopyTo(lic);
                return lic;
            })
            .ToArray();

        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;
    }
}
