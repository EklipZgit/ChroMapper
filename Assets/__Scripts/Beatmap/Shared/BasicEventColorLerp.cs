using UnityEngine;

namespace Beatmap.Shared
{
    // Use the same RGB, HSV, and TrueHSV mode IDs in preview lights and ribbon shaders.
    public enum BasicEventColorLerpType
    {
        RGB = 0,
        LegacyHSV = 1,
        TrueHSV = 2
    }

    // Centralizing classification and CPU interpolation prevents the two HSV semantics from drifting across consumers.
    public static class BasicEventColorLerp
    {
        // Chroma's HSV mode interpolates hue as a number from 0 to 1. TrueHSV takes the shortest path around
        // the hue wheel.
        public static BasicEventColorLerpType FromSerializedName(string lerpType)
        {
            return lerpType switch
            {
                "HSV" => BasicEventColorLerpType.LegacyHSV,
                "TrueHSV" => BasicEventColorLerpType.TrueHSV,
                _ => BasicEventColorLerpType.RGB
            };
        }

        // Preview evaluation is per-frame, so dispatch directly without allocations or repeated serialized-name checks.
        public static Color Interpolate(
            Color start,
            Color end,
            float t,
            BasicEventColorLerpType lerpType)
        {
            return lerpType switch
            {
                BasicEventColorLerpType.LegacyHSV => LerpLegacyHsv(start, end, t),
                BasicEventColorLerpType.TrueHSV => LerpTrueHSV(start, end, t),
                _ => Color.LerpUnclamped(start, end, t)
            };
        }

        public static Color ApplyBrightness(Color color, float brightness)
        {
            color.a *= brightness;
            return color;
        }

        public static Color InterpolateWithBrightness(
            Color start,
            float startBrightness,
            Color end,
            float endBrightness,
            float t,
            BasicEventColorLerpType lerpType) =>
            Interpolate(
                ApplyBrightness(start, startBrightness),
                ApplyBrightness(end, endBrightness),
                t,
                lerpType);

        public static Color InterpolateStrobeFade(
            Color normal,
            Color strobe,
            float t,
            BasicEventColorLerpType lerpType)
        {
            return lerpType switch
            {
                BasicEventColorLerpType.LegacyHSV => LerpLegacyHsv(normal, strobe, t),
                BasicEventColorLerpType.TrueHSV => LerpTrueHSV(normal, strobe, t),
                _ => Color.LerpUnclamped(normal, strobe, t)
            };
        }

        // Legacy Chroma compatibility requires treating normalized hue as an ordinary scalar, including its green detours.
        private static Color LerpLegacyHsv(Color start, Color end, float t)
        {
            Color.RGBToHSV(start, out var startHue, out var startSaturation, out var startValue);
            Color.RGBToHSV(end, out var endHue, out var endSaturation, out var endValue);
            return WithInterpolatedAlpha(
                Color.HSVToRGB(
                    Mathf.LerpUnclamped(startHue, endHue, t),
                    Mathf.LerpUnclamped(startSaturation, endSaturation, t),
                    Mathf.LerpUnclamped(startValue, endValue, t)),
                start,
                end,
                t);
        }

        private static Color LerpTrueHSV(Color start, Color end, float t)
        {
            Color.RGBToHSV(start, out var startHue, out var startSaturation, out var startValue);
            Color.RGBToHSV(end, out var endHue, out var endSaturation, out var endValue);
            var hue = Mathf.LerpAngle(startHue * 360f, endHue * 360f, t);
            return WithInterpolatedAlpha(
                Color.HSVToRGB(
                    Mathf.Repeat(hue, 360f) / 360f,
                    Mathf.LerpUnclamped(startSaturation, endSaturation, t),
                    Mathf.LerpUnclamped(startValue, endValue, t)),
                start,
                end,
                t);
        }

        // Catmull-Rom controls live in the lerp mode's own space so one set of knots drives all channels.
        // TrueHSV unwraps the outer hues to the nearest turn of the inner controls, matching LerpAngle's
        // shortest path; LegacyHSV keeps raw hue per its scalar semantics.
        public static Vector4 ToSplineSpace(Color color, BasicEventColorLerpType lerpType)
        {
            if (lerpType == BasicEventColorLerpType.RGB)
            {
                return color;
            }

            Color.RGBToHSV(color, out var h, out var s, out var v);
            return new Vector4(h, s, v, color.a);
        }

        public static void UnwrapSplineHues(
            BasicEventColorLerpType lerpType,
            ref Vector4 p0,
            Vector4 p1,
            ref Vector4 p2,
            ref Vector4 p3)
        {
            if (lerpType != BasicEventColorLerpType.TrueHSV)
            {
                return;
            }

            // p2 unwraps first so an inner interval crossing the seam keeps p3 on its far side.
            p2.x = p1.x + (Mathf.DeltaAngle(p1.x * 360f, p2.x * 360f) / 360f);
            p0.x = p1.x - (Mathf.DeltaAngle(p0.x * 360f, p1.x * 360f) / 360f);
            p3.x = p2.x + (Mathf.DeltaAngle(p2.x * 360f, p3.x * 360f) / 360f);
        }

        // Evaluate a precomputed spline in lerp space and convert back to RGB. Saturation stays valid
        // while overshoot on HDR value and alpha is preserved, matching the endpoint lerp paths.
        public static Color InterpolateSpline(
            Vector4 p1,
            Vector4 p2,
            Vector4 m1,
            Vector4 m2,
            float t,
            BasicEventColorLerpType lerpType)
        {
            var result = global::Easing.Hermite(p1, p2, m1, m2, t);
            if (lerpType == BasicEventColorLerpType.RGB)
            {
                return new Color(result.x, result.y, result.z, result.w);
            }

            var hue = lerpType == BasicEventColorLerpType.TrueHSV
                ? Mathf.Repeat(result.x, 1f)
                : result.x;
            var color = Color.HSVToRGB(hue, Mathf.Clamp01(result.y), result.z);
            color.a = result.w;
            return color;
        }

        // Both HSV modes retain the existing independent alpha interpolation used by Basic Event brightness handling.
        private static Color WithInterpolatedAlpha(Color color, Color start, Color end, float t)
        {
            color.a = Mathf.LerpUnclamped(start.a, end.a, t);
            return color;
        }
    }
}
