using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EnvironmentDescriptor : MonoBehaviour
{
    public string ID;

    [SerializeField] public BasicEventEffectManager BasicEventEffectManager;
    [SerializeField] public LightColorGroupEffectManager LightColorGroupEffectManager;
    [SerializeField] public LightRotationGroupEffectManager LightRotationGroupEffectManager;
    [SerializeField] public LightTranslationGroupEffectManager LightTranslationGroupEffectManager;
    [SerializeField] public FloatFxGroupEffectManager FloatFxGroupEffectManager;

    [SerializeField] public BloomFogParams BloomFogParams = new();

    public List<ChromaIDMarker> ChromaIDMarkers = new();

    private bool hasInitialized;

    // below is old
    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    public void Initialize(BeatmapRuntimeContext context)
    {
        hasInitialized = true;
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();

        BasicEventEffectManager.Initialize(context.Atsc, context.ColorScheme);
        LightColorGroupEffectManager.Initialize(context.Atsc, context.ColorScheme);
        LightRotationGroupEffectManager.Initialize(context.Atsc);
        LightTranslationGroupEffectManager.Initialize(context.Atsc);
        FloatFxGroupEffectManager.Initialize(context.Atsc);

        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        BasicLightEffect.FlashTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FlashTimeSecond);
        BasicLightEffect.FadeTimeBeat = context.Atsc.GetBeatFromSeconds(BasicLightEffect.FadeTimeSecond);
    }

    public void Reinitialize()
    {
        BasicEventEffectManager.Reinitialize();
        LightColorGroupEffectManager.Reinitialize();
        LightRotationGroupEffectManager.Reinitialize();
        LightTranslationGroupEffectManager.Reinitialize();
        FloatFxGroupEffectManager.Reinitialize();
    }

    public void Refresh()
    {
        BasicEventEffectManager.Refresh();
        LightColorGroupEffectManager.Refresh();
        LightRotationGroupEffectManager.Refresh();
        LightTranslationGroupEffectManager.Refresh();
        FloatFxGroupEffectManager.Refresh();
    }


    public void Register(LightController controller, bool strict = true)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                BasicEventEffectManager.Register(controller, strict);
                break;
            case LightController.LightKind.Group:
                LightColorGroupEffectManager.Register(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void Unregister(LightController controller)
    {
        switch (controller.Kind)
        {
            case LightController.LightKind.Basic:
                BasicEventEffectManager.Unregister(controller);
                break;
            case LightController.LightKind.Group:
                LightColorGroupEffectManager.Unregister(controller);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
