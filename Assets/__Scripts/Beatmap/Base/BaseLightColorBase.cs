using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorBase : BaseGLSEvent
    {
        public BaseLightColorBase()
        {
        }

        protected BaseLightColorBase(
            float time,
            int color,
            float brightness,
            int easing,
            int usePrevious,
            int frequency,
            float strobeBrightness,
            int strobeFade,
            JSONNode customData = null) : base(time, customData)
        {
            Color = color;
            Brightness = brightness;
            Easing = easing;
            UsePrevious = usePrevious;
            Frequency = frequency;
            StrobeBrightness = strobeBrightness;
            StrobeFade = strobeFade;
        }

        protected BaseLightColorBase(BaseLightColorBase other) : base(other.JsonTime, other.CustomData)
        {
            Color = other.Color;
            Brightness = other.Brightness;
            Easing = other.Easing;
            UsePrevious = other.UsePrevious;
            Frequency = other.Frequency;
            StrobeBrightness = other.StrobeBrightness;
            StrobeFade = other.StrobeFade;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;
        public int Color { get; set; }
        public float Brightness { get; set; }
        public int UsePrevious { get; set; }
        public int Easing { get; set; } // new to V4
        public int Frequency { get; set; }
        public float StrobeBrightness { get; set; }
        public int StrobeFade { get; set; }

        public override string CustomKeyColor { get; } = "unusedColor";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightColorBase lcb)
                return Color == lcb.Color
                    || Math.Abs(Brightness - lcb.Brightness) < DecimalTolerance
                    || Easing == lcb.Easing
                    || UsePrevious == lcb.UsePrevious
                    || Frequency == lcb.Frequency
                    || Math.Abs(StrobeBrightness - lcb.StrobeBrightness) < DecimalTolerance
                    || StrobeFade == lcb.StrobeFade;
            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightColorBase.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightColorBase(this);
    }
}
