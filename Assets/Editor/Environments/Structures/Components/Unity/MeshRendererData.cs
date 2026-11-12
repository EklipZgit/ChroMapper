using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Imports a mesh renderer's material references and authored visibility.
/// </summary>
public class MeshRendererData : EnvironmentComponentData<MeshRenderer>
{
    [JsonProperty("materials")] public List<string> Materials = new();

    [JsonProperty("lightmapIndex")] public int LightmapIndex = -1;
    [JsonProperty("lightmapScaleOffset")] public Vector4 LightmapScaleOffset = new(1f, 1f, 0f, 0f);

    // Preserve Unity's default visibility when the export omits the flag.
    public MeshRendererData() => IsEnabled = true;

    public override void FillComponents(GameObject self, MeshRenderer comp, CreateContainer container)
    {
        comp.enabled = IsEnabled;
        if (LightmapIndex >= 0)
        {
            var atlas = self.GetOrAddComponent<BakedLightmapAtlas>();
            atlas.Configure(comp, LightmapIndex, LightmapScaleOffset);
        }
    }
}
