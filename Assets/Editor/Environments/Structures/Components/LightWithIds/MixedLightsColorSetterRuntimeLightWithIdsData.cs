using System.Linq;
using UnityEngine;

public class MixedLightsColorSetterRuntimeLightWithIdsData : EnvironmentComponentData<MixedLightsController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public string MaterialPropertyBlockColorSetterId;
    public float LightMultiplier = 1f;

    public override void SearchAndFillComponents(GameObject self, MixedLightsController comp, CreateContainer container)
    {
        comp.MpbColorSetter = container
            .GetGameObjectOrNull(MaterialPropertyBlockColorSetterId, self)
            .GetComponent<MaterialPropertyBlockColorSetter>();
    }

    public override void CopyTo(MixedLightsController comp)
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

        comp.LightMultiplier = LightMultiplier;
    }
}
