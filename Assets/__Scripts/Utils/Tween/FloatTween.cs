using System;
using UnityEngine;

public class FloatTween
{
    public float StartTime;
    public float EndTime;

    public float StartValue;
    public float EndValue;

    public float Current;

    public Func<float, float> Easing = global::Easing.Step;

    // Catmull-Rom spline mode: StartValue/EndValue are the inner controls, with the neighboring states as
    // the outer ones. Every tween preparation path must either call SetSpline or leave UseSpline false.
    public bool UseSpline;
    private float splineM1;
    private float splineM2;

    // Neighbor times are the actual generated node times; a missing outer control duplicates the inner
    // endpoint's value one segment duration outward so both segments touching a node agree on its
    // velocity. startAlpha is the current node's profile, endAlpha the destination's.
    public void SetSpline(
        float previousValue,
        float previousTime,
        float nextValue,
        float nextTime,
        float startAlpha,
        float endAlpha)
    {
        UseSpline = true;
        global::Easing.CatmullRomTimeTangents(
            previousValue,
            StartValue,
            EndValue,
            nextValue,
            previousTime,
            StartTime,
            EndTime,
            nextTime,
            startAlpha,
            endAlpha,
            out splineM1,
            out splineM2);
    }

    public void ClearSpline() => UseSpline = false;

    public bool UpdateTime(float time)
    {
        var t = Mathf.InverseLerp(StartTime, EndTime, time);
        return Current
            != (Current = UseSpline
                ? global::Easing.Hermite(StartValue, EndValue, splineM1, splineM2, t)
                : Mathf.LerpUnclamped(StartValue, EndValue, Easing(t)));
    }
}
