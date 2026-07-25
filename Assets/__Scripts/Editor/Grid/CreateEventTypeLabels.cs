using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;

public class CreateEventTypeLabels : MonoBehaviour
{
    public GameObject LabelPrefab;
    [SerializeField] private BeatmapRuntimeContext context;
    [SerializeField] private Transform target;

    private readonly List<(int id, int type, string nameFilter)> laneObjs = new();

    private Dictionary<int, BasicLightEffect> typeToManager = new();

    // Use this for initialization
    private void Start() => context.OnEnvironmentLoaded += HandleEnvironmentLoaded;
    private void OnDestroy() => context.OnEnvironmentLoaded -= HandleEnvironmentLoaded;

    private void HandleEnvironmentLoaded(EnvironmentDescriptor descriptor)
    {
        typeToManager = descriptor
            .BasicEventEffectManager.GetEffects<BasicLightEffect>()
            .ToDictionary(x => x.type, x => x.effect);
    }

    public void UpdateLabels(EventGridContainer.PropMode propMode, int eventType, int lanes)
    {
        foreach (Transform children in target)
        {
            if (children.gameObject.activeSelf) Destroy(children.gameObject);
        }

        laneObjs.Clear();

        if (propMode == EventGridContainer.PropMode.Off)
        {
            // Present ordinary light lanes before control lanes without changing their serialized event-type IDs.
            // Read lane definitions through the dev branch's renamed runtime-context property.
            var entries = context.TracksDefinition.Basic
                .OrderBy(entry => entry.Value.Kind == BasicEventKind.Lights ? 0 : 1)
                .ToList();
            var lane = 0;
            for (var i = 0; i < entries.Count; i++)
            {
                AddLabel(lane++, entries[i].Value.Type, null, entries[i].Value.Name);
                // Name filters belong to tracks consumed by ring-rotation components, regardless of event-type number.
                if (!entries[i].Value.Components.HasFlag(BasicEventComponent.RingRotation)) continue;

                var filters = BeatSaberSongContainer.Instance.Map.Events
                    .Where(x => x.Type == entries[i].Value.Type && !string.IsNullOrEmpty(x.CustomNameFilter))
                    .Select(x => x.CustomNameFilter)
                    .Distinct()
                    .OrderBy(x => x);
                foreach (var filter in filters) AddLabel(lane++, entries[i].Value.Type, filter, filter);
            }
        }
        else
        {
            for (var i = 0; i < lanes; i++)
            {
                var instantiate = Instantiate(LabelPrefab, target);
                var laneInfo = (i, i, (string)null);
                instantiate.SetActive(true);
                instantiate.transform.localPosition =
                    new Vector3(i, 0, 0);
                laneObjs.Add(laneInfo);

                try
                {
                    var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                    textMesh.text = i == 0
                        ? "All Lights"
                        : $"{context.TracksDefinition.GetBasicOrDefault(eventType).Name} ID {LaneToLightID(eventType, i - 1)}";
                }
                catch { }
            }
        }
    }

    private void AddLabel(int lane, int eventType, string nameFilter, string label)
    {
        var instantiate = Instantiate(LabelPrefab, target);
        instantiate.SetActive(true);
        instantiate.transform.localPosition = new Vector3(lane, 0, 0);
        laneObjs.Add((lane, eventType, nameFilter));

        try
        {
            var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.text = label;
        }
        catch { }
    }

    public int LaneIdToEventType(int laneId)
    {
        if (laneId < 0 || laneId >= laneObjs.Count) return -1;
        return laneObjs[laneId].type;
    }

    public int EventToLaneId(BaseEvent data)
    {
        foreach (var (id, type, nameFilter) in laneObjs)
        {
            if (type != data.Type) continue;
            if (nameFilter == data.CustomNameFilter) return id;
        }

        return EventTypeToLaneId(data.Type);
    }

    public int EventTypeToLaneId(int eventType)
    {
        foreach (var (id, type, _) in laneObjs)
        {
            if (type != eventType) continue;
            return id;
        }

        return -1;
    }

    // Expose the visible basic-event lane mirror so the mirror command can move ordinary light events between displayed lanes.
    public int MirroredEventType(BaseEvent data)
    {
        // Mirror only among visible light lanes; control/event lanes must not become mirror destinations.
        var lightTypes = laneObjs
            .Where(entry => context.TracksDefinition.GetBasicOrDefault(entry.type).Kind == BasicEventKind.Lights)
            .Select(entry => entry.type)
            .Distinct()
            .ToList();
        var index = lightTypes.IndexOf(data.Type);
        return index >= 0 ? lightTypes[lightTypes.Count - 1 - index] : data.Type;
    }

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return null;

        var id = manager.LaneToLightIDs.FindIndex(x => Array.Exists(
            x,
            y => Array.Exists(lightID, z => z == y)));

        return id != -1 ? id : null;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return Array.Empty<int>();

        return 0 <= propID && propID < manager.LaneToLightIDs.Count
            ? manager.LaneToLightIDs[propID]
            : Array.Empty<int>();
    }

    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var id in PropIdToLightIds(type, propID)) result.Add(id);
        return result;
    }

    public int LaneToLightID(int type, int lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return -1;
        return lightID >= 0 && lightID < manager.LaneToLightID.Count ? manager.LaneToLightID[lightID] : -1;
    }

    public int LightIDToLane(int type, int lightID)
    {
        if (!typeToManager.TryGetValue(type, out var manager)) return -1;
        return manager.LightIDToLane.GetValueOrDefault(lightID, -1);
    }

    // Resolve multi-ID events through displayed physical lanes so hidden IDs cannot become an anchor.
    public int LightIDsToVisibleLane(int type, IEnumerable<int> lightIDs)
    {
        if (lightIDs == null) return -1;
        return lightIDs
            .Select(lightID => LightIDToLane(type, lightID))
            .Where(lane => lane >= 0)
            .DefaultIfEmpty(-1)
            .Min();
    }

    public int LightIDsToPropID(int type, int[] lightIDs)
    {
        if (!typeToManager.TryGetValue(type, out var manager) || lightIDs == null) return -1;
        foreach (var lightID in lightIDs)
        {
            for (var index = 0; index < manager.LaneToLightIDs.Count; index++)
            {
                var id = manager.LaneToLightIDs[index];
                if (!id.Contains(lightID)) continue;
                return index;
            }
        }

        return -1;
    }

    public int MaxLaneId() => laneObjs.Count - 1;

    public int LaneCount => laneObjs.Count;
}
