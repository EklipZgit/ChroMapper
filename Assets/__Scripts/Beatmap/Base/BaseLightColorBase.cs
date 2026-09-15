using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
            // V3ColorBoxRoundTripPreservesShiftPayloadAndUnknownCustomData requires cloned nodes to retain ordered event-scope distributions independently of their source arrays.
            Shifts = other.Shifts.ToArray();
            StrobeShifts = other.StrobeShifts.ToArray();
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
        // ShiftedColorNodeInfoExposesNormalAndStrobeDistributionMarkers reads these authored strings while playback consumes the parsed cache populated by ParseCustom.
        public string[] Shifts { get; set; } = Array.Empty<string>();
        public string[] StrobeShifts { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GLSColorShiftInstruction> ParsedShifts { get; private set; } =
            Array.Empty<GLSColorShiftInstruction>();
        public IReadOnlyList<GLSColorShiftInstruction> ParsedStrobeShifts { get; private set; } =
            Array.Empty<GLSColorShiftInstruction>();

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
        public string CustomKeyShifts => GLSColorShift.ShiftsKey;
        public string CustomKeyStrobeShifts => GLSColorShift.StrobeShiftsKey;

        // V3ColorNodeTrackEasingsMarkIsChroma: per-track easing keys change rendered output exactly like colors do.
        public override bool IsChroma() =>
            CustomData != null && (CustomData.HasKey(CustomKeyColor) || CustomData.HasKey(CustomKeyStrobeColor)
                || CustomData.HasKey(CustomKeyColorEasing) || CustomData.HasKey(CustomKeyStrobeColorEasing)
                || CustomData.HasKey(CustomKeyStrobeEasing) || CustomData.HasKey(CustomKeyLerpType)
                || CustomData.HasKey(CustomKeyShifts) || CustomData.HasKey(CustomKeyStrobeShifts));

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
            // Event replacement must carry both raw authoring strings and their fresh parsed caches into the cloned group.
            Shifts = other.Shifts.ToArray();
            StrobeShifts = other.StrobeShifts.ToArray();
            RefreshShiftCaches();
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
            // Parse once when custom data changes so the per-light preview path never reparses compact strings.
            Shifts = GLSColorShift.ReadStrings(CustomData, CustomKeyShifts);
            StrobeShifts = GLSColorShift.ReadStrings(CustomData, CustomKeyStrobeShifts);
            RefreshShiftCaches();
        }

        // Track easing keys accept authored custom curves; colorEasing keeps Linear native while the two
        // optional tracks retain Linear=0 because their absent states have different fallback curves.
        private bool TryGetCustomEasingId(string key, out int easing)
        {
            easing = 0;
            var node = CustomData?[key];
            var valid = node != null
                && node.IsNumber
                && node.AsDouble == node.AsInt
                && (V3LightColorBase.RequiresCustomEasing(node.AsInt)
                    // GlsEasingCycleMatchesEditorOrder needs optional-track Linear=0 overrides: absent
                    // strobeEasing means native InOutCubic, while absent strobeColorEasing follows the interval.
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
            // Rewriting only the owned arrays leaves every unrelated customData key untouched.
            GLSColorShift.WriteStrings(node, CustomKeyShifts, Shifts);
            GLSColorShift.WriteStrings(node, CustomKeyStrobeShifts, StrobeShifts);
            RefreshShiftCaches();
            return node;
        }

        // UI mutations update the raw arrays first, so refresh both parsed lists at the same ownership boundary as JSON writes.
        private void RefreshShiftCaches()
        {
            ParsedShifts = GLSColorShift.Parse(Shifts);
            ParsedStrobeShifts = GLSColorShift.Parse(StrobeShifts);
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

    // FirstColorModelWinsWhileFRemainsIndependent represents parsed channels without reparsing strings in GLS playback.
    [Flags]
    public enum GLSColorShiftTargets
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

    // OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields keeps each instruction's coordinate mode paired with its easing instead of imposing one mode on the whole list.
    public sealed class GLSColorShiftInstruction
    {
        public GLSColorShiftInstruction(
            GLSColorShiftTargets targets,
            float offset,
            Func<float, float> easing,
            bool usesAffectedLightProgress)
        {
            Targets = targets;
            Offset = offset;
            Easing = easing;
            UsesAffectedLightProgress = usesAffectedLightProgress;
        }

        public GLSColorShiftTargets Targets { get; }
        public float Offset { get; }
        public Func<float, float> Easing { get; }
        public bool UsesAffectedLightProgress { get; }
    }

    // The shared codec and evaluator keep JSON, editor, node appearance, and runtime behavior on one interpretation of compact shifts.
    public static class GLSColorShift
    {
        public const string ShiftsKey = "shifts";
        public const string StrobeShiftsKey = "strobeShifts";

        private static readonly Dictionary<string, Func<float, float>> CompactEasings = new(
            StringComparer.OrdinalIgnoreCase)
        {
            { "lin", Easing.Linear },
            { "iq", Easing.Quadratic.In },
            { "oq", Easing.Quadratic.Out },
            { "ioq", Easing.Quadratic.InOut },
            { "ic", Easing.Cubic.In },
            { "oc", Easing.Cubic.Out },
            { "ioc", Easing.Cubic.InOut },
            { "iqt", Easing.Quartic.In },
            { "oqt", Easing.Quartic.Out },
            { "ioqt", Easing.Quartic.InOut },
            { "iqn", Easing.Quintic.In },
            { "oqn", Easing.Quintic.Out },
            { "ioqn", Easing.Quintic.InOut },
            { "is", Easing.Sinusoidal.In },
            { "os", Easing.Sinusoidal.Out },
            { "ios", Easing.Sinusoidal.InOut },
            { "ie", Easing.Exponential.In },
            { "oe", Easing.Exponential.Out },
            { "ioe", Easing.Exponential.InOut },
            { "icr", Easing.Circular.In },
            { "ocr", Easing.Circular.Out },
            { "iocr", Easing.Circular.InOut },
            { "ib", Easing.Back.In },
            { "ob", Easing.Back.Out },
            { "iob", Easing.Back.InOut },
            { "iel", Easing.Elastic.In },
            { "oel", Easing.Elastic.Out },
            { "ioel", Easing.Elastic.InOut },
            { "ibo", Easing.Bounce.In },
            { "obo", Easing.Bounce.Out },
            { "iobo", Easing.Bounce.InOut },
            { "step", Easing.Step }
        };

        // OptionalLightProgressFieldAcceptsForwardCompatibleTokensAndTrailingFields reads only the first four slots, treating exactly l as per-light mode while preserving required-field validation and forward-compatible targets.
        public static IReadOnlyList<GLSColorShiftInstruction> Parse(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<GLSColorShiftInstruction>();
            }

            var result = new List<GLSColorShiftInstruction>(values.Count);
            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                // OptionalProgressModesRemainTolerantWithoutChangingRequiredFields accepts extra slots but still rejects missing or malformed offset/easing.
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
                if (targets != GLSColorShiftTargets.None)
                {
                    // OptionalProgressModesRemainTolerantWithoutChangingRequiredFields ignores unknown fourth values and all later fields rather than invalidating recognized targets.
                    var usesAffectedLightProgress = parts.Length >= 4
                        && string.Equals(parts[3].Trim(), "l", StringComparison.OrdinalIgnoreCase);
                    result.Add(new GLSColorShiftInstruction(
                        targets,
                        offset,
                        easing,
                        usesAffectedLightProgress));
                }
            }

            return result;
        }

        // JSON reads keep recognized strings in their authored order; the containing customData object remains authoritative for unknown keys.
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
                    result.Add(value.Value.Value);
                }
            }

            return result.ToArray();
        }

        // JSON writes replace only the requested extension field and remove empty arrays without rebuilding the surrounding customData object.
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

        // Semicolon or newline separation lets compact event and box controls edit ordered JSON arrays without making commas ambiguous.
        public static string[] FromEditorText(string value) =>
            (value ?? string.Empty)
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToArray();

        public static string ToEditorText(IReadOnlyList<string> values) => values == null
            ? string.Empty
            : string.Join("; ", values);

        // Existing callers provide one affected-chunk coordinate, so this compatibility overload preserves all three-field shift behavior.
        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyNormal(color, box, evt, distributionProgress, distributionProgress);

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases supplies both dense coordinates so each box and event instruction can select its authored mode independently.
        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress) =>
            Apply(color, box?.ParsedShifts, evt.ParsedShifts, affectedChunkProgress, affectedLightProgress);

        // Existing callers provide one affected-chunk coordinate, so omitted and unknown fourth-slot values keep their prior strobe behavior.
        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyStrobe(mainColor, box, evt, distributionProgress, distributionProgress);

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases gives normal and strobe lists the same instruction-specific choice without leaking either list into the other phase.
        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress) =>
            Apply(
                evt.StrobeColor ?? mainColor,
                box?.ParsedStrobeShifts,
                evt.ParsedStrobeShifts,
                affectedChunkProgress,
                affectedLightProgress);

        // Existing direct evaluator callers retain one-coordinate semantics while new runtime paths can provide an additional affected-light coordinate.
        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorShiftInstruction> boxInstructions,
            IReadOnlyList<GLSColorShiftInstruction> eventInstructions,
            float distributionProgress) =>
            Apply(color, boxInstructions, eventInstructions, distributionProgress, distributionProgress);

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases preserves cumulative order, HSV/HDR/f rules, and independent easings while selecting progress per instruction.
        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorShiftInstruction> boxInstructions,
            IReadOnlyList<GLSColorShiftInstruction> eventInstructions,
            float affectedChunkProgress,
            float affectedLightProgress)
        {
            color = Apply(color, boxInstructions, affectedChunkProgress, affectedLightProgress);
            return Apply(color, eventInstructions, affectedChunkProgress, affectedLightProgress);
        }

        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases switches only the spatial input before the existing channel evaluator, leaving hue wrap, saturation clamp, HDR, and f unchanged.
        private static Color Apply(
            Color color,
            IReadOnlyList<GLSColorShiftInstruction> instructions,
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
                // PerLightShiftsPreserveHsvHdrAndIndependentF changes only each instruction's easing input, leaving the channel operations and cumulative ordering below intact.
                var distributionProgress = instruction.UsesAffectedLightProgress
                    ? affectedLightProgress
                    : affectedChunkProgress;
                var offset = instruction.Offset * instruction.Easing(distributionProgress);
                var targets = instruction.Targets;
                if ((targets & (GLSColorShiftTargets.Hue | GLSColorShiftTargets.Saturation | GLSColorShiftTargets.Value)) != 0)
                {
                    Color.RGBToHSV(color, out var hue, out var saturation, out var value);
                    // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases prevents invalid negative RGB by wrapping hue and clamping saturation while leaving HDR value unbounded above.
                    if ((targets & GLSColorShiftTargets.Hue) != 0)
                    {
                        hue = Mathf.Repeat(hue + offset, 1f);
                    }
                    if ((targets & GLSColorShiftTargets.Saturation) != 0)
                    {
                        saturation = Mathf.Clamp01(saturation + offset);
                    }
                    if ((targets & GLSColorShiftTargets.Value) != 0)
                    {
                        value += offset;
                    }

                    var alpha = color.a;
                    color = Color.HSVToRGB(hue, saturation, value, true);
                    color.a = alpha;
                }
                else
                {
                    if ((targets & GLSColorShiftTargets.Red) != 0)
                    {
                        color.r += offset;
                    }
                    if ((targets & GLSColorShiftTargets.Green) != 0)
                    {
                        color.g += offset;
                    }
                    if ((targets & GLSColorShiftTargets.Blue) != 0)
                    {
                        color.b += offset;
                    }
                }

                if ((targets & GLSColorShiftTargets.Brightness) != 0)
                {
                    color.a += offset;
                }
            }

            return color;
        }

        private static GLSColorShiftTargets ParseTargets(string value)
        {
            var targets = GLSColorShiftTargets.None;
            var model = ColorModel.None;
            foreach (var target in value ?? string.Empty)
            {
                switch (char.ToLowerInvariant(target))
                {
                    case 'h' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorShiftTargets.Hue;
                        break;
                    case 's' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorShiftTargets.Saturation;
                        break;
                    case 'v' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorShiftTargets.Value;
                        break;
                    case 'r' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorShiftTargets.Red;
                        break;
                    case 'g' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorShiftTargets.Green;
                        break;
                    case 'b' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorShiftTargets.Blue;
                        break;
                    case 'f':
                        targets |= GLSColorShiftTargets.Brightness;
                        break;
                }
            }

            return targets;
        }

        private static bool TryGetEasing(string compactName, out Func<float, float> easing)
        {
            var name = compactName?.Trim();
            return CompactEasings.TryGetValue(name ?? string.Empty, out easing)
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
