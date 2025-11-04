using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlatformDescriptor : MonoBehaviour
{
    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsStateManager SmallRingStateManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsStateManagerBase BigRingStateManager;

    [FormerlySerializedAs("DiskManager")] [Tooltip("Leave null if you do not want gaga environment disks.")]
    public GagaDiskStateManager DiskStateManager;

    [Header("Lighting Groups")] [Tooltip("Manually map an Event ID (Index) to a group of lights (LightingManagers)")]
    public BasicLightStateManager[] LightingManagers = { };

    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    [FormerlySerializedAs("Colors")] [FormerlySerializedAs("colors")] [HideInInspector]
    public PlatformColorScheme ColorScheme;

    [FormerlySerializedAs("DefaultColors")] [FormerlySerializedAs("defaultColors")]
    public PlatformColorScheme DefaultColorScheme = new();

    [Tooltip(
        "-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special | 2 = New lanes 6/7 + 16/17 | 3 = Gaga Lanes")]
    public int SortMode;

    [Tooltip("Objects to disable through the L keybind, like lights and static objects in 360 environments.")]
    public GameObject[] DisablableObjects;

    private AudioTimeSyncController atsc;
    private ColorBoostStateManager colorBoostStateManager;

    public readonly Dictionary<int, List<StateManager<BaseEvent>>> EventTypeManagerMap = new();
    public readonly List<StateManager<BaseEvent>> SortedPriorityManagers = new();

    public event Action OnRefreshed;

    public bool SoloAnEventType { get; private set; }
    public int SoloEventType { get; private set; }

    // loading happens too fast now
    private void Awake()
    {
        colorBoostStateManager = gameObject.AddComponent<ColorBoostStateManager>();

        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
        {
            LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;
            LoadedDifficultySelectController.OnLoadedDifficultyChanged += HandleLevelLoaded;
        }
    }

    private void OnDestroy()
    {
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding")
        {
            LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
            LoadedDifficultySelectController.OnLoadedDifficultyChanged -= HandleLevelLoaded;
        }

        foreach (var manager in LightingManagers.Where(manager => manager != null))
            colorBoostStateManager.OnStateChanged -= manager.ToggleBoostState;
    }

    private void HandleLevelLoaded()
    {
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.Atsc;
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        RefreshPlatform();
    }

    public void RefreshPlatform() => StartCoroutine(PlatformLoadFromHell());

    // first off, what the fuck
    private IEnumerator PlatformLoadFromHell()
    {
        yield return new WaitForEndOfFrame(); // Actually wait for platform to fully load from Awake and Start

        BasicLightStateManager.FlashTimeBeat = atsc.GetBeatFromSeconds(BasicLightStateManager.FlashTimeSecond);
        BasicLightStateManager.FadeTimeBeat = atsc.GetBeatFromSeconds(BasicLightStateManager.FadeTimeSecond);
        BasicLightStateManager.ColorScheme = ColorScheme;

        SortedPriorityManagers.Clear();
        EventTypeManagerMap.Clear();

        for (var type = 0; type < LightingManagers.Length; type++)
        {
            var manager = LightingManagers[type];
            if (manager is null) continue;
            colorBoostStateManager.OnStateChanged += manager.ToggleBoostState;
            MapEventManager(manager, type);
        }

        MapEventManager(colorBoostStateManager, 5);

        if (BigRingStateManager != null)
        {
            BigRingStateManager.RingFilter = RingFilter.Big;
            MapEventManager(BigRingStateManager, 8);
            MapEventManager(BigRingStateManager, 9);
        }

        if (SmallRingStateManager != null)
        {
            SmallRingStateManager.RingFilter = RingFilter.Small;
            MapEventManager(SmallRingStateManager, 8);
            MapEventManager(SmallRingStateManager, 9);
        }

        if (DiskStateManager != null)
        {
            MapEventManager(DiskStateManager, 12);
            MapEventManager(DiskStateManager, 13);
            MapEventManager(DiskStateManager, 16);
            MapEventManager(DiskStateManager, 17);
            MapEventManager(DiskStateManager, 18);
            MapEventManager(DiskStateManager, 19);
        }

        foreach (var handler in GetComponentsInChildren<PlatformEventStateManager>())
        foreach (var type in handler.ListeningEventTypes)
            MapEventManager(handler, type);

        var leftEventTypes = new List<int>
        {
            (int)EventTypeValue.LeftLasers, (int)EventTypeValue.ExtraLeftLasers, (int)EventTypeValue.ExtraLeftLights
        };
        foreach (var l in leftEventTypes
            .Where(t => t <= LightingManagers.Length)
            .SelectMany(eventType => LightingManagers[eventType].RotatingLights))
            MapEventManager(l, 12);
        var rightEventTypes = new List<int>
        {
            (int)EventTypeValue.RightLasers,
            (int)EventTypeValue.ExtraRightLasers,
            (int)EventTypeValue.ExtraRightLights
        };
        foreach (var l in rightEventTypes
            .Where(t => t <= LightingManagers.Length)
            .SelectMany(eventType => LightingManagers[eventType].RotatingLights))
        {
            l.Mirror = true;
            MapEventManager(l, 13);
        }

        foreach (var manager in EventTypeManagerMap.Values.SelectMany(manager => manager))
        {
            manager.Atsc = atsc;
            SortedPriorityManagers.Add(manager);
        }

        OnRefreshed?.Invoke();

        if (Settings.Instance.HideDisablableObjectsOnLoad) ToggleDisablableObjects();
    }

    private void MapEventManager(StateManager<BaseEvent> manager, int type)
    {
        if (!EventTypeManagerMap.ContainsKey(type)) EventTypeManagerMap.Add(type, new());
        EventTypeManagerMap[type].Add(manager);
    }

    public void UpdateSoloEventType(bool solo, int soloTypeID)
    {
        SoloAnEventType = solo;
        SoloEventType = soloTypeID;
    }

    public void ToggleDisablableObjects()
    {
        foreach (var go in DisablableObjects) go.SetActive(!go.activeInHierarchy);
    }
}
