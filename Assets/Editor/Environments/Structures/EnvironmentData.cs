using Newtonsoft.Json;

public class EnvironmentData
{
    [JsonProperty("environmentTitle")]
    public string Title;

    [JsonProperty("environmentID")]
    public string ID;

    //[JsonProperty("colorScheme")]
    [JsonIgnore]
    public PlatformColors ColorScheme;
}
