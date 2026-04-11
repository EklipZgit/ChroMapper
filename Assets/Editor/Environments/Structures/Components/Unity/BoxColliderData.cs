using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class BoxColliderData : EnvironmentComponentData<BoxCollider>
{
    public Vector3 Center;
    public Vector3 Size;

    public override void SearchAndFillComponents(GameObject self, BoxCollider comp, CreateContainer container) { }

    public override void CopyTo(BoxCollider comp)
    {
        comp.center = Center;
        comp.size = Size;
    }
}
