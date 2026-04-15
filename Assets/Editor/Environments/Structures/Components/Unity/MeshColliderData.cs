using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshColliderData : EnvironmentComponentData<MeshCollider>
{
    public override void FillComponents(GameObject self, MeshCollider comp, CreateContainer container)
    {
        var mf = self.GetComponent<MeshFilter>();
        if (mf != null) comp.sharedMesh = mf.sharedMesh;
    }
}
