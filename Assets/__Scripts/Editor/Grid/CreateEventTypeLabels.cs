using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;

public class CreateEventTypeLabels : MonoBehaviour
{
    public Material AvailableMaterial;
    public Material UtilityMaterial;
    public Material RedMaterial;
    public GameObject LabelPrefab;
    [SerializeField] private BeatmapRuntimeContext context;

    private readonly List<(int id, int type)> laneObjs = new();

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
        foreach (Transform children in transform)
        {
            if (children.gameObject.activeSelf) Destroy(children.gameObject);
        }

        laneObjs.Clear();

        if (propMode == EventGridContainer.PropMode.Off)
        {
            var entries = context.TracksDefinition.Basic.ToList();
            for (var i = 0; i < entries.Count; i++)
            {
                var instantiate = Instantiate(LabelPrefab, transform);
                var laneInfo = (i, entries[i].Value.Type);
                instantiate.SetActive(true);
                instantiate.transform.localPosition = new Vector3(i, 0, 0);
                laneObjs.Add(laneInfo);

                try
                {
                    var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                    textMesh.text = entries[i].Value.Name;
                    textMesh.fontSharedMaterial = UtilityMaterial;
                }
                catch { }
            }
        }
        else
        {
            for (var i = 0; i < lanes; i++)
            {
                var instantiate = Instantiate(LabelPrefab, transform);
                var laneInfo = (i, i);
                instantiate.SetActive(true);
                instantiate.transform.localPosition =
                    new Vector3(i, 0, 0);
                laneObjs.Add(laneInfo);

                try
                {
                    var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                    textMesh.fontSharedMaterial = UtilityMaterial;
                    if (i == 0)
                    {
                        textMesh.text = "All Lights";
                        textMesh.fontSharedMaterial = RedMaterial;
                    }
                    else
                    {
                        textMesh.text =
                            $"{context.TracksDefinition.GetBasicOrDefault(eventType).Name} ID {LaneToLightID(eventType, i - 1)}";
                        textMesh.fontSharedMaterial = i % 2 == 0 ? UtilityMaterial : AvailableMaterial;
                    }
                }
                catch { }
            }
        }
    }

    public int LaneIdToEventType(int laneId) => laneObjs[laneId].type;

    public int EventTypeToLaneId(int eventType)
    {
        var idx = laneObjs.FindIndex(it => it.type == eventType);
        return idx == -1 ? -1 : laneObjs[idx].id;
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

    public int LightIDsToPropID(int type, int[] lightIDs)
    {
        if (!typeToManager.TryGetValue(type, out var manager) || lightIDs == null) return -1;
        foreach (var lightID in lightIDs)
        {
            var idx = manager.LaneToLightIDs.FindIndex(x => x.Contains(lightID));
            if (idx != -1) return idx;
        }

        return -1;
    }

    public int MaxLaneId() => laneObjs.Count - 1;
}
