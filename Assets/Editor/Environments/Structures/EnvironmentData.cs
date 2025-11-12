using System.Collections.Generic;
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

    [JsonProperty("fogParams")] 
    public EnvironmentFogDefinition FogParameters;
    
    [JsonProperty("lightTracks")] 
    public LightTracksDefinition LightTracks;
    
    [JsonProperty("uniqueMaterials")] 
    public EnvInfoMaterial[] UniqueMaterials;
    
    [JsonProperty("uniqueMeshes")]
    public string[] UniqueMeshes;
}

public class EnvironmentFogDefinition
{
    public float Offset;
    public float Height;
    public float StartY;
    public float Attenuation;
}

public class LightTracksDefinition
{
    [JsonProperty("eventTracks")] 
    public List<BasicTrackDefinition> V2LightTracks;
    
    [JsonProperty("groupPages")]
    public Dictionary<string, List<PageDefinition>> V3GroupPages;
    
    public class BasicTrackDefinition
    {
        [JsonProperty("trackName")]
        public string TrackName = "";
        [JsonProperty("eventType")]
        public string EventType = "";
        [JsonProperty("toolbarType")]
        public string ToolbarType = "";
        [JsonProperty("page")]
        public string Page = "";
    }

    public class PageDefinition
    {
        [JsonProperty("groupName")]
        public string GroupName = "";
        [JsonProperty("colorTrack")]
        public bool ColorTrack = false;
        [JsonProperty("floatFxTrack")]
        public bool FloatFxTrack = false;
        [JsonProperty("duplicate")]
        public bool Duplicate = false;
        
        [JsonProperty("rotationTracks")]
        public List<string> RotationTracks = new();
        [JsonProperty("overrideDefaultRotationAxis")]
        public string OverrideDefaultRotationAxis = "";
        [JsonProperty("translationTracks")]
        public List<string> TranslationTracks = new();
        [JsonProperty("overrideDefaultTranslationAxis")]
        public string OverrideDefaultTranslationAxis = "";
    }
}

public class EnvInfoMaterial
{
    public string Name;
    public string[] Keywords;
}
