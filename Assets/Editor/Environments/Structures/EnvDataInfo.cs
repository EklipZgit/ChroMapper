using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// Metadata about the environment, including its name, internal ID, color scheme, light lanes, and more.
/// </summary>
public class EnvDataInfo
{
    // The in-game title of the environment (ex: "The First")
    [JsonProperty("environmentTitle")] public string Title;

    // The serialized name of the environment (ex: "DefaultEnvironment")
    [JsonProperty("environmentID")] public string ID;

    [JsonProperty("colorScheme")] public EnvColorScheme ColorScheme;

    // The environment-specific bloom fog parameters
    [JsonProperty("fogParams")] public EnvFogDefinition FogParameters;

    // The light tracks/lanes of the environment
    [JsonProperty("lightTracks")] public LightTracksDefinition LightTracks;

    // Every unique material found in the environments' objects (name, keyword list)
    [JsonProperty("uniqueMaterials")] public EnvInfoMaterial[] UniqueMaterials;

    // Every unique mesh name found in the environments' objects
    [JsonProperty("uniqueMeshes")] public EnvInfoMesh[] UniqueMeshes;
}

public class LightTracksDefinition
{
    // Basic Event Tracks
    [JsonProperty("eventTracks")] public List<BasicTrackDefinition> BasicLightTracks;

    // Event Box Group Pages with their lanes
    [JsonProperty("groupPages")] public Dictionary<string, List<PageDefinition>> GroupPages;

    public class BasicTrackDefinition
    {
        [JsonProperty("trackName")] public string TrackName = "";
        [JsonProperty("eventType")] public string EventType = "";
        [JsonProperty("toolbarType")] public string ToolbarType = "";
        [JsonProperty("page")] public string Page = "";

        public int ConvertType()
        {
            return EventType switch
            {
                "Event0" => 0,
                "Event1" => 1,
                "Event2" => 2,
                "Event3" => 3,
                "Event4" => 4,
                "Event5" => 5,
                "Event6" => 6,
                "Event7" => 7,
                "Event8" => 8,
                "Event9" => 9,
                "Event10" => 10,
                "Event11" => 11,
                "Event12" => 12,
                "Event13" => 13,
                "Event14" => 14,
                "Event15" => 15,
                "Event16" => 16,
                "Event17" => 17,
                "Event18" => 18,
                "Event19" => 19,
                "Event20" => 20,
                "Event21" => 21,
                "VoidEvent" => -1,
                "Special0" => 40,
                "Special1" => 41,
                "Special2" => 42,
                "Special3" => 43,
                "BpmChange" => 100,
                _ => throw new Exception("Unknown event or new?")
            };
        }

        public BasicEventKind ConvertKind()
        {
            return ToolbarType switch
            {
                "None" => BasicEventKind.None,
                "Lights" => BasicEventKind.Lights,
                "Toggle" => BasicEventKind.Toggle,
                "FloatValue" => BasicEventKind.FloatValue,
                "IntValue" => BasicEventKind.IntValue,
                "BtsCharacterSelection" => BasicEventKind.BtsCharacter,
                "CarSelection" => BasicEventKind.Car,
                _ => throw new Exception("Unknown toolbar type: " + ToolbarType)
            };
        }
    }

    public class PageDefinition
    {
        [JsonProperty("groupId")] public int GroupId;
        [JsonProperty("groupName")] public string GroupName = "";
        [JsonProperty("colorTrack")] public bool ColorTrack;
        [JsonProperty("floatFxTrack")] public bool FloatFxTrack;
        [JsonProperty("duplicate")] public bool Duplicate;

        [JsonProperty("rotationTracks")] public List<string> RotationTracks = new();

        [JsonProperty("overrideDefaultRotationAxis")]
        public string OverrideDefaultRotationAxis = "";

        [JsonProperty("translationTracks")] public List<string> TranslationTracks = new();

        [JsonProperty("overrideDefaultTranslationAxis")]
        public string OverrideDefaultTranslationAxis = "";

        public bool[] GetAxisBool(List<string> axisNames)
        {
            var res = new bool[3];
            res[0] = axisNames.Contains("X");
            res[1] = axisNames.Contains("Y");
            res[2] = axisNames.Contains("Z");
            return res;
        }
    }

    public void CopyTo(TracksDefinitionSO copy)
    {
        copy.UnregisterAll();
        BasicLightTracks
            .Select(x =>
                new TrackDefinitionBasic() { Name = x.TrackName, Type = x.ConvertType(), Kind = x.ConvertKind() })
            .ToList()
            .ForEach(copy.Register);
        GroupPages
            .SelectMany(x => x.Value.Select(y => (group: x.Key, id: y)))
            .Select(x =>
                new TrackDefinitionGLS()
                {
                    Group = x.group,
                    Name = x.id.GroupName,
                    ColorTrack = x.id.ColorTrack,
                    RotationTracks = x.id.GetAxisBool(x.id.RotationTracks),
                    OverrideDefaultRotationAxis = x.id.OverrideDefaultRotationAxis,
                    TranslationTracks = x.id.GetAxisBool(x.id.TranslationTracks),
                    OverrideDefaultTranslationAxis = x.id.OverrideDefaultTranslationAxis,
                    FloatFXTrack = x.id.FloatFxTrack,
                    Duplicate = x.id.Duplicate
                })
            .ToList()
            .ForEach(copy.Register);
    }
}

public class EnvFogDefinition
{
    public float Offset;
    public float Height;
    public float StartY;
    public float Attenuation;

    public void CopyTo(BloomFogParams copy)
    {
        copy.Offset = Offset;
        copy.Height = Height;
        copy.StartY = StartY;
        copy.Attenuation = Attenuation;
    }
}

public class EnvColorScheme
{
    public float[] ColorLeft;
    public float[] ColorRight;
    public float[] EnvColorLeft;
    public float[] EnvColorRight;
    public float[] ObstacleColor;
    public float[] EnvColorLeftBoost;
    public float[] EnvColorRightBoost;
    public float[] EnvColorWhite;
    public float[] EnvColorWhiteBoost;

    public void CopyTo(ColorSchemeSO copy)
    {
        copy.LeftNoteColor = ToColor(ColorLeft);
        copy.RightNoteColor = ToColor(ColorRight);

        copy.EnvironmentLeftColor = ToColor(EnvColorLeft);
        copy.EnvironmentRightColor = ToColor(EnvColorRight);
        copy.EnvironmentWhiteColor = ToColor(EnvColorWhite);

        copy.EnvironmentLeftBoostColor = ToColor(EnvColorLeftBoost);
        copy.EnvironmentRightBoostColor = ToColor(EnvColorRightBoost);
        copy.EnvironmentWhiteBoostColor = ToColor(EnvColorWhiteBoost);

        copy.ObstacleColor = ToColor(ObstacleColor);
    }

    private Color ToColor(float[] nums) => new(nums[0], nums[1], nums[2]);
}

public class EnvInfoMaterial
{
    public string Hash;
    public string Name;
    public string Shader;
    public float[] Color;
    public string[] Keywords;
}

public class EnvInfoMesh
{
    public string Hash;
    public string Name;
    public Vector3 BoundsSize;
    public Vector3 BoundsCenter;
}
