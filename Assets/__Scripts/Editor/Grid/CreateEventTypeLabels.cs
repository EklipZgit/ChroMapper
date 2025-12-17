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
    public RotationCallbackController RotationCallback;

    private readonly List<LaneInfo> laneObjs = new List<LaneInfo>();

    private PlatformDescriptor descriptor;
    private Dictionary<int, BasicLightManager> lightingManagers = new();
    private bool loadedWithRotationEvents;
    public int NoRotationLaneOffset => loadedWithRotationEvents || RotationCallback.IsActive ? 2 : 0;

    // Use this for initialization
    private void Start()
    {
        loadedWithRotationEvents = BeatSaberSongContainer.Instance.Map.Events.Any(i => i.IsLaneRotationEvent());
        LoadInitialMap.OnPlatformLoaded += HandlePlatformLoaded;
    }

    private void OnDestroy() => LoadInitialMap.OnPlatformLoaded -= HandlePlatformLoaded;

    public void UpdateLabels(EventGridContainer.PropMode propMode, int eventType, int lanes)
    {
        foreach (Transform children in transform)
            if (children.gameObject.activeSelf)
                Destroy(children.gameObject);

        laneObjs.Clear();

        if (propMode == EventGridContainer.PropMode.Off)
        {
            for (var i = 0; i < descriptor.TrackDefinition.Basic.Length; i++)
            {
                var modified = i + NoRotationLaneOffset;
                var instantiate = Instantiate(LabelPrefab, transform);
                var laneInfo = new LaneInfo(i, descriptor.TrackDefinition.Basic[i].Type);
                instantiate.SetActive(true);
                instantiate.transform.localPosition =
                    new Vector3(modified, 0, 0);
                laneObjs.Add(laneInfo);

                try
                {
                    var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
                    textMesh.text = descriptor.TrackDefinition.Basic[i].Name;
                    textMesh.fontSharedMaterial = UtilityMaterial;

                    laneInfo.Name = textMesh.text;
                }
                catch { }
            }
        }
        else
        {
            for (var i = 0; i < lanes; i++)
            {
                var instantiate = Instantiate(LabelPrefab, transform);
                var laneInfo = new LaneInfo(i, i);
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
                            $"{lightingManagers[eventType].name} ID {EditorToLightID(eventType, i - 1)}";
                        textMesh.fontSharedMaterial = i % 2 == 0 ? UtilityMaterial : AvailableMaterial;
                    }

                    laneInfo.Name = textMesh.text;
                }
                catch { }
            }
        }

        laneObjs.Sort();
    }

    private void HandlePlatformLoaded(PlatformDescriptor descriptor)
    {
        this.descriptor = descriptor;
        lightingManagers = descriptor
            .BasicEventEffectManager.EventTypeManagerMap.Where(x => x.Value is BasicLightManager)
            .ToDictionary(x => x.Key, x => x.Value as BasicLightManager);
    }

    public int LaneIdToEventType(int laneId) => laneObjs[laneId].Type;

    public int EventTypeToLaneId(int eventType)
    {
        var idx = laneObjs.FindIndex(it => it.Type == eventType);
        return idx == -1 ? -1 : laneObjs[idx].Index;
    }

    public int? LightIdsToPropId(int type, int[] lightID)
    {
        if (!lightingManagers.ContainsKey(type)) return null;

        var light = lightingManagers[type].ControllableLights.Find(x => Array.IndexOf(lightID, x.ID) > -1);

        return light != null ? light.PropGroup : (int?)null;
    }

    public int[] PropIdToLightIds(int type, int propID)
    {
        if (!lightingManagers.ContainsKey(type)) return Array.Empty<int>();

        return lightingManagers[type]
            .ControllableLights.Where(x => x.PropGroup == propID)
            .Select(x => x.ID)
            .OrderBy(x => x)
            .Distinct()
            .ToArray();
    }

    public JSONArray PropIdToLightIdsJ(int type, int propID)
    {
        var result = new JSONArray();
        foreach (var lightingEvent in PropIdToLightIds(type, propID)) result.Add(lightingEvent);
        return result;
    }

    public int EditorToLightID(int type, int lightID) => lightingManagers[type].LightIDPlacementMap[lightID];

    public int LightIDToEditor(int type, int lightID)
    {
        if (lightingManagers[type].LightIDPlacementMapReverse.ContainsKey(lightID))
            return lightingManagers[type].LightIDPlacementMapReverse[lightID];
        return -1;
    }

    public int MaxLaneId() => laneObjs.Count - 1;
}
