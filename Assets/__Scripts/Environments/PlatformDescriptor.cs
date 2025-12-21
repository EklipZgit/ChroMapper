using System;
using System.Linq;
using UnityEngine;

public class EnvironmentDescriptor : MonoBehaviour
{
    public string ID;

    [SerializeField] public BasicEventEffectManager BasicEventEffectManager;
    // public LightColorGroupEffectManager lightColorGroupEffectManager;
    // public LightRotationGroupEffectManager lightRotationGroupEffectManager;
    // public LightTranslationGroupEffectManager lightTranslationGroupEffectManager;
    // public FloatFxGroupEffectManager floatFxGroupEffectManager;

    [SerializeField] public BloomFogParams BloomFogParams = new();

    // below is old
    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManagerBase BigRingManager;

    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    private void Start()
    {
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        var context = FindAnyObjectByType<BeatmapRuntimeContext>();
        BasicEventEffectManager.Initialize(context.Atsc, context.ColorScheme);
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        BasicLightManager.FlashTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightManager.FlashTimeSecond);
        BasicLightManager.FadeTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightManager.FadeTimeSecond);
    }
}
