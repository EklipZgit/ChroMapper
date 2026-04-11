using UnityEngine;

public class ParticleSystemData : EnvironmentComponentData<ParticleSystem>
{
    public override void SearchAndFillComponents(GameObject self, ParticleSystem comp, CreateContainer container) { }

    public override void CopyTo(ParticleSystem comp)
    {
    }
}
