using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class BasicLightStateData : BasicEventStateData
{

    public float
        StartTimeColor = float.MinValue; // this is supposedly the same as start time, special case for chroma gradient

    public LightColor StartColor;
    public Color? StartChromaColor;
    public float StartAlpha;

    public float EndTimeAlpha; // similarly this match next start, otherwise used to interpolate flash/fade
    public float EndTimeColor; // also same case above, only special case for chroma gradient
    public LightColor EndColor;
    public Color? EndChromaColor;
    public float EndAlpha;

    public Func<float, float> Easing = global::Easing.Linear;
    public bool UseHSV;

    public BasicLightStateData(BaseEvent evt) : base(evt) { }
}
