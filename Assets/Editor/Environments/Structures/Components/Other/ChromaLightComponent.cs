using Newtonsoft.Json;

/// <summary>
/// Stores Chroma light data from a lightable object.
/// </summary>
public class ChromaLightComponent
{
    [JsonProperty("lightId")]
    public int Type = -1;
    [JsonProperty("type")]
    public int LightId = -1;
}
