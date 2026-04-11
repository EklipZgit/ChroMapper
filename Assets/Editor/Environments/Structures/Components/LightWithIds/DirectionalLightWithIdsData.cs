using System.Linq;
using UnityEngine;

public class DirectionalLightWithIdsData : EnvironmentComponentData<DirectionalLightsController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;
    public string DirectionalLight;
    public bool SetIntensityOnly;

    public override void SearchAndFillComponents(
        GameObject self,
        DirectionalLightsController comp,
        CreateContainer container)
    {
        comp.Light = container
            .GetGameObjectOrNull(DirectionalLight, self)
            .GetComponent<DirectionalLight>();
    }

    public override void CopyTo(DirectionalLightsController comp)
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
        comp.SetIntensityOnly = SetIntensityOnly;
    }
}
