using Newtonsoft.Json;

/// <summary>
/// Metadata about the environment, including its name, internal ID, and color scheme.
/// </summary>
public class EnvironmentData
{
    [JsonProperty("environmentTitle")]
    public string Title;

    [JsonProperty("environmentID")]
    public string ID;

    //[JsonProperty("colorScheme")]
    [JsonIgnore]
    public PlatformColorScheme ColorScheme;
}
