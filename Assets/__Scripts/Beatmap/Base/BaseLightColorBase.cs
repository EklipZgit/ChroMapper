using System;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    /// <summary>
    /// GLS light color node base
    /// </summary>
    public class BaseLightColorBase : BaseGLSEvent
    {
        public BaseLightColorBase()
        {
        }

        // Used for Node Editor
        public BaseLightColorBase(JSONNode node) : this(V3LightColorBase.GetFromJson(node))
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

        protected BaseLightColorBase(BaseLightColorBase other) : base(other)
        {
            Color = other.Color;
            Brightness = other.Brightness;
            Easing = other.Easing;
            UsePrevious = other.UsePrevious;
            Frequency = other.Frequency;
            StrobeBrightness = other.StrobeBrightness;
            StrobeFade = other.StrobeFade;
            StrobeColor = other.StrobeColor;
            ChromaStrobeInterval = other.ChromaStrobeInterval;
        }

        public override ObjectType ObjectType { get; set; } = ObjectType.GLSEvent;
        public int Color { get; set; }
        public float Brightness { get; set; }
        public int UsePrevious { get; set; }
        public int Easing { get; set; } // new to V4
        public int Frequency { get; set; }
        public float StrobeBrightness { get; set; }
        public int StrobeFade { get; set; }
        public Color? StrobeColor { get; set; }
        public float? ChromaStrobeInterval { get; set; }

        // Currently not supported in ChromaGLS, TODO.
        public virtual string CustomLerpType { get; set; }

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public virtual string CustomKeyLerpType => V3BasicEvent.CustomKeyLerpType;

        public string CustomKeyStrobeColor => "strobeColor";
        public string CustomKeyStrobeInterval => "strobeInterval";

        public override bool IsChroma() =>
            CustomData != null && (CustomData.HasKey(CustomKeyColor) || CustomData.HasKey(CustomKeyStrobeColor));

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseLightColorBase other)
                return;

            Color = other.Color;
            Brightness = other.Brightness;
            UsePrevious = other.UsePrevious;
            Easing = other.Easing;
            Frequency = other.Frequency;
            StrobeBrightness = other.StrobeBrightness;
            StrobeFade = other.StrobeFade;
            StrobeColor = other.StrobeColor;
            ChromaStrobeInterval = other.ChromaStrobeInterval;
            CustomLerpType = other.CustomLerpType;
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            CustomLerpType = (CustomData?.HasKey(CustomKeyLerpType) ?? false)
                ? CustomData?[CustomKeyLerpType].Value
                : null;
            StrobeColor = (CustomData?.HasKey(CustomKeyStrobeColor) ?? false)
                ? CustomData?[CustomKeyStrobeColor].ReadColor()
                : null;
            ChromaStrobeInterval = (CustomData?.HasKey(CustomKeyStrobeInterval) ?? false)
                ? CustomData?[CustomKeyStrobeInterval].AsFloat
                : null;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomLerpType != null)
                node[CustomKeyLerpType] = CustomLerpType;
            else
                node.Remove(CustomKeyLerpType);
            if (StrobeColor != null)
                // Keep opaque strobe colors compact; Chroma treats an omitted alpha as fully opaque.
                node[CustomKeyStrobeColor] = new JSONArray().WriteColor(StrobeColor.Value, StrobeColor.Value.a != 1f);
            else
                node.Remove(CustomKeyStrobeColor);
            if (ChromaStrobeInterval.HasValue)
                node[CustomKeyStrobeInterval] = ChromaStrobeInterval.Value;
            else
                node.Remove(CustomKeyStrobeInterval);
            return node;
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightColorBase lcb) return BoxIndex == lcb.BoxIndex;
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
