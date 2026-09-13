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
            ChromaColorEasing = other.ChromaColorEasing;
            ChromaStrobeColorEasing = other.ChromaStrobeColorEasing;
            ChromaStrobeEasing = other.ChromaStrobeEasing;
            CustomLerpType = other.CustomLerpType;
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
        // GLSColorEasingInputTest: three independent ChromaGLS easing tracks ride customData on light color nodes.
        public int? ChromaColorEasing { get; set; }
        public int? ChromaStrobeColorEasing { get; set; }
        public int? ChromaStrobeEasing { get; set; }

        // GLSEasingTypeRibbonInputTest: GLS easingType admits only authored "HSV" (true angular hue lerp);
        // RGB is the absent default, unlike the Basic Event lerpType key this field shares storage with.
        public virtual string CustomLerpType { get; set; }

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public virtual string CustomKeyLerpType => V3LightColorBase.CustomKeyEasingType;

        public string CustomKeyStrobeColor => "strobeColor";
        public string CustomKeyStrobeInterval => "strobeInterval";
        // GLSColorEasingInputTest: the per-track easing keys stay distinct from the V3 interval "easing" key.
        public string CustomKeyColorEasing => "colorEasing";
        public string CustomKeyStrobeColorEasing => "strobeColorEasing";
        public string CustomKeyStrobeEasing => "strobeEasing";

        // V3ColorNodeTrackEasingsMarkIsChroma: per-track easing keys change rendered output exactly like colors do.
        public override bool IsChroma() =>
            CustomData != null && (CustomData.HasKey(CustomKeyColor) || CustomData.HasKey(CustomKeyStrobeColor)
                || CustomData.HasKey(CustomKeyColorEasing) || CustomData.HasKey(CustomKeyStrobeColorEasing)
                || CustomData.HasKey(CustomKeyStrobeEasing) || CustomData.HasKey(CustomKeyLerpType));

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
            ChromaColorEasing = other.ChromaColorEasing;
            ChromaStrobeColorEasing = other.ChromaStrobeColorEasing;
            ChromaStrobeEasing = other.ChromaStrobeEasing;
            CustomLerpType = other.CustomLerpType;
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            // V3ColorNodeNormalizesRgbAndUnknownEasingType: only authored "HSV" survives; RGB and unknown
            // strings are the absent RGB default, and Instant endpoints own no interval for it to drive.
            CustomLerpType = Easing != (int)EaseType.None
                && (CustomData?.HasKey(CustomKeyLerpType) ?? false)
                && CustomData?[CustomKeyLerpType].Value == "HSV"
                ? "HSV"
                : null;
            StrobeColor = (CustomData?.HasKey(CustomKeyStrobeColor) ?? false)
                ? CustomData?[CustomKeyStrobeColor].ReadColor()
                : null;
            ChromaStrobeInterval = (CustomData?.HasKey(CustomKeyStrobeInterval) ?? false)
                ? CustomData?[CustomKeyStrobeInterval].AsFloat
                : null;
            // V3ColorNodeInstantStripsColorEasing: an Instant node has no interval for colorEasing to drive.
            ChromaColorEasing = Easing != (int)EaseType.None
                && TryGetCustomEasingId(CustomKeyColorEasing, out var colorEasing)
                ? colorEasing
                : null;
            ChromaStrobeColorEasing = TryGetCustomEasingId(CustomKeyStrobeColorEasing, out var strobeColorEasing)
                ? strobeColorEasing
                : null;
            // V3ColorNodeStrobeEasingExcludesNativeDefault: InOutCubic is the game's authored fade curve already.
            ChromaStrobeEasing = TryGetCustomEasingId(CustomKeyStrobeEasing, out var strobeEasing)
                    && strobeEasing != (int)EaseType.InOutCubic
                ? strobeEasing
                : null;
        }

        // Track easing keys accept only authored custom curves; None/Linear and invalid IDs stay native metadata-free.
        private bool TryGetCustomEasingId(string key, out int easing)
        {
            easing = 0;
            var node = CustomData?[key];
            var valid = node != null
                && node.IsNumber
                && node.AsDouble == node.AsInt
                && V3LightColorBase.RequiresCustomEasing(node.AsInt);
            if (valid)
            {
                easing = node.AsInt;
            }

            return valid;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            // GLSEasingTypeRibbonInputTest: easingType serializes only while a transition interval owns it,
            // matching ChromaGLS normalization for Instant endpoints.
            if (CustomLerpType != null && Easing != (int)EaseType.None)
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
            // GLSColorEasingInputTest: OEM states serialize no key so the native curve takes over.
            if (ChromaColorEasing.HasValue && Easing != (int)EaseType.None)
                node[CustomKeyColorEasing] = ChromaColorEasing.Value;
            else
                node.Remove(CustomKeyColorEasing);
            if (ChromaStrobeColorEasing.HasValue)
                node[CustomKeyStrobeColorEasing] = ChromaStrobeColorEasing.Value;
            else
                node.Remove(CustomKeyStrobeColorEasing);
            if (ChromaStrobeEasing.HasValue && StrobeFade == 1)
                node[CustomKeyStrobeEasing] = ChromaStrobeEasing.Value;
            else
                node.Remove(CustomKeyStrobeEasing);
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
