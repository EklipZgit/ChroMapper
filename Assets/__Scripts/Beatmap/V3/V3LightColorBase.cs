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
        public const string CustomKeyEasing = "easing";

        public const string CustomKeyEasingType = "easingType";

        public static bool RequiresCustomEasing(int easing) =>
            (easing >= (int)EaseType.InQuadratic && easing <= (int)EaseType.InOutBounce)
            || (easing >= (int)EaseType.BeatSaberInOutBack && easing <= (int)EaseType.BeatSaberInOutBounce);

        public static BaseLightColorBase GetFromJson(JSONNode node)
        {
            var lightColorBase = new BaseLightColorBase();

            var iInt = node["i"].AsInt;
            lightColorBase.JsonTime = lightColorBase.RelativeJsonTime = node["b"].AsFloat;
            lightColorBase.Color = node["c"].AsInt;
            lightColorBase.Brightness = node["s"].AsFloat;
            lightColorBase.UsePrevious = iInt == (int)TransitionType.Extend ? 1 : 0;
            lightColorBase.Easing =
                (int)(iInt == (int)TransitionType.Instant ? EaseType.None : EaseType.Linear);
            lightColorBase.Frequency = node["f"].AsInt;
            lightColorBase.StrobeBrightness = node["sb"].AsFloat;
            lightColorBase.StrobeFade = node["sf"].AsInt;
            lightColorBase.CustomData = node["customData"];

            var transition = iInt;
            if ((transition == (int)TransitionType.Interpolate || transition == (int)TransitionType.Extend)
                && lightColorBase.CustomData.HasKey(CustomKeyEasing))
            {
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
            var requiresCustomEasing = RequiresCustomEasing(lightColorBase.Easing);
            if (requiresCustomEasing || lightColorBase.CustomData.HasKey(CustomKeyEasing))
                lightColorBase.SetCustomData(lightColorBase.CustomData.Clone());

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
