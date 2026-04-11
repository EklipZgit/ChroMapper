using Newtonsoft.Json;
using UnityEngine;

public class ParticleSystemLightWithIdData : EnvironmentComponentData<ParticleSystemLightController>
{
    [JsonProperty("lightId")] public int Id;

    public string ParticleSystem;

    public bool SetOnlyOnce;
    public bool SetColorOnly;
    public float Intensity = 1f;
    public float MinAlpha;


    public override void SearchAndFillComponents(
        GameObject self,
        ParticleSystemLightController comp,
        CreateContainer container) =>
        comp.ParticleSystem = container.GetGameObjectOrNull(ParticleSystem, self).GetComponent<ParticleSystem>();

    public override void CopyTo(ParticleSystemLightController comp)
    {
        comp.SetOnlyOnce = SetOnlyOnce;
        comp.SetColorOnly = SetColorOnly;
        comp.Intensity = Intensity;
        comp.MinAlpha = MinAlpha;
    }
}
