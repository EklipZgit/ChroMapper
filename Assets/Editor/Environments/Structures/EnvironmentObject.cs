using Newtonsoft.Json;

#nullable enable
/// <summary>
/// A single object in an environment, with its properties and components.
/// </summary>
public class EnvironmentObject
{
    [JsonProperty("name")]
    public string GameObjectName = string.Empty;

    [JsonProperty("id")]
    public string ChromaID = string.Empty;

    [JsonProperty("meshName")]
    public string? MeshName;

    [JsonProperty("components")]
    public EnvironmentComponents Components = new();

    // We can leave this to standard Newtonsoft.Json serialization.
    public class EnvironmentComponents
    {
        public EnvironmentTransformComponent? Transform;
    }
}
#nullable restore
