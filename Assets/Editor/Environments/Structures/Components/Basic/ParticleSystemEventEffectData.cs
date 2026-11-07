using UnityEngine;

public class ParticleSystemEventEffectData : EnvironmentComponentData<ParticleSystemEffect>
{
    public EnvironmentEventType EventType;
    public bool LightOnStart;
    public int ParticleSystem;

    public override void FillComponents(
        GameObject self,
        ParticleSystemEffect comp,
        CreateContainer container)
    {
        comp.enabled = true;
        comp.ColorSchemeProvider = container.Descriptor.ColorSchemeProvider;
        var timeline = self.AddComponent<ParticleLightTimeline>();
        timeline.Visual = comp;
        container.Descriptor.BasicEventEffectManager.Register(EventType, timeline);

        comp.ParticleSystem = container.GetComponentOrNull<ParticleSystem>(ParticleSystem);
        comp.LightOnStart = LightOnStart;
    }
}
