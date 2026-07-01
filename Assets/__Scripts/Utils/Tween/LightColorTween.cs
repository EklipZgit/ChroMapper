using System;
using UnityEngine;

public class LightColorTween
{
    public float StartTimeAlpha;
    public float StartTimeColor;
    public Color StartColor;
    public float StartAlpha;
    public float StartStrobeFrequency;
    public float StartStrobeBrightness;

    public float EndTimeAlpha;
    public float EndTimeColor;
    public Color EndColor;
    public float EndAlpha;
    public float EndStrobeFrequency;
    public float EndStrobeBrightness;

    public bool StrobeFade;

    public bool UseHSV;
    public Func<float, float> Easing = global::Easing.ByName["easeLinear"];

    public Color Color;

    public bool UpdateTime(float time)
    {
        var nTimeAlpha = Mathf.InverseLerp(StartTimeAlpha, EndTimeAlpha, time);
        var nTimeColor = Mathf.InverseLerp(StartTimeColor, EndTimeColor, time);
        var color = UseHSV
            ? LerpHSV(StartColor, EndColor, Easing(nTimeColor))
            : Color.LerpUnclamped(StartColor, EndColor, Easing(nTimeColor));
        var alpha = Mathf.LerpUnclamped(StartAlpha, EndAlpha, Easing(nTimeAlpha));

        // Bake brightness into color alpha upfront, matching game behaviour
        color.a *= alpha;

        if (StartStrobeFrequency > 0 || EndStrobeFrequency > 0)
        {
            var strobePeak = (StartStrobeBrightness <= 0 && EndStrobeBrightness <= 0)
                ? 1f
                : Mathf.LerpUnclamped(StartStrobeBrightness, EndStrobeBrightness, nTimeAlpha);
            var duration = EndTimeAlpha - StartTimeAlpha;
            var elapsed = nTimeAlpha * duration;
            var elapsedHalf = elapsed * elapsed / (2f * duration);
            var half = (((0f - StartStrobeFrequency) * elapsedHalf)
                    + (StartStrobeFrequency * elapsed)
                    + (EndStrobeFrequency * elapsedHalf))
                % 1f;
            if (StrobeFade)
            {
                var fadeColor = color;
                fadeColor.a *= strobePeak;
                color = Color.LerpUnclamped(
                    color,
                    fadeColor,
                    global::Easing.Cubic.InOut(1f - Mathf.Abs((half * 2f) - 1f)));
            }
            else if (half > 0.5f)
                color.a *= strobePeak;
            // off phase: color.a already has brightness baked in, leave it alone
        }

        if (Color == color) return false;
        Color = color;
        return true;
    }

    private static Color LerpHSV(Color start, Color end, float t)
    {
        Color.RGBToHSV(start, out var sH, out var sS, out var sV);
        Color.RGBToHSV(end, out var eH, out var eS, out var eV);
        var hue = Mathf.LerpAngle(sH * 360f, eH * 360f, t);
        return Color
            .HSVToRGB(
                Mathf.Repeat(hue, 360f) / 360f,
                Mathf.LerpUnclamped(sS, eS, t),
                Mathf.LerpUnclamped(sV, eV, t))
            .WithAlpha(Mathf.LerpUnclamped(start.a, end.a, t));
    }
}