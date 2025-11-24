using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshRendererComponent : EnvironmentComponent<MeshRenderer>
{
    [JsonProperty("materials")]
    public List<string> Materials = new ();

    public override void CopyTo(MeshRenderer target)
    {
    }
}
