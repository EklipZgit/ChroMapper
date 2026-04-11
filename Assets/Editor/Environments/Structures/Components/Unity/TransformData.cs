using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class TransformData : EnvironmentComponentData<Transform>
{
    public Vector3 Position;
    public Vector3 LocalPosition;

    public Vector3 Rotation;
    public Vector3 LocalRotation;

    public Vector3 Scale = Vector3.one;

    public override void SearchAndFillComponents(GameObject self, Transform comp, CreateContainer container) { }

    public override void CopyTo(Transform comp)
    {
        comp.localPosition = LocalPosition;
        comp.localEulerAngles = LocalRotation;
        comp.localScale = Scale;
    }
}
