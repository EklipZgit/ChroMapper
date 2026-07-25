using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventCommon
{
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;

    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return ApplyBrightness(GetBaseColor(evt, boost, eventAppearance), evt.Brightness, eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        // Strobe corners use their own brightness, falling back to the main event color when no override exists.
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return ApplyBrightness(color, evt.StrobeBrightness, eventAppearance);
    }

    // Keep the existing GLS dimness curve shared by main and strobe sections.
    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

    private static Color GetBaseColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        if (evt.CustomColor.HasValue) return evt.CustomColor.Value;

        return evt.Color == (int)LightColor.Red
            ? boost ? eventAppearance.RedBoostColor : eventAppearance.RedColor
            : evt.Color == (int)LightColor.Blue
                ? boost ? eventAppearance.BlueBoostColor : eventAppearance.BlueColor
                : boost
                    ? eventAppearance.WhiteBoostColor
                    : eventAppearance.WhiteColor;
    }

    public static string GetColorInfo(BaseLightColorBase evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Brightness * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));
        if (evt.Frequency > 0) sb.Append($"1/{evt.Frequency}");
        if (evt.Frequency > 0 && evt.StrobeFade == 1) sb.Append(' ');
        if (evt.StrobeFade == 1) sb.Append('L');
        if ((evt.Frequency > 0 || evt.StrobeFade == 1) && evt.StrobeBrightness > 0f) sb.Append(' ');
        if (evt.StrobeBrightness > 0f) sb.Append((evt.StrobeBrightness * 100f).ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    public static string GetRotationInfo(BaseLightRotationBase evt)
    {
        var sb = new StringBuilder();

        var direction = evt.Direction switch
        {
            (int)LightRotationDirection.Clockwise => "CW",
            (int)LightRotationDirection.CounterClockwise => "CCW",
            _ => "A"
        };

        sb.AppendLine(evt.Rotation.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));
        sb.AppendLine($"{direction} <{evt.Loop}>");

        return sb.ToString();
    }

    public static string GetTranslationInfo(BaseLightTranslationBase evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Translation * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));

        return sb.ToString();
    }

    public static string GetFloatFXInfo(BaseFxEventFloat evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Value * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));

        return sb.ToString();
    }
}
