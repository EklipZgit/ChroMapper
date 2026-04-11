using System.Linq;
using UnityEngine;

public class MaterialLightWithIdsData : EnvironmentComponentData<MaterialLightsController>
{
    public int InstanceId;
    public LightIntensityIdData[] LightIntensityData;

    public float Intensity = 1f;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public string MeshRenderer;
    public bool SetAlphaOnly;
    public bool AlphaIntoColor;
    public bool SetColorOnly;
    public string ColorProperty = "_Color";

    public override void SearchAndFillComponents(
        GameObject self,
        MaterialLightsController comp,
        CreateContainer container)
    {
        comp.MeshRenderer = container
            .GetGameObjectOrNull(MeshRenderer, self)
            .GetComponent<MeshRenderer>();
    }

    public override void CopyTo(MaterialLightsController comp)
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

        comp.SetAlphaOnly = SetAlphaOnly;
        comp.AlphaIntoColor = AlphaIntoColor;
        comp.SetColorOnly = SetColorOnly;
        comp.ColorProperty = ColorProperty;
    }
}
