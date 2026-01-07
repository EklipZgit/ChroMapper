using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class ColliderComponent
{
    public string Type;
    public Vector3 BoundsCenter = Vector3.zero;
    public Vector3 BoundsSize = Vector3.zero;
}
