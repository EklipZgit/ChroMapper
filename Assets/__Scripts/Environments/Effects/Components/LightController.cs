using UnityEngine;

public class LightController : BaseLightController
{
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);
    public LightObject BoxLight;
    public LightObject SpriteLight;
    public LightObjectBloomFog BloomFog;

    private bool useBoxLight;
    private bool useSpriteLight;
    private bool useBloomFog;

    private void Start()
    {
        useBoxLight = BoxLight != null;
        useSpriteLight = SpriteLight != null;
        useBloomFog = BloomFog != null;
    }

    public override void UpdateTime(float time)
    {
        var nTimeAlpha = Mathf.InverseLerp(StartTimeAlpha, EndTimeAlpha, time);
        var nTimeColor = Mathf.InverseLerp(StartTimeColor, EndTimeColor, time);
        var color = UseHSV
            ? LerpHSV(StartColor, EndColor, Easing(nTimeColor))
            : Color.LerpUnclamped(StartColor, EndColor, Easing(nTimeColor));
        var alpha = Mathf.LerpUnclamped(StartAlpha, EndAlpha, Easing(nTimeAlpha));

        if (StartStrobeFrequency > 0 || EndStrobeFrequency > 0)
        {
            var strobeFadeAlpha = Mathf.LerpUnclamped(StartStrobeBrightness, EndStrobeBrightness, nTimeAlpha);
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
                fadeColor.a *= strobeFadeAlpha;
                color = Color.LerpUnclamped(
                    color,
                    fadeColor,
                    global::Easing.Cubic.InOut(1f - Mathf.Abs((half * 2f) - 1f)));
            }
            else if (half > 0.5f)
                color.a *= strobeFadeAlpha;
            else
                color.a *= alpha;
        }
        else
            color.a *= alpha;

        if (LastColor == color) return;
        LastColor = color;

        // These are basically cached null checks to avoid doing them every frame
        if (useBoxLight) BoxLight.UpdateLighting(color);
        if (useSpriteLight) SpriteLight.UpdateLighting(color);
        if (useBloomFog) BloomFog.UpdateLighting(color);
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
