using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3LightColorBase
    {
        // V3ColorExtendedEasingRoundTripsWithoutChangingTransition uses a numeric save-enum extension;
        // keeping its key here separates GLS color easing from basic events' string-valued easing.
        public const string CustomKeyEasing = "easing";

        // GLSEasingTypeRibbonInputTest: GLS color transitions own a separate easingType key (authored "HSV"
        // or absent RGB) so it never collides with the Basic Event lerpType schema.
        public const string CustomKeyEasingType = "easingType";

        // V3ColorOtherKnownEasingsRoundTrip and VanillaGLSEasingRequirementsRespectV3ColorSchema share
        // this boundary: V3 color needs the plugin for every known curve beyond native None/Linear.
        public static bool RequiresCustomEasing(int easing) =>
            (easing >= (int)EaseType.InQuadratic && easing <= (int)EaseType.InOutBounce)
            || (easing >= (int)EaseType.BeatSaberInOutBack && easing <= (int)EaseType.BeatSaberInOutBounce);

        public static BaseLightColorBase GetFromJson(JSONNode node)
        {
            var lightColorBase = new BaseLightColorBase();

            lightColorBase.JsonTime = lightColorBase.RelativeJsonTime = node["b"].AsFloat;
            lightColorBase.Color = node["c"].AsInt;
            lightColorBase.Brightness = node["s"].AsFloat;
            lightColorBase.UsePrevious = node["i"].AsInt == (int)TransitionType.Extend ? 1 : 0;
            lightColorBase.Easing =
                (int)(node["i"].AsInt == (int)TransitionType.Instant ? EaseType.None : EaseType.Linear);
            lightColorBase.Frequency = node["f"].AsInt;
            lightColorBase.StrobeBrightness = node["sb"].AsFloat;
            lightColorBase.StrobeFade = node["sf"].AsInt;
            lightColorBase.CustomData = node["customData"];

            // V3ColorExtendedEasingRoundTripsWithoutChangingTransition retains Extend's selected curve
            // without changing UsePrevious; V3ColorInstantIgnoresStaleCustomEasing keeps Instant authoritative.
            var transition = node["i"].AsInt;
            if ((transition == (int)TransitionType.Interpolate || transition == (int)TransitionType.Extend)
                && lightColorBase.CustomData.HasKey(CustomKeyEasing))
            {
                // V3ColorInvalidCustomEasingKeepsNativeTransition rejects coercion of strings/fractions
                // and unknown IDs rather than silently converting malformed data to another curve.
                var customEasing = lightColorBase.CustomData[CustomKeyEasing];
                if (customEasing.IsNumber
                    && customEasing.AsDouble == customEasing.AsInt
                    && RequiresCustomEasing(customEasing.AsInt))
                {
                    lightColorBase.Easing = customEasing.AsInt;
                }
            }

            return lightColorBase;
        }

        public static JSONNode ToJson(BaseLightColorBase lightColorBase)
        {
            JSONNode node = new JSONObject();
            node["b"] = lightColorBase.RelativeJsonTime;
            node["c"] = lightColorBase.Color;
            node["s"] = lightColorBase.Brightness;
            node["i"] = (int)(lightColorBase.UsePrevious == 1 ? TransitionType.Extend :
                lightColorBase.Easing == (int)EaseType.None   ? TransitionType.Instant : TransitionType.Interpolate);
            node["f"] = lightColorBase.Frequency;
            node["sb"] = lightColorBase.StrobeBrightness;
            node["sf"] = lightColorBase.StrobeFade;
            // V3ColorEasingSaveUsesModelRatherThanStaleCustomData protects source/previous JSON snapshots:
            // SaveCustom normalizes in place, so detach before updating a generated easing extension.
            var requiresCustomEasing = RequiresCustomEasing(lightColorBase.Easing);
            if (requiresCustomEasing || lightColorBase.CustomData.HasKey(CustomKeyEasing))
                lightColorBase.SetCustomData(lightColorBase.CustomData.Clone());

            // V3ColorReturningToNativeEasingRemovesStaleExtension makes the authored Easing authoritative;
            // other custom fields keep their normal serialization while None/Linear stop requiring the plugin.
            var customData = lightColorBase.SaveCustom();
            if (requiresCustomEasing)
                customData[CustomKeyEasing] = lightColorBase.Easing;
            else
                customData.Remove(CustomKeyEasing);

            lightColorBase.CustomData = customData;
            if (!customData.Children.Any())
                return node;
            node["customData"] = customData;
            return node;
        }
    }
}
