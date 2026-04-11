using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Simple EnvironmentComponent for a Unity Transform.
/// </summary>
public class MeshRendererData : EnvironmentComponentData<MeshRenderer>
{
    [JsonProperty("materials")] public List<string> Materials = new();

    public override void SearchAndFillComponents(GameObject self, MeshRenderer comp, CreateContainer container)
    {
    }

    public override void CopyTo(MeshRenderer comp)
    {
    }
}
