using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>
/// Metadata about the environment, including its name, internal ID, color scheme, light lanes, and more.
/// </summary>
public class EnvironmentInfo
{
    // The in-game title of the environment (ex: "The First")
    [JsonProperty("environmentTitle")]
    public string Title;

    // The serialized name of the environment (ex: "DefaultEnvironment")
    [JsonProperty("environmentID")]
    public string ID;
    
    //[JsonProperty("colorScheme")]
    [JsonIgnore]
    public PlatformColorScheme ColorScheme;

    // The environment-specific bloom fog parameters
    [JsonProperty("fogParams")]
    public EnvironmentFogDefinition FogParameters;
    
    // The light tracks/lanes of the environment
    [JsonProperty("lightTracks")]
    public LightTracksDefinition LightTracks;
    
    // Every unique material found in the environments' objects (name, keyword list)
    [JsonProperty("uniqueMaterials")]
    public EnvInfoMaterial[] UniqueMaterials;
    
    // Every unique mesh name found in the environments' objects
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
    // Basic Event Tracks
    [JsonProperty("eventTracks")]
    public List<BasicTrackDefinition> BasicLightTracks;
    
    // Event Box Group Pages with their lanes
    [JsonProperty("groupPages")]
    public Dictionary<string, List<PageDefinition>> GroupPages;
    
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
