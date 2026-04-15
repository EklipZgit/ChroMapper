using UnityEngine;

public class ColliderEventEffectData : EnvironmentComponentData<ColliderFx>
{
    public string EffectCollider;
    public float Value;

    public override void FillComponents(GameObject self, ColliderFx comp, CreateContainer container)
    {
        comp.Repository = container.Descriptor.FloatFxGroupEffectManager.gameObject
            .GetOrAddComponent<ColliderRepository>();

        var coll = container.TryGetGameObjectOrNull(EffectCollider, self, out var o)
            ? o.GetComponent<Collider>()
            : null;
        comp.Collider = coll;
        comp.Value = Value;
    }
}
