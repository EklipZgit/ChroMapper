using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventCommon
{
    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        var color = evt.Color == (int)LightColor.Red
            ? boost ? eventAppearance.RedBoostColor : eventAppearance.RedColor
            : evt.Color == (int)LightColor.Blue
                ? boost ? eventAppearance.BlueBoostColor : eventAppearance.BlueColor
                : boost
                    ? eventAppearance.WhiteBoostColor
                    : eventAppearance.WhiteColor;

        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, 0.25f);
        color = Color.Lerp(clampedOffColor, color, evt.Brightness);

        return color;
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
