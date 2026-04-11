using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshFilterData : EnvironmentComponentData<MeshFilter>
{
    [JsonProperty("hash")] public string Hash;

    public override void SearchAndFillComponents(GameObject self, MeshFilter comp, CreateContainer container)
    {
    }

    public override void CopyTo(MeshFilter comp)
    {
    }
}
