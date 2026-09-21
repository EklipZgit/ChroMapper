using System;
using UnityEngine;

[Serializable]
public sealed class BloomFogParams
{
    public float Offset;
    public float Height;
    public float StartY;
    public float Attenuation;
    public float AutoExposureLimit;
    public bool LegacyAutoExposure;

    [SerializeField] private bool defaultsCaptured;
    [SerializeField] private float defaultOffset;
    [SerializeField] private float defaultHeight;
    [SerializeField] private float defaultStartY;
    [SerializeField] private float defaultAttenuation;
    [SerializeField] private float defaultAutoExposureLimit;
    [SerializeField] private bool defaultLegacyAutoExposure;

    public void CaptureDefaults()
    {
        defaultOffset = Offset;
        defaultHeight = Height;
        defaultStartY = StartY;
        defaultAttenuation = Attenuation;
        defaultAutoExposureLimit = AutoExposureLimit;
        defaultLegacyAutoExposure = LegacyAutoExposure;
        defaultsCaptured = true;
    }

    public void ResetToDefaults()
    {
        if (!defaultsCaptured)
        {
            CaptureDefaults();
            return;
        }

        Offset = defaultOffset;
        Height = defaultHeight;
        StartY = defaultStartY;
        Attenuation = defaultAttenuation;
        AutoExposureLimit = defaultAutoExposureLimit;
        LegacyAutoExposure = defaultLegacyAutoExposure;
    }
}
