using System;
using UnityEngine;

public abstract class BaseLightController : MonoBehaviour
{
    public float StartTimeAlpha;
    public float StartTimeColor;
    public Color StartColor = Color.white;
    public float StartAlpha;
    public float StartStrobeFrequency;
    public float StartStrobeBrightness;
    
    public float EndTimeAlpha;
    public float EndTimeColor;
    public Color EndColor = Color.white;
    public float EndAlpha;
    public float EndStrobeFrequency;
    public float EndStrobeBrightness;

    public bool StrobeFade;
    
    public bool UseHSV;
    public Func<float, float> Easing = global::Easing.ByName["easeLinear"];

    public abstract void UpdateTime(float time);
    public abstract void UpdateBoostState(bool boost);
}
