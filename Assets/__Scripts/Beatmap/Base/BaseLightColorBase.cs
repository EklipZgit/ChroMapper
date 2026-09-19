using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Shared;
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
            ColorDistributions = other.ColorDistributions.ToArray();
            StrobeColorDistributions = other.StrobeColorDistributions.ToArray();
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
        public int? ChromaColorEasing { get; set; }
        public int? ChromaStrobeColorEasing { get; set; }
        public int? ChromaStrobeEasing { get; set; }
        public string[] ColorDistributions { get; set; } = Array.Empty<string>();
        public string[] StrobeColorDistributions { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedStrobeColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();

        public virtual BasicEventColorLerpType CustomLerpType { get; set; }

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyTrack { get; } = "unusedKeyTrack";

        public virtual string CustomKeyLerpType => V3LightColorBase.CustomKeyEasingType;

        public string CustomKeyStrobeColor => "strobeColor";
        public string CustomKeyStrobeInterval => "strobeInterval";
        public string CustomKeyColorEasing => "colorEasing";
        public string CustomKeyStrobeColorEasing => "strobeColorEasing";
        public string CustomKeyStrobeEasing => "strobeEasing";
        public string CustomKeyColorDistributions => GLSColorDistribution.ColorDistributionsKey;
        public string CustomKeyStrobeColorDistributions => GLSColorDistribution.StrobeColorDistributionsKey;

        public override bool IsChroma() =>
            CustomData != null && (CustomData.HasKey(CustomKeyColor) || CustomData.HasKey(CustomKeyStrobeColor)
                || CustomData.HasKey(CustomKeyColorEasing) || CustomData.HasKey(CustomKeyStrobeColorEasing)
                || CustomData.HasKey(CustomKeyStrobeEasing) || CustomData.HasKey(CustomKeyLerpType)
                || CustomData.HasKey(CustomKeyColorDistributions) || CustomData.HasKey(CustomKeyStrobeColorDistributions));

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
            ColorDistributions = other.ColorDistributions.ToArray();
            StrobeColorDistributions = other.StrobeColorDistributions.ToArray();
            RefreshColorDistributionCaches();
        }

        protected override void ParseCustom()
        {
            // Temp just for a few cycles, delete after the beta testers have re-saved their maps.
            GLSColorDistribution.MigrateLegacyPropertyNames(CustomData);

            base.ParseCustom();
            CustomLerpType = Easing != (int)EaseType.None
                && (CustomData?.HasKey(CustomKeyLerpType) ?? false)
                && CustomData?[CustomKeyLerpType].Value == "HSV"
                ? BasicEventColorLerpType.TrueHSV
                : BasicEventColorLerpType.RGB;
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
            ChromaStrobeEasing = TryGetCustomEasingId(CustomKeyStrobeEasing, out var strobeEasing)
                    && strobeEasing != (int)EaseType.InOutCubic
                ? strobeEasing
                : null;
            ColorDistributions = GLSColorDistribution.ReadStrings(CustomData, CustomKeyColorDistributions);
            StrobeColorDistributions = GLSColorDistribution.ReadStrings(CustomData, CustomKeyStrobeColorDistributions);
            RefreshColorDistributionCaches();
        }

        private bool TryGetCustomEasingId(string key, out int easing)
        {
            easing = 0;
            var node = CustomData?[key];
            var valid = node != null
                && node.IsNumber
                && node.AsDouble == node.AsInt
                && (V3LightColorBase.RequiresCustomEasing(node.AsInt)
                    || ((key == CustomKeyStrobeEasing || key == CustomKeyStrobeColorEasing)
                        && node.AsInt == (int)EaseType.Linear));
            if (valid)
            {
                easing = node.AsInt;
            }

            return valid;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomLerpType == BasicEventColorLerpType.TrueHSV && Easing != (int)EaseType.None)
                node[CustomKeyLerpType] = "HSV";
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
            GLSColorDistribution.WriteStrings(node, CustomKeyColorDistributions, ColorDistributions);
            GLSColorDistribution.WriteStrings(node, CustomKeyStrobeColorDistributions, StrobeColorDistributions);
            RefreshColorDistributionCaches();
            return node;
        }

        // UI mutations update the raw arrays first, so refresh both parsed lists at the same ownership boundary as JSON writes.
        private void RefreshColorDistributionCaches()
        {
            ParsedColorDistributions = GLSColorDistribution.Parse(ColorDistributions);
            ParsedStrobeColorDistributions = GLSColorDistribution.Parse(StrobeColorDistributions);
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

    [Flags]
    public enum GLSColorDistributionTargets
    {
        None = 0,
        Hue = 1 << 0,
        Saturation = 1 << 1,
        Value = 1 << 2,
        Red = 1 << 3,
        Green = 1 << 4,
        Blue = 1 << 5,
        Brightness = 1 << 6
    }

    public sealed class GLSColorDistributionInstruction
    {
        public GLSColorDistributionInstruction(
            GLSColorDistributionTargets targets,
            float offset,
            Func<float, float> easing,
            bool usesAffectedLightProgress)
        {
            Targets = targets;
            Offset = offset;
            Easing = easing;
            UsesAffectedLightProgress = usesAffectedLightProgress;
        }

        public GLSColorDistributionTargets Targets { get; }
        public float Offset { get; }
        public Func<float, float> Easing { get; }
        public bool UsesAffectedLightProgress { get; }
    }

    public static class GLSColorDistribution
    {
        public const string ColorDistributionsKey = "colorDistributions";
        public const string StrobeColorDistributionsKey = "strobeColorDistributions";
        private const string LegacyColorDistributionsKey = "shifts";
        private const string LegacyStrobeColorDistributionsKey = "strobeShifts";

        private static readonly Dictionary<string, Func<float, float>> CompactEasings = new(StringComparer.Ordinal)
        {
            { "L", Easing.Linear },
            { "I^2", Easing.Quadratic.In },
            { "O^2", Easing.Quadratic.Out },
            { "IO^2", Easing.Quadratic.InOut },
            { "I^3", Easing.Cubic.In },
            { "O^3", Easing.Cubic.Out },
            { "IO^3", Easing.Cubic.InOut },
            { "I^4", Easing.Quartic.In },
            { "O^4", Easing.Quartic.Out },
            { "IO^4", Easing.Quartic.InOut },
            { "I^5", Easing.Quintic.In },
            { "O^5", Easing.Quintic.Out },
            { "IO^5", Easing.Quintic.InOut },
            { "ISn", Easing.Sinusoidal.In },
            { "OSn", Easing.Sinusoidal.Out },
            { "IOSn", Easing.Sinusoidal.InOut },
            { "IEx", Easing.Exponential.In },
            { "OEx", Easing.Exponential.Out },
            { "IOEx", Easing.Exponential.InOut },
            { "ICr", Easing.Circular.In },
            { "OCr", Easing.Circular.Out },
            { "IOCr", Easing.Circular.InOut },
            { "IBk", Easing.Back.In },
            { "OBk", Easing.Back.Out },
            { "IOTBk", Easing.Back.InOut },
            { "IEl", Easing.Elastic.In },
            { "OEl", Easing.Elastic.Out },
            { "IOTEl", Easing.Elastic.InOut },
            { "IBo", Easing.Bounce.In },
            { "OBo", Easing.Bounce.Out },
            { "IOTBo", Easing.Bounce.InOut },
            { "IOBk", Easing.Back.BeatSaberInOut },
            { "IOEl", Easing.Elastic.BeatSaberInOut },
            { "IOBo", Easing.Bounce.BeatSaberInOut },
            { "N", Easing.Step }
        };

        // TODO: Remove after a cycle, here for anyone who used the beta.
        private static readonly Dictionary<string, string> CanonicalCompactEasingNames = new(StringComparer.Ordinal)
        {
            { "lin", "L" }, { "iq", "I^2" }, { "oq", "O^2" }, { "ioq", "IO^2" },
            { "ic", "I^3" }, { "oc", "O^3" }, { "ioc", "IO^3" },
            { "iqt", "I^4" }, { "oqt", "O^4" }, { "ioqt", "IO^4" },
            { "iqn", "I^5" }, { "oqn", "O^5" }, { "ioqn", "IO^5" },
            { "is", "ISn" }, { "os", "OSn" }, { "ios", "IOSn" },
            { "ie", "IEx" }, { "oe", "OEx" }, { "ioe", "IOEx" },
            { "icr", "ICr" }, { "ocr", "OCr" }, { "iocr", "IOCr" },
            { "ib", "IBk" }, { "ob", "OBk" }, { "iob", "IOTBk" },
            { "iel", "IEl" }, { "oel", "OEl" }, { "ioel", "IOTEl" },
            { "ibo", "IBo" }, { "obo", "OBo" }, { "iobo", "IOTBo" }, { "step", "N" },
            { "L", "L" }, { "I^2", "I^2" }, { "O^2", "O^2" }, { "IO^2", "IO^2" },
            { "I^3", "I^3" }, { "O^3", "O^3" }, { "IO^3", "IO^3" },
            { "I^4", "I^4" }, { "O^4", "O^4" }, { "IO^4", "IO^4" },
            { "I^5", "I^5" }, { "O^5", "O^5" }, { "IO^5", "IO^5" },
            { "ISn", "ISn" }, { "OSn", "OSn" }, { "IOSn", "IOSn" },
            { "IEx", "IEx" }, { "OEx", "OEx" }, { "IOEx", "IOEx" },
            { "ICr", "ICr" }, { "OCr", "OCr" }, { "IOCr", "IOCr" },
            { "IBk", "IBk" }, { "OBk", "OBk" }, { "IOTBk", "IOTBk" },
            { "IEl", "IEl" }, { "OEl", "OEl" }, { "IOTEl", "IOTEl" },
            { "IBo", "IBo" }, { "OBo", "OBo" }, { "IOTBo", "IOTBo" },
            { "IOBk", "IOBk" }, { "IOEl", "IOEl" }, { "IOBo", "IOBo" }, { "N", "N" }
        };

        public static void MigrateLegacyPropertyNames(JSONNode customData)
        {
            MigrateLegacyPropertyName(customData, LegacyColorDistributionsKey, ColorDistributionsKey);
            MigrateLegacyPropertyName(customData, LegacyStrobeColorDistributionsKey, StrobeColorDistributionsKey);
        }

        private static void MigrateLegacyPropertyName(JSONNode customData, string legacyKey, string currentKey)
        {
            if (customData == null || !customData.HasKey(legacyKey))
            {
                return;
            }

            if (!customData.HasKey(currentKey))
            {
                customData[currentKey] = customData[legacyKey];
            }

            customData.Remove(legacyKey);
        }

        public static IReadOnlyList<GLSColorDistributionInstruction> Parse(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<GLSColorDistributionInstruction>();
            }

            var result = new List<GLSColorDistributionInstruction>(values.Count);
            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                var parts = values[valueIndex]?.Split(',');
                if (parts == null
                    || parts.Length < 3
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var offset)
                    || float.IsNaN(offset)
                    || float.IsInfinity(offset)
                    || !TryGetEasing(parts[2], out var easing))
                {
                    continue;
                }

                var targets = ParseTargets(parts[0]);
                if (targets != GLSColorDistributionTargets.None)
                {
                    var usesAffectedLightProgress = parts.Length >= 4
                        && string.Equals(parts[3].Trim(), "l", StringComparison.OrdinalIgnoreCase);
                    result.Add(new GLSColorDistributionInstruction(
                        targets,
                        offset,
                        easing,
                        usesAffectedLightProgress));
                }
            }

            return result;
        }

        public static string[] ReadStrings(JSONNode customData, string key)
        {
            if (customData == null || !customData.HasKey(key) || customData[key] is not JSONArray values)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(values.Count);
            foreach (var value in values)
            {
                if (value.Value is JSONString)
                {
                    result.Add(CanonicalizeCompactEasing(value.Value.Value));
                }
            }

            return result.ToArray();
        }

        private static string CanonicalizeCompactEasing(string value)
        {
            var parts = value?.Split(',');
            if (parts == null
                || parts.Length < 3)
            {
                return value;
            }

            var authoredName = parts[2].Trim();
            if (CanonicalCompactEasingNames.TryGetValue(authoredName, out var legacyCanonicalName))
            {
                parts[2] = legacyCanonicalName;
                return string.Join(",", parts);
            }

            return value;
        }

        public static void WriteStrings(JSONNode customData, string key, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                customData.Remove(key);
                return;
            }

            var array = new JSONArray();
            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                array.Add(values[valueIndex]);
            }

            customData[key] = array;
        }

        public static string[] FromEditorText(string value) =>
            (value ?? string.Empty)
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToArray();

        public static string ToEditorText(IReadOnlyList<string> values) => values == null
            ? string.Empty
            : string.Join("; ", values);

        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyNormal(color, box, evt, distributionProgress, distributionProgress);

        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress) =>
            Apply(color, box?.ParsedColorDistributions, evt.ParsedColorDistributions, affectedChunkProgress, affectedLightProgress);

        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyStrobe(mainColor, box, evt, distributionProgress, distributionProgress);

        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress) =>
            Apply(
                evt.StrobeColor ?? mainColor,
                box?.ParsedStrobeColorDistributions,
                evt.ParsedStrobeColorDistributions,
                affectedChunkProgress,
                affectedLightProgress);

        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorDistributionInstruction> boxInstructions,
            IReadOnlyList<GLSColorDistributionInstruction> eventInstructions,
            float distributionProgress) =>
            Apply(color, boxInstructions, eventInstructions, distributionProgress, distributionProgress);

        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorDistributionInstruction> boxInstructions,
            IReadOnlyList<GLSColorDistributionInstruction> eventInstructions,
            float affectedChunkProgress,
            float affectedLightProgress)
        {
            color = Apply(color, boxInstructions, affectedChunkProgress, affectedLightProgress);
            return Apply(color, eventInstructions, affectedChunkProgress, affectedLightProgress);
        }

        private static Color Apply(
            Color color,
            IReadOnlyList<GLSColorDistributionInstruction> instructions,
            float affectedChunkProgress,
            float affectedLightProgress)
        {
            if (instructions == null)
            {
                return color;
            }

            for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
            {
                var instruction = instructions[instructionIndex];
                var distributionProgress = instruction.UsesAffectedLightProgress
                    ? affectedLightProgress
                    : affectedChunkProgress;
                var offset = instruction.Offset * instruction.Easing(distributionProgress);
                var targets = instruction.Targets;
                if ((targets & (GLSColorDistributionTargets.Hue | GLSColorDistributionTargets.Saturation | GLSColorDistributionTargets.Value)) != 0)
                {
                    Color.RGBToHSV(color, out var hue, out var saturation, out var value);
                    // prevent invalid negative RGB by wrapping hue and clamping saturation while leaving HDR
                    if ((targets & GLSColorDistributionTargets.Hue) != 0)
                    {
                        hue = Mathf.Repeat(hue + offset, 1f);
                    }
                    if ((targets & GLSColorDistributionTargets.Saturation) != 0)
                    {
                        saturation = Mathf.Clamp01(saturation + offset);
                    }
                    if ((targets & GLSColorDistributionTargets.Value) != 0)
                    {
                        value += offset;
                    }

                    var alpha = color.a;
                    color = Color.HSVToRGB(hue, saturation, value, true);
                    color.a = alpha;
                }
                else
                {
                    if ((targets & GLSColorDistributionTargets.Red) != 0)
                    {
                        color.r += offset;
                    }
                    if ((targets & GLSColorDistributionTargets.Green) != 0)
                    {
                        color.g += offset;
                    }
                    if ((targets & GLSColorDistributionTargets.Blue) != 0)
                    {
                        color.b += offset;
                    }
                }

                if ((targets & GLSColorDistributionTargets.Brightness) != 0)
                {
                    color.a += offset;
                }
            }

            return color;
        }

        private static GLSColorDistributionTargets ParseTargets(string value)
        {
            var targets = GLSColorDistributionTargets.None;
            var model = ColorModel.None;
            foreach (var target in value ?? string.Empty)
            {
                switch (char.ToLowerInvariant(target))
                {
                    case 'h' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Hue;
                        break;
                    case 's' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Saturation;
                        break;
                    case 'v' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Value;
                        break;
                    case 'r' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Red;
                        break;
                    case 'g' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Green;
                        break;
                    case 'b' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Blue;
                        break;
                    case 'f':
                        targets |= GLSColorDistributionTargets.Brightness;
                        break;
                }
            }

            return targets;
        }

        private static bool TryGetEasing(string compactName, out Func<float, float> easing)
        {
            var name = compactName?.Trim();
            return (CanonicalCompactEasingNames.TryGetValue(name ?? string.Empty, out var canonicalName)
                    && CompactEasings.TryGetValue(canonicalName, out easing))
                || CompactEasings.TryGetValue(name ?? string.Empty, out easing)
                || Easing.ByName.TryGetValue(name ?? string.Empty, out easing);
        }

        private enum ColorModel
        {
            None,
            Hsv,
            Rgb
        }
    }
}
