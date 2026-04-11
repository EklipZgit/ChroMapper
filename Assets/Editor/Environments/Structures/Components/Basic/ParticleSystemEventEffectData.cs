using UnityEngine;

public class ParticleSystemEventEffectData : EnvironmentComponentData<ParticleSystemEffect>
{
    public string EventType;
    public bool LightOnStart;
    public string ParticleSystem;

    public override void SearchAndFillComponents(
        GameObject self,
        ParticleSystemEffect comp,
        CreateContainer container) =>
        comp.ParticleSystem = container.GetGameObjectOrNull(ParticleSystem, self).GetComponent<ParticleSystem>();

    public override void CopyTo(ParticleSystemEffect comp) => comp.LightOnStart = LightOnStart;
}
