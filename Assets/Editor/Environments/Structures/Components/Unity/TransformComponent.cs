using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class TransformComponent : EnvDataComponent<Transform>
{
    public Vector3 Position = Vector3.zero;
    public Vector3 LocalPosition = Vector3.zero;

    public Vector3 Rotation = Vector3.zero;
    public Vector3 LocalRotation = Vector3.zero;

    public Vector3 Scale = Vector3.one;

    public override void CopyTo(Transform target)
    {
        target.localPosition = LocalPosition;
        target.localEulerAngles = LocalRotation;
        target.localScale = Scale;
    }
}
