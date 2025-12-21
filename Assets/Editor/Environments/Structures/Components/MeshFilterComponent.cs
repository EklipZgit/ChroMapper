using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshFilterComponent : EnvDataComponent<MeshFilter>
{
    [JsonProperty("hash")]
    public string Hash;

    public override void CopyTo(MeshFilter target)
    {
    }
}
