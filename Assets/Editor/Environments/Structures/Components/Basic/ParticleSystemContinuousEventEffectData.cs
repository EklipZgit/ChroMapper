using System.Linq;
using UnityEngine;

public class ParticleSystemContinuousEventEffectData : EnvironmentComponentData<ParticleSystemContinuous>
{
    public string EventType;
    public string[] ParticleSystems;

    public override void SearchAndFillComponents(
        GameObject self,
        ParticleSystemContinuous comp,
        CreateContainer container)
    {
        comp.ParticleSystems = ParticleSystems
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.GetComponent<ParticleSystem>() : null)
            .Where(y => y != null)
            .ToArray();
    }

    public override void CopyTo(ParticleSystemContinuous comp)
    {
    }
}
