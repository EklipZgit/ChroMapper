using System.Linq;
using UnityEngine;

public class DirectionalLightWithGroupIdsData : EnvironmentComponentData<DirectionalLightsGroupController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public string DirectionalLight;

    public override void SearchAndFillComponents(
        GameObject self,
        DirectionalLightsGroupController comp,
        CreateContainer container)
    {
        comp.Light = container
            .GetGameObjectOrNull(DirectionalLight, self)
            .GetComponent<DirectionalLight>();
    }

    public override void CopyTo(DirectionalLightsGroupController comp)
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
    }
}
