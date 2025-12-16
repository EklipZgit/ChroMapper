using System;
using UnityEngine;

public abstract class BaseLightController : MonoBehaviour
{
    public bool OverrideLightGroup;
    public int OverrideLightGroupID;

    public int ID;
    public int PropGroup;

    public float StartTimeAlpha;
    public float StartTimeColor;
    public Color StartColor = Color.white;
    public float StartAlpha;
    public float EndTimeAlpha;
    public float EndTimeColor;
    public Color EndColor = Color.white;
    public float EndAlpha;
    public bool UseHSV;
    public Func<float, float> Easing = global::Easing.ByName["easeLinear"];

    public abstract void UpdateTime(float time);
    public abstract void UpdateBoostState(bool boost);
}
