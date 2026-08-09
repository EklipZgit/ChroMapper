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
    public Color StartStrobeColor;

    public float EndTimeAlpha;
    public float EndTimeColor;
    public Color EndColor;
    public float EndAlpha;
    public float EndStrobeFrequency;
    public float EndStrobeBrightness;
    public Color EndStrobeColor;

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

        if (StartStrobeFrequency > 0 || EndStrobeFrequency > 0)
        {
            // Interpolate strobe brightness between start and end
            // When strobe brightness is 0, the light should be black (off) during the strobe on phase
            // When strobe brightness is > 0, use the strobe brightness as the alpha channel.
            //   Fixes bug where a 1/2 strobe node stays a solid color in CM rather than matching game rendering of flashing to 0 brightness.
            // Use the event transition easing here as well as for normal brightness. Step easing keeps a non-transition node from fading between strobe levels.
            var strobeBrightness = Mathf.LerpUnclamped(
                StartStrobeBrightness,
                EndStrobeBrightness,
                Easing(nTimeAlpha));

            var duration = EndTimeAlpha - StartTimeAlpha;
            var elapsed = nTimeAlpha * duration;
            var elapsedHalf = elapsed * elapsed / (2f * duration);
            
            // The strobe frequency from JSON is in "cycles per beat"
            // The phase calculation uses quadratic interpolation between start and end frequencies
            // When strobe frequency is constant (e.g., 2), this simplifies to: phase = (frequency * elapsed) % 1f
            var phase = (((0f - StartStrobeFrequency) * elapsedHalf)
                    + (StartStrobeFrequency * elapsed)
                    + (EndStrobeFrequency * elapsedHalf))
                % 1f;

            // Interpolate strobe color between start and end
            var strobeColor = Color.LerpUnclamped(StartStrobeColor, EndStrobeColor, Easing(nTimeColor));
            // If no explicit strobe color, fall back to normal color
            if (StartStrobeColor == Color.clear && EndStrobeColor == Color.clear)
            {
                strobeColor = color;
            }

            // Preserve zero strobe brightness as an off/transparent strobe color; opaque Color.black would not represent a zero light level.
            var useStrobeColor = new Color(strobeColor.r, strobeColor.g, strobeColor.b, strobeBrightness);

            // Apply the base brightness to the off-phase color before mixing the strobe overlay.
            //   Because of the massive bloom at high light levels I can't even tell if this is right or if this just matches a bug we have with base light levels with the strobe light level.
            //   But this right here "correctly" makes the strobe light level bloom match the non-strobe light level bloom.
            color.a *= alpha;

            if (StrobeFade)
            {
                var fade = global::Easing.Cubic.InOut(1f - Mathf.Abs((phase * 2f) - 1f));
                color = Color.LerpUnclamped(color, useStrobeColor, fade);
            }
            else if (phase >= 0.5f)
            {
                color = useStrobeColor;
            }
            // else // off phase: base color already scaled by brightness, nothing to do here.
        }
        else
            color.a *= alpha;

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
