using System;
using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class EnvironmentDescriptor : MonoBehaviour
{
    public string ID;

    [SerializeField] public BasicEventEffectManager BasicEventEffectManager;
    [SerializeField] public LightColorGroupEffectManager LightColorGroupEffectManager;

    [SerializeField] public LightRotationGroupEffectManager LightRotationGroupEffectManager;
    // [SerializeField] public LightTranslationGroupEffectManager LightTranslationGroupEffectManager;
    // [SerializeField] public FloatFxGroupEffectManager FloatFxGroupEffectManager;

    [SerializeField] public BloomFogParams BloomFogParams = new();

    // below is old
    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public BaseTrackLaneRingsManager SmallRingManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public BaseTrackLaneRingsEffect BigRingManager;

    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    private void Start()
    {
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        var context = FindAnyObjectByType<BeatmapRuntimeContext>();
        BasicEventEffectManager.Initialize(context.Atsc, context.ColorScheme);
        LightColorGroupEffectManager.Initialize(context.Atsc, context.ColorScheme);
        LightRotationGroupEffectManager.Initialize(context.Atsc);
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        BasicLightEffect.FlashTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FlashTimeSecond);
        BasicLightEffect.FadeTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FadeTimeSecond);
    }
}
