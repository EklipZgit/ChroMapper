using UnityEngine;

public class LightController : BaseLightController
{
    public static readonly float HDRIntensity = Mathf.GammaToLinearSpace(2.4169f);
    public LightObject LightObject;

    public override void UpdateTime(float time)
    {
        var nTimeAlpha = (time - StartTimeAlpha) / (EndTimeAlpha - StartTimeAlpha);
        var nTimeColor = (time - StartTimeColor) / (EndTimeColor - StartTimeColor);
        var color = UseHSV
            ? LerpHSV(StartColor, EndColor, Easing(nTimeColor))
            : Color.Lerp(StartColor, EndColor, Easing(nTimeColor));
        var alpha = Mathf.Lerp(StartAlpha, EndAlpha, Easing(nTimeAlpha));

        color.a *= alpha;
        LightObject.UpdateLighting(color);
    }

    private static Color LerpHSV(Color start, Color end, float t)
    {
        Color.RGBToHSV(start, out var sH, out var sS, out var sV);
        Color.RGBToHSV(end, out var eH, out var eS, out var eV);
        var hue = Mathf.LerpAngle(sH * 360f, eH * 360f, t);
        return Color
            .HSVToRGB(
                Mathf.Repeat(hue, 360f) / 360f,
                Mathf.Lerp(sS, eS, t),
                Mathf.Lerp(sV, eV, t))
            .WithAlpha(Mathf.Lerp(start.a, end.a, t));
    }

    public override void UpdateBoostState(bool boost) => LightObject.UpdateBoostState(boost);
}
