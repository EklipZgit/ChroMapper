using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshColliderData : EnvironmentComponentData<MeshCollider>
{
    public override void SearchAndFillComponents(GameObject self, MeshCollider comp, CreateContainer container)
    {
    }

    public override void CopyTo(MeshCollider comp) { }
}
