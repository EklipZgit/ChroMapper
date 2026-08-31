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

    [JsonProperty("sizeData")] public EnvSizeData SizeData;

    // The light tracks/lanes of the environment
    [JsonProperty("lightTracks")] public LightTracksDefinition LightTracks;

    // Every unique material found in the environments' objects (name, keyword list)
    [JsonProperty("uniqueMaterials")] public EnvInfoMaterial[] UniqueMaterials;

    // Every unique mesh name found in the environments' objects
    [JsonProperty("uniqueMeshes")] public EnvInfoMesh[] UniqueMeshes;
}

public class LightTracksDefinition
{
    private const string BillieEnvironmentId = "BillieEnvironment";
    private const string TheSecondEnvironmentId = "TheSecondEnvironment";
    private const string SkrillexEnvironmentId = "SkrillexEnvironment";

    // BillieTrackDefinitionImportUsesCorrectedLaneOrder and SkrillexBasicEventLanesUseEnvironmentPresentationOrder
    // keep each environment remap in one registry entry so adding another order does not also require a switch edit.
    private static readonly Dictionary<string, Dictionary<int, int>> BasicEventPresentationOrders = new()
    {
        {
            BillieEnvironmentId,
            new Dictionary<int, int>
            {
                { 1, 0 },
                { 6, 1 },
                { 7, 2 },
                { 0, 3 },
                { 10, 4 },
                { 11, 5 },
                { 4, 6 },
                { 2, 7 },
                { 3, 8 },
                { 5, 9 },
                { 12, 10 },
                { 13, 11 },
                { 9, 12 },
                { 8, 13 }
            }
        },
        {
            SkrillexEnvironmentId,
            new Dictionary<int, int>
            {
                { 0, 0 },
                { 2, 1 },
                { 3, 2 },
                { 6, 3 },
                { 7, 4 },
                { 1, 5 },
                { 4, 6 },
                { 5, 7 },
                { 9, 8 },
                { 8, 9 },
                { 12, 10 },
                { 13, 11 }
            }
        }
    };

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

    public void CopyTo(TracksDefinitionSO copy, IEnumerable<EnvDataObject> objects, string environmentId)
    {
        copy.UnregisterAll();
        var basicTracks = BasicLightTracks
            .Select(x =>
                new TrackDefinitionBasic
                {
                    // SkrillexTrackDefinitionImportRewritesMixedRingLaneNames preserves the raw dump while correcting ChroMapper's mixed-lane labels.
                    Name = GetBasicTrackName(environmentId, ConvertUtils.ToEventType(x.EventType), x.TrackName),
                    Type = ConvertUtils.ToEventType(x.EventType),
                    Kind = ConvertUtils.ToEventKind(x.ToolbarType)
                })
            .ToList();

        // BillieTrackDefinitionImportUsesCorrectedLaneOrder and SkrillexTrackDefinitionImportRewritesMixedRingLaneNames
        // keep presentation corrections in the importer so runtime label creation remains environment-agnostic.
        basicTracks = ApplyBasicTrackPresentationOrder(environmentId, basicTracks);

        // Infer Basic Event capabilities from the game components that register for each event type.
        foreach (var components in objects.Select(x => x.Components))
        {
            foreach (var rotation in components.TrackLaneRingsRotationEffectSpawner ?? Array.Empty<TrackLaneRingsRotationEffectSpawnerComponent>())
            {
                if (rotation.IsEnabled)
                    AddComponent(basicTracks, ConvertUtils.ToEventType(rotation.EventType), BasicEventComponent.RingRotation);
            }

            foreach (var zoom in components.TrackLaneRingsPositionStepEffectSpawner ?? Array.Empty<TrackLaneRingsPositionStepEffectSpawnerComponent>())
            {
                if (zoom.IsEnabled)
                    AddComponent(basicTracks, ConvertUtils.ToEventType(zoom.EventType), BasicEventComponent.RingZoom);
            }

            foreach (var rotation in components.LightRotationEventEffect ?? Array.Empty<LightRotationEventEffectComponent>())
            {
                // Match Create from Data, which registers direct light-rotation effects by event type.
                AddComponent(basicTracks, ConvertUtils.ToEventType(rotation.EventType), BasicEventComponent.LightRotation);
            }

            foreach (var pair in components.LightPairRotationEventEffect ?? Array.Empty<LightPairRotationEventEffectComponent>())
            {
                // Pair rotation registers independent left and right light-rotation event consumers.
                AddComponentIfValid(
                    basicTracks,
                    pair.EventTypeL,
                    BasicEventComponent.LightRotation | BasicEventComponent.LightRotationLeft);
                AddComponentIfValid(
                    basicTracks,
                    pair.EventTypeR,
                    BasicEventComponent.LightRotation | BasicEventComponent.LightRotationRight);
            }

            foreach (var pair in components.LightPairSinMoveEventEffect ?? Array.Empty<LightPairSinMoveEventEffectComponent>())
            {
                // Pair sinusoidal movement uses the same light-rotation event effect and speed-value semantics.
                AddComponentIfValid(
                    basicTracks,
                    pair.EventTypeL,
                    BasicEventComponent.LightRotation | BasicEventComponent.LightRotationLeft);
                AddComponentIfValid(
                    basicTracks,
                    pair.EventTypeR,
                    BasicEventComponent.LightRotation | BasicEventComponent.LightRotationRight);
            }
        }

        // The Second's legacy smooth-step ring registration is absent from its export, so hardcode its known Event9 capability.
        if (environmentId == TheSecondEnvironmentId)
            AddComponent(basicTracks, 9, BasicEventComponent.SmoothStepRingZoom);

        basicTracks.ForEach(copy.Register);
        GroupPages
            .SelectMany(x => x.Value.Select(y => (group: x.Key, id: y)))
            .Select(x =>
                new TrackDefinitionGLS
                {
                    Group = x.group,
                    Name = x.id.GroupName,
                    ID = x.id.GroupId,
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

    // Environment-specific aliases belong at import time so regenerated assets remain stable without editing authoritative exports.
    private static string GetBasicTrackName(string environmentId, int eventType, string exportedName)
    {
        if (environmentId != SkrillexEnvironmentId)
            return exportedName;

        // SkrillexPanelSpeedLanesUseDescriptiveTrackName keeps regenerated labels aligned with the corrected asset.
        return eventType switch
        {
            8 => "Ring 2 Rotation / Zoom",
            9 => "Ring 1 Rotation / Zoom",
            12 => "Left Panel Speed",
            13 => "Right Panel Speed",
            _ => exportedName
        };
    }

    // Unknown future tracks retain their exported relative order after every explicitly ordered current lane.
    private static List<TrackDefinitionBasic> ApplyBasicTrackPresentationOrder(
        string environmentId,
        List<TrackDefinitionBasic> tracks)
    {
        // The presentation-order registry makes environment selection a single lookup and leaves unknown exports unchanged.
        if (!BasicEventPresentationOrders.TryGetValue(environmentId, out var presentationOrder))
            return tracks;

        return tracks
            .Select((track, sourceIndex) => new { Track = track, SourceIndex = sourceIndex })
            .OrderBy(entry => presentationOrder.TryGetValue(entry.Track.Type, out var order)
                ? order
                : presentationOrder.Count + entry.SourceIndex)
            .Select(entry => entry.Track)
            .ToList();
    }

    private static void AddComponent(
        IEnumerable<TrackDefinitionBasic> tracks,
        int eventType,
        BasicEventComponent component)
    {
        var track = tracks.FirstOrDefault(x => x.Type == eventType);
        // Preserve the supported track list; component discovery only enriches tracks already exported for the toolbar.
        if (track != null) track.Components |= component;
    }

    private static void AddComponentIfValid(
        IEnumerable<TrackDefinitionBasic> tracks,
        string eventType,
        BasicEventComponent component)
    {
        // Paired effects can use VoidEvent for either side, so ignore registrations without a real event type.
        if (ConvertUtils.ToEventType(eventType, out var type) && type != (int)Beatmap.Enums.EventTypeValue.VoidEvent)
            AddComponent(tracks, type, component);
    }
}

public class EnvFogDefinition
{
    public float Offset;
    public float Height;
    public float StartY;
    public float Attenuation;
    public float AutoExposureLimit;

    public void CopyTo(BloomFogParams copy)
    {
        copy.Offset = Offset;
        copy.Height = Height;
        copy.StartY = StartY;
        copy.Attenuation = Attenuation;
        copy.AutoExposureLimit = AutoExposureLimit;
    }
}

public class EnvSizeData
{
    public string FloorType;
    public string CeilingType;
    public string TrackLaneType;

    public void CopyTo(EnvironmentSizeData copy)
    {
        copy.FloorType = Enum.Parse<FloorType>(FloorType);
        copy.CeilingType = Enum.Parse<CeilingType>(CeilingType);
        copy.TrackLaneType = Enum.Parse<TrackLaneType>(TrackLaneType);
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
    [JsonProperty("shaderProperties")] public Dictionary<string, dynamic> ShaderProps;

    [JsonProperty("enabledShaderKeywords")]
    public string[] Keywords;
}

public class EnvInfoMesh
{
    public string Hash;
    public string Name;
    public Vector3 BoundsSize;
    public Vector3 BoundsCenter;
}
