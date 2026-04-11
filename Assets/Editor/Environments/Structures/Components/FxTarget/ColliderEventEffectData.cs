using UnityEngine;

public class ColliderEventEffectData : EnvironmentComponentData<ColliderFx>
{
    public string EffectCollider;
    public float Value;

    public override void SearchAndFillComponents(GameObject self, ColliderFx comp, CreateContainer container)
    {
        var coll = container.TryGetGameObjectOrNull(EffectCollider, self, out var o)
            ? o.GetComponent<Collider>()
            : null;
        comp.Collider = coll;
    }

    public override void CopyTo(ColliderFx comp) => comp.Value = Value;
}
