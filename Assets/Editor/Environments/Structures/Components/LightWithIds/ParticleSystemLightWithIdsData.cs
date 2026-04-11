using Newtonsoft.Json;
using UnityEngine;

public class ParticleSystemLightWithIdsData : EnvironmentComponentData<ParticleSystemLightsController>
{
    [JsonProperty("lightId")] public int Id;

    public float Intensity;
    public float MaxIntensity = 1f;
    public bool MultiplyColorByAlpha = true;
    public int MixType;

    public string ParticleSystem;
    public bool SetOnlyOnce;
    public bool SetColorOnly;
    public float MinAlpha;

    public override void SearchAndFillComponents(
        GameObject self,
        ParticleSystemLightsController comp,
        CreateContainer container) =>
        comp.ParticleSystem = container.GetGameObjectOrNull(ParticleSystem, self).GetComponent<ParticleSystem>();

    public override void CopyTo(ParticleSystemLightsController comp)
    {
        comp.Intensity = Intensity;
        comp.MaxIntensity = MaxIntensity;
        comp.MultiplyColorByAlpha = MultiplyColorByAlpha;
        comp.MixType = (ColorMixAndWeightingApproach)MixType;

        comp.SetOnlyOnce = SetOnlyOnce;
        comp.SetColorOnly = SetColorOnly;
        comp.MinAlpha = MinAlpha;
    }
}
