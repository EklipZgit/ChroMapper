using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Stores Chroma light data from a lightable object.
/// </summary>
public class ChromaLightComponent
{
    [JsonProperty("type")]
    public int Type = -1;
    [JsonProperty("lightId")]
    public int LightId = -1;
}
