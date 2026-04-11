using UnityEngine;

public class SphereColliderData : EnvironmentComponentData<SphereCollider>
{
    public Vector3 Center;
    public float Radius;

    public override void SearchAndFillComponents(GameObject self, SphereCollider comp, CreateContainer container)
    {
    }

    public override void CopyTo(SphereCollider comp)
    {
        comp.center = Center;
        comp.radius = Radius;
    }
}
