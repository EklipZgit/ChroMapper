using System.Linq;
using UnityEngine;

public class ParticleSystemContinuousEventEffectData : EnvironmentComponentData<ParticleSystemContinuous>
{
    public string EventType;
    public string[] ParticleSystems;

    public override void FillComponents(
        GameObject self,
        ParticleSystemContinuous comp,
        CreateContainer container)
    {
        comp.Effect = container.Descriptor.BasicEventEffectManager.GetOrRegister<GenericCallbackEventEffect>(
            ConvertUtils.ToEventType(EventType));

        comp.ParticleSystems = ParticleSystems
            .Select(y =>
                container.TryGetGameObjectOrNull(y, self, out var g) ? g.GetComponent<ParticleSystem>() : null)
            .Where(y => y != null)
            .ToArray();
    }
}
