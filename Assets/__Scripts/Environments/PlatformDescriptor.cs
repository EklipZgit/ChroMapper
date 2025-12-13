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
    public BasicEventEffectController basicEventEffectController;
    // public LightColorGroupEffectController lightColorGroupEffectController;
    // public LightRotationGroupEffectController lightRotationGroupEffectController;
    // public LightTranslationGroupEffectController lightTranslationGroupEffectController;
    // public FloatFxGroupEffectController floatFxGroupEffectController;

    private MonoBehaviour[] activeEffect;

    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManagerBase BigRingManager;

    [Tooltip("Leave null if you do not want gaga environment disks.")]
    public GagaDiskManager DiskManager;

    [Header("Lighting Groups")] [Tooltip("Manually map an Event ID (Index) to a group of lights (LightingManagers)")]
    public BasicLightManager[] LightingManagers = { };

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
    private ColorBoostManager colorBoostManager;

    public bool SoloAnEventType { get; private set; }
    public int SoloEventType { get; private set; }

    // loading happens too fast now
    private void Awake()
    {
        colorBoostManager = gameObject.AddComponent<ColorBoostManager>();
    }

    private void Start()
    {
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.Atsc;
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        BasicLightManager.FlashTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FlashTimeSecond);
        BasicLightManager.FadeTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FadeTimeSecond);
        BasicLightManager.ColorScheme = ColorScheme;

        if (Settings.Instance.HideDisablableObjectsOnLoad) ToggleDisablableObjects();
    }

    private void OnDestroy()
    {
        foreach (var manager in LightingManagers.Where(manager => manager != null))
            colorBoostManager.OnStateChanged -= manager.ToggleBoost;
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
