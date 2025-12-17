using System;
using System.Linq;
using UnityEngine;

public class PlatformDescriptor : MonoBehaviour
{
    [SerializeField] public BasicEventEffectManager BasicEventEffectManager;
    // public LightColorGroupEffectManager lightColorGroupEffectManager;
    // public LightRotationGroupEffectManager lightRotationGroupEffectManager;
    // public LightTranslationGroupEffectManager lightTranslationGroupEffectManager;
    // public FloatFxGroupEffectManager floatFxGroupEffectManager;

    [SerializeField] public PlatformColorScheme ColorScheme = new();
    [NonSerialized] public readonly PlatformColorScheme RuntimeColorScheme = new();
    [SerializeField] public EnvironmentTrackDefinition TrackDefinition = new();
    [SerializeField] public BloomFogParams BloomFogParams = new();

    // below is old
    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManagerBase BigRingManager;

    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    private void Awake()
    {
        TrackDefinition.Initialize();
        RuntimeColorScheme.Copy(ColorScheme);
    }

    private void Start()
    {
        var rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        var atsc = rotationCallback.Atsc;
        BasicEventEffectManager.Initialize(atsc, RuntimeColorScheme);
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        BasicLightManager.FlashTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FlashTimeSecond);
        BasicLightManager.FadeTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FadeTimeSecond);
    }
}
