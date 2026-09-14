using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventCommon
{
    // TransformNodeLayoutMatchesOe uses one node-relative delta for the requested inward columns and downward rotation-icon movement.
    public const float RotationLayoutAdjustment = 1f / 30f;
    public const float RotationColumnHorizontalOffset = 0.25f - RotationLayoutAdjustment;
    // TransformNodeLayoutMatchesOe makes the easing icon row structurally shared by Rotation, Translation, and FloatFX.
    public const float TransformEasingIconHeight = 0.29f - RotationLayoutAdjustment;
    // TransformNodeLayoutMatchesOe keeps the direction icon's prior top edge fixed while its 30% growth extends left, right, and down.
    public const float RotationDirectionIconHeight =
        (0.273333f - RotationLayoutAdjustment) -
        ((Beatmap.Containers.GLSEventIconView.RotationDirectionIconSize -
          Beatmap.Containers.GLSEventIconView.PreviousRotationDirectionIconSize) * 0.5f);
    // TransformValueBaselinesMatchAcrossNodeTypes offsets TMP's enlarged multiline recentering without changing any bottom-value baseline.
    public const float TransformTextVerticalOffset = -0.073232f;

    private const float TransformTextFaceWidth = 1.2f;
    private const float TransformTextColumnWidth = 0.3f;
    // TransformValueBaselinesMatchAcrossNodeTypes keeps all three transform node types on the exact same TMP row constants.
    // ScaledRotationLabelsPreserveRequestedRows grows both small labels 20% while coordinated offsets preserve easing and lower loop with its icon.
    private const string TransformLabelSizeTag = "<size=58.8%>";
    private const string TransformLabelOffsetTag = "<voffset=0.585em>";
    private const string TransformLoopOffsetTag = "<voffset=0.635em>";
    // TransformValueBaselinesMatchAcrossNodeTypes coordinates with the shared mesh correction to preserve every bottom value.
    private const string TransformValueTag = "<margin=0%><size=100%><voffset=-0.285em><align=center>";

    // ColorNodeLayoutUsesRequestedVerticalOffsets restores thirty percent of the shared drop, then keeps each icon's requested adjustment relative to that baseline.
    public const float ColorFaceVerticalOffset = -7f / 120f;
    public const float ColorEasingIconHeight = 0.20f + ColorFaceVerticalOffset - (1f / 10f);
    public const float ColorStrobeIconHeight = -0.10f + ColorFaceVerticalOffset + (1f / 15f) - (1f / 25f);
    public const float ColorTertiaryIconHeight = -0.17f + ColorFaceVerticalOffset + (1f / 15f);
    // Text columns share the icon column center so labels sit directly under their markers.
    private const float ColorTextColumnCenter =
        Beatmap.Containers.GLSEventIconView.StateIconHorizontalPosition;
    // ColorNodeTwoColumnLayout: TMP resolves margin percentages against the font size, so columns need em
    // margins; one em measures 0.36 node units on the prefab's 12pt/0.3-scale face (calibration-measured).
    private const float ColorTextEmScale = 0.36f;
    // Six rows at 60% line height stack the columns at icon-adjacent bands; small voffsets nudge each row
    // under its icon because TMP grows line boxes around offset glyphs rather than shifting the stack.
    private const string ColorLineHeightTag = "<line-height=55%>";
    private const string ColorBrightnessOffsetTag = "<voffset=0.2em>";
    // ColorNodeLayoutUsesRequestedVerticalOffsets restores thirty percent of each prior row-specific drop through the calibrated 0.36-node em scale.
    private const string ColorEasingAbbrevOffsetTag = "<voffset=-0.288889em>";
    private const string ColorStrobeValueOffsetTag = "<voffset=0.355556em>";
    private const string ColorStrobeRateOffsetTag = "<voffset=0.1em>";
    private const string ColorStrobeAbbrevOffsetTag = "<voffset=0.4em>";
    // ColorNodeTwoColumnLayout keeps easing labels smaller than the transform labels sharing the same face technique.
    private const string ColorLabelSizeTag = "<size=52%>";
    private const string ColorStrobeSizeTag = "<size=66%>";
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;
    // Partition color-transition timelines by GLS group ID so unrelated light groups never share cache work.
    private static readonly Dictionary<int, ColorTransitionGroupCache> colorTransitionCaches = new();
    // Index only sources with active transition intervals so viewport retention never scans all color sequences.
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorTransitionIntervals = new();
    // Reuse the cross-group query buffer because color-pool refresh runs every viewport update.
    private static readonly List<BaseLightColorBase> colorTransitionQueryResults = new();
    // Reuse the event ordering comparer while inserting edited nodes into their cached timelines.
    private static readonly Comparer<BaseLightColorBase> colorEventComparer =
        Comparer<BaseLightColorBase>.Create(CompareColorEvents);
    private static BaseDifficulty cachedColorTransitionMap;
    // Collider wave routing needs per-light intervals, including incoming cross-group spans in the inner editor.
    private static readonly Dictionary<int, int> colorLightCounts = new();
    private static readonly HashSet<int> dirtyColorTimelines = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorOutgoingIntervals = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorInnerIntervals = new();

    // GLS edits replace groups and child objects with clones, so diagnostics must expose reference identity as well as serialized values.
    public static string DescribeEvent(BaseGLSEvent evt)
    {
        if (evt == null)
        {
            return "null";
        }

        var color = evt is BaseLightColorBase colorEvent
            ? $", color={colorEvent.Color}, custom={FormatColor(colorEvent.CustomColor)}, usePrevious={colorEvent.UsePrevious}, easing={colorEvent.Easing}"
            : string.Empty;
        return $"{evt.GetType().Name}@{ReferenceId(evt)} abs={evt.JsonTime:R} rel={evt.RelativeJsonTime:R} " +
               $"box={evt.BoxIndex}/@{ReferenceId(evt.EventBoxData)} group={DescribeGroup(evt.EventBoxGroupData)}{color}";
    }

    // Group identity is logged separately because equal serialized GLS groups are routinely replaced by new instances.
    public static string DescribeGroup(BaseEventBoxGroup group) => group == null
        ? "null"
        : $"{group.GetType().Name}@{ReferenceId(group)} id={group.ID} beat={group.JsonTime:R} boxes={group.ReadOnlyBoxes.Count}";

    // Reference IDs remain null-safe while exposing identity independently of mutable beatmap equality.
    public static int ReferenceId(object value) => value == null
        ? 0
        : RuntimeHelpers.GetHashCode(value);

    // Nullable Chroma values need an invariant compact representation so cloned-event logs can be compared directly.
    public static string FormatColor(Color? color) => color.HasValue
        ? $"({color.Value.r:R},{color.Value.g:R},{color.Value.b:R},{color.Value.a:R})"
        : "null";

    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve enters the common display path with the same alpha-composed color sent to light renderers.
        return GetNodePreviewColor(GetLightColor(evt, boost, eventAppearance), eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve composes sb before the common display path while retaining the main-color fallback.
        return GetNodePreviewColor(GetLightStrobeColor(evt, boost, eventAppearance), eventAppearance);
    }

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips interpolates renderer-level endpoints so each ribbon strip represents the light color instead of the node-preview display curve.
    public static Color GetLightColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance) =>
        BasicEventColorLerp.ApplyBrightness(
            GetBaseColor(evt, boost, eventAppearance),
            evt.Brightness);

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips keeps omitted strobe colors on the same main-color fallback used by playback.
    public static Color GetLightStrobeColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance)
    {
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return BasicEventColorLerp.ApplyBrightness(color, evt.StrobeBrightness);
    }

    // ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights computes filter, distribution, and shift results once per appearance refresh instead of per rendered pixel.
    public static bool PopulateColorDistributionPreview(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        Color[] mainColors,
        Color[] strobeColors,
        float[] perLightDepthTable) =>
        PopulateColorDistribution(
            evt,
            lightCount,
            boost,
            eventAppearance,
            mainColors,
            strobeColors,
            perLightDepthTable,
            false);

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips needs endpoint colors even when only the opposite endpoint enables distribution rendering.
    public static bool PopulateColorTransitionEndpoint(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        Color[] mainColors,
        Color[] strobeColors) =>
        PopulateColorDistribution(
            evt,
            lightCount,
            boost,
            eventAppearance,
            mainColors,
            strobeColors,
            null,
            true);

    // LightIdTransitionRibbonKeepsSparseLanesBlackWithoutShifts also enables the endpoint table when a light-ID filter selects only part of the physical group.
    public static bool HasColorTransitionDistribution(BaseLightColorBase evt, int lightCount)
    {
        if (lightCount <= 0 || evt?.EventBoxData is not BaseLightColorEventBox box)
        {
            return false;
        }

        if (HasColorShiftOrBrightnessDistribution(box, evt))
        {
            return true;
        }

        var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, lightCount);
        return indexFilter != null && indexFilter.AffectedLightCount < lightCount;
    }

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips shares the playback color inputs while allowing a plain endpoint to fill its selected lights for a shifted counterpart.
    private static bool PopulateColorDistribution(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        Color[] mainColors,
        Color[] strobeColors,
        float[] perLightDepthTable,
        bool populateWhenDisabled)
    {
        if (lightCount <= 0
            || mainColors == null
            || strobeColors == null
            || (perLightDepthTable != null && perLightDepthTable.Length < lightCount)
            || mainColors.Length < lightCount
            || strobeColors.Length < lightCount
            || evt.EventBoxData is not BaseLightColorEventBox box)
        {
            return false;
        }

        // ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights initializes every physical light as black before the authored filter fills only controlled IDs.
        for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
        {
            mainColors[lightIndex] = Color.black;
            strobeColors[lightIndex] = Color.black;
            if (perLightDepthTable != null)
            {
                // FrontToBackPreviewMapsMostShiftedLightFirst stores each reversed physical light at its texture-center depth while skipped IDs remain black.
                perLightDepthTable[lightIndex] = (lightCount - lightIndex - 0.5f) / lightCount;
            }
        }

        // BrightnessDistributionEnablesPreviewWithoutColorShifts shows ordinary distribution and chunk/filter patterns as well as Chroma color shifts.
        var previewEnabled = HasColorDistributionPreview(box, evt);
        if (!previewEnabled && !populateWhenDisabled)
        {
            return false;
        }

        var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, lightCount);
        if (indexFilter == null)
        {
            return previewEnabled;
        }

        var baseColor = GetBaseColor(evt, boost, eventAppearance);
        var distributionCount = DistributionHelper.GetDistributionCount(indexFilter);
        var affectsFirst = box.BrightnessAffectFirst == 1
            || box.Events.Length == 0
            || !ReferenceEquals(box.Events[0], evt);
        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases caches both denominators per refresh; partial and skipped chunks must not consume per-light progress.
        var chunkProgressDenominator = (float)Mathf.Max(indexFilter.VisibleCount - 1, 1);
        var lightProgressDenominator = (float)Mathf.Max(indexFilter.AffectedLightCount - 1, 1);
        foreach (var entry in indexFilter)
        {
            // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases mirrors the two coordinates cached in playback states, rather than substituting a physical light ID.
            var distributionProgress = entry.AffectedChunkOrder / chunkProgressDenominator;
            var affectedLightProgress = entry.AffectedLightOrder / lightProgressDenominator;
            var brightnessOffset = affectsFirst
                ? DistributionHelper.GetValueStep(
                    entry.DistributionOrder,
                    distributionCount,
                    (DistributionType)box.BrightnessDistributionType,
                    box.BrightnessDistribution,
                    (EaseType)box.Easing)
                : 0f;
            // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases lets every box/event normal/strobe instruction choose its own progress without altering renderer brightness or the node display curve.
            var shiftedColor = GLSColorShift.ApplyNormal(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            var shiftedStrobeColor = GLSColorShift.ApplyStrobe(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            // ReportedHsvShiftPreviewMatchesLightRendererAtBothStrobePhases mirrors LightColorTween's renderer input: shifts own RGB and light level multiplies alpha only.
            shiftedColor.a *= evt.Brightness + brightnessOffset;
            shiftedStrobeColor.a *= evt.StrobeBrightness;
            mainColors[entry.Element] = shiftedColor;
            strobeColors[entry.Element] = shiftedStrobeColor;
        }

        return previewEnabled;
    }

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips centralizes the cheap feature check so ordinary GLS ribbons keep the two-color shader path.
    private static bool HasColorDistributionPreview(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) =>
        box.IndexFilter.Chunks != 0 || HasColorShiftOrBrightnessDistribution(box, evt);

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips keeps transition refreshes cheap unless authored distribution can actually change at least one endpoint.
    private static bool HasColorShiftOrBrightnessDistribution(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) =>
        !Mathf.Approximately(box.BrightnessDistribution, 0f)
        || box.ParsedShifts.Count > 0
        || box.ParsedStrobeShifts.Count > 0
        || evt.ParsedShifts.Count > 0
        || evt.ParsedStrobeShifts.Count > 0;

    // Keep the strobe-band predicate tied to actual strobe timing, not its independently configurable dark brightness.
    public static bool IsStrobing(BaseLightColorBase evt)
        => evt.Frequency > 0 
            || (evt.ChromaStrobeInterval is { } interval && interval > 0f);

    // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint shares playback's cycles-per-beat conversion so ribbon phase cannot drift from the rendered light.
    public static float GetStrobeFrequency(BaseLightColorBase evt)
    {
        if (evt.Brightness <= 0f && evt.StrobeBrightness <= 0f)
            return 0f;

        return evt.ChromaStrobeInterval is { } interval && interval > 0f
            ? 1f / interval
            : evt.Frequency;
    }

    // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve preserves HDR hue while folding RGB value and renderer alpha into the one existing GLS node brightness curve.
    public static Color GetNodePreviewColor(Color rendererColor, EventAppearanceSO eventAppearance)
    {
        var maximumChannel = Mathf.Max(rendererColor.r, Mathf.Max(rendererColor.g, rendererColor.b));
        var hdrIntensity = Mathf.Max(maximumChannel, 1f);
        if (maximumChannel > 1f)
        {
            rendererColor.r /= maximumChannel;
            rendererColor.g /= maximumChannel;
            rendererColor.b /= maximumChannel;
        }

        var effectiveBrightness = rendererColor.a * hdrIntensity;
        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve avoids applying renderer alpha twice while retaining the established opaque node-color endpoint.
        rendererColor.a = 1f;
        return ApplyBrightness(rendererColor, effectiveBrightness, eventAppearance);
    }

    // Keep the existing GLS dimness curve shared by ordinary nodes and both per-light distribution sections.
    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

    // Timeline preparation shares the same default/custom color selection as node appearance.
    internal static Color GetBaseColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
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

    // ColorNodeTwoColumnLayout replaces the centered easing line and strobe-line fade marker with a left
    // easing column (transition abbrev under its top-left icon, strobeColorEasing under the bottom-left icon)
    // and a right strobe column (brightness, an icon gap row, then the rate in its existing formats).
    public static string GetColorInfo(BaseLightColorBase evt)
    {
        var sb = new StringBuilder(192);
        sb.Append(ColorLineHeightTag);
        sb.Append(ColorBrightnessOffsetTag);
        sb.Append("<align=center>");
        sb.Append((evt.Brightness * 100f).ToString(CultureInfo.InvariantCulture));
        sb.Append("</voffset>");
        sb.AppendLine();

        // The former centered "L" becomes the small abbrev under the top-left icon and follows the effective
        // colorEasing curve, so it changes to the authored curve name whenever customData overrides it.
        sb.Append(ColorEasingAbbrevOffsetTag);
        AppendEmColumn(sb, -ColorTextColumnCenter);
        sb.Append(ColorLabelSizeTag);
        sb.Append(Easing.IDToShortName.GetValueOrDefault(evt.ChromaColorEasing ?? evt.Easing));
        sb.Append("</size></voffset>");
        sb.AppendLine();

        // The right column stacks strobe brightness, the strobeEasing icon gap, then the strobe rate.
        sb.Append(ColorStrobeValueOffsetTag);
        AppendEmColumn(sb, ColorTextColumnCenter);
        sb.Append(ColorStrobeSizeTag);
        if (evt.StrobeBrightness > 0f)
        {
            sb.Append((evt.StrobeBrightness * 100f).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            // ColorInfoKeepsRowsFixedWithoutOptionalValues gives centered TMP the same line metrics whether strobe brightness is visible or absent.
            sb.Append(' ');
        }

        sb.Append("</size></voffset>");
        sb.AppendLine();

        // TransformNodeTextUsesIconAwareRows rejects alpha-hidden glyphs, so whitespace reserves the icon row.
        AppendEmColumn(sb, ColorTextColumnCenter);
        sb.AppendLine(" ");

        // The rate row lifts a little above the natural pitch so it sits directly under the strobeEasing icon.
        sb.Append(ColorStrobeRateOffsetTag);
        AppendEmColumn(sb, ColorTextColumnCenter);
        sb.Append(ColorStrobeSizeTag);
        if (evt.ChromaStrobeInterval is { } strobeInterval && strobeInterval > 0f)
        {
            sb.Append(FormatStrobeInterval(strobeInterval));
        }
        else if (evt.Frequency > 0)
        {
            sb.Append($"1/{evt.Frequency}");
        }
        else
        {
            // ColorInfoKeepsRowsFixedWithoutOptionalValues prevents an absent interval from recentering unrelated rows.
            sb.Append(' ');
        }

        sb.Append("</size></voffset>");
        sb.AppendLine();

        // The bottom-left abbrev renders only when customData.strobeColorEasing is authored.
        sb.Append(ColorStrobeAbbrevOffsetTag);
        AppendEmColumn(sb, -ColorTextColumnCenter);
        sb.Append(ColorLabelSizeTag);
        if (evt.ChromaStrobeColorEasing is { } strobeColorEasing)
        {
            sb.Append(Easing.IDToShortName.GetValueOrDefault(strobeColorEasing));
        }
        else
        {
            // ColorInfoKeepsRowsFixedWithoutOptionalValues preserves the final row's metrics without rendering fallback text.
            sb.Append(' ');
        }

        sb.Append("</size></voffset>");
        sb.Append("</line-height>");
        // ShiftedColorNodeInfoOmitsDistributionMarkers leaves shift visualization to the color bands so compact node text never gains triangle-like glyphs.
        return sb.ToString();
    }

    private static string FormatStrobeInterval(float strobeInterval)
    {
        // Force the requested number of decimal places so values like 2.0 render as 2.00 and .333 as .333.
        var format = strobeInterval >= 10f
            ? "0.0"
            : "0.00";
        return strobeInterval.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string GetRotationInfo(BaseLightRotationBase evt)
    {
        // OE-style rotation text uses the shared three-row GLS layout: loop in the direction icon, easing below its icon, value last.
        return GetTransformInfo(
            evt.Rotation.ToString(CultureInfo.InvariantCulture),
            Easing.IDToShortName.GetValueOrDefault(evt.EaseType),
            evt.Loop.ToString(CultureInfo.InvariantCulture));
    }

    public static Color GetAxisColor(BaseGLSEvent evt, EventAppearanceSO eventAppearance)
    {
        // GLS X remains the neutral ring gray, while Y/Z reuse Basic Event CW/CCW's light and dark grays.
        return evt.EventBoxData?.GetAxis() switch
        {
            Axis.Y => eventAppearance.RingEventsClockwiseColor,
            Axis.Z => eventAppearance.RingEventsCounterClockwiseColor,
            _ => eventAppearance.RingEventsColor,
        };
    }

    public static string GetTranslationInfo(BaseLightTranslationBase evt)
    {
        // Translation uses the same easing and value rows as rotation, with an empty direction-icon row centered above them.
        return GetTransformInfo(
            GLSEventTranslationCommand.IsYeet(evt.Translation)
                ? "YEET"
                : (evt.Translation * 100f).ToString(CultureInfo.InvariantCulture),
            Easing.IDToShortName.GetValueOrDefault(evt.EaseType));
    }

    // TransformNodeTextUsesIconAwareRows keeps both text faces on one TMP draw while centering rotation labels in mirrored columns.
    private static string GetTransformInfo(string value, string easing, string loop = null)
    {
        var sb = new StringBuilder(192);
        // TransformNodeTextUsesIconAwareRows applies the shared 49% label sizing before either a loop value or an empty row is emitted.
        sb.Append("<line-height=55%>");
        sb.Append(TransformLabelSizeTag);
        if (loop != null)
        {
            // RotationDirectionAndLoopRowsReceiveIndependentOffsets lowers the loop count by one thirtieth of the node height.
            sb.Append(TransformLoopOffsetTag);
            AppendColumn(sb, -RotationColumnHorizontalOffset);
            sb.Append(loop);
            sb.Append("</voffset>");
            sb.AppendLine();
            sb.Append(TransformLabelOffsetTag);
            AppendColumn(sb, RotationColumnHorizontalOffset);
        }
        else
        {
            // TransformNodeTextUsesIconAwareRows uses whitespace for line metrics because alpha-zero glyphs render black in the bloom text shader.
            sb.Append(TransformLoopOffsetTag);
            sb.Append(" </voffset>");
            sb.AppendLine();
            sb.Append(TransformLabelOffsetTag);
            sb.Append("<align=center>");
        }

        sb.Append(easing);
        sb.AppendLine("</voffset></size>");
        // TransformValueBaselinesMatchAcrossNodeTypes resets an identically sized first row before applying the shared bottom-value offset.
        sb.Append(TransformValueTag);
        sb.Append(value);
        sb.Append("</voffset></size></line-height>");
        return sb.ToString();
    }

    // TransformNodeTextUsesIconAwareRows derives symmetric TMP margins from the requested column center instead of hand-tuned positions.
    private static void AppendColumn(StringBuilder sb, float center)
    {
        var halfFace = TransformTextFaceWidth * 0.5f;
        var halfColumn = TransformTextColumnWidth * 0.5f;
        var leftMargin = ((center - halfColumn + halfFace) / TransformTextFaceWidth) * 100f;
        var rightMargin = ((halfFace - center - halfColumn) / TransformTextFaceWidth) * 100f;
        sb.Append("<margin-left=");
        sb.Append(leftMargin.ToString("0.###", CultureInfo.InvariantCulture));
        sb.Append("%><margin-right=");
        sb.Append(rightMargin.ToString("0.###", CultureInfo.InvariantCulture));
        sb.Append("%><align=center>");
    }

    // ColorInfoRowsLandInTheirColumnsAndBands: TMP resolves margin percentages against the font size, which
    // collapses any column narrower than the whole face; em margins reach the measured 4-unit face instead.
    private static void AppendEmColumn(StringBuilder sb, float center)
    {
        var halfColumn = TransformTextColumnWidth * 0.5f;
        var leftMargin = (0.6f + center - halfColumn) / ColorTextEmScale;
        var rightMargin = (0.6f - center - halfColumn) / ColorTextEmScale;
        sb.Append("<margin-left=");
        sb.Append(leftMargin.ToString("0.###", CultureInfo.InvariantCulture));
        sb.Append("em><margin-right=");
        sb.Append(rightMargin.ToString("0.###", CultureInfo.InvariantCulture));
        sb.Append("em><align=center>");
    }

    public static string GetFloatFXInfo(BaseFxEventFloat evt)
    {
        // FloatFxUsesTranslationLayout preserves percent-scaled values while sharing translation's centered easing and bottom value rows.
        return GetTransformInfo(
            (evt.Value * 100f).ToString(CultureInfo.InvariantCulture),
            Easing.IDToShortName.GetValueOrDefault(evt.Easing));
    }

    // Environment initialization supplies counts once, keeping scene discovery out of timeline and viewport queries.
    public static void ResetColorTransitionLightCounts()
    {
        colorLightCounts.Clear();
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        foreach (var entry in colorTransitionCaches)
        {
            entry.Value.InvalidateTimeline();
            dirtyColorTimelines.Add(entry.Key);
        }
    }

    // Count changes invalidate one ID; repeated appearance calls do not rebuild an unchanged timeline.
    public static void SetColorTransitionLightCount(int groupId, int lightCount)
    {
        if (lightCount > 0 && (!colorLightCounts.TryGetValue(groupId, out var previous) || previous != lightCount))
        {
            colorLightCounts[groupId] = lightCount;
            dirtyColorTimelines.Add(groupId);
        }
    }

    // All rendering and retention consumers share an ID-scoped schedule instead of querying serialized filter equality.
    public static GLSColorTimeline GetColorTimeline(BaseLightColorBase node, int lightCount)
    {
        var song = BeatSaberSongContainer.Instance;
        if (song == null || song.Map == null || node.EventBoxGroupData == null || lightCount <= 0)
            return null;
        SetColorTransitionLightCount(node.EventBoxGroupData.ID, lightCount);
        EnsureColorTransitionCache(song.Map);
        return colorTransitionCaches.TryGetValue(node.EventBoxGroupData.ID, out var cache)
            ? cache.GetTimeline(song.Map, lightCount)
            : null;
    }

    // Render each transition ribbon forward from its preceding matching-filter node to the transition target.
    public static void UpdateColorTransitionRibbon(
        LightGradientController controller,
        BaseLightColorBase source,
        EventAppearanceSO eventAppearance,
        Func<float, bool> isBoostAt,
        int lightCount,
        bool aggregateSameTimeBoxes = false)
    {
        // RibbonPixelsMatchEachOwnedPreviewLight follows each light's actual next event and distributed clock.
        if (lightCount > 0)
        {
            // Outer timestamps represent multiple boxes; inner lanes retain source-specific ownership.
            controller.UpdateColorTimeline(GetColorTimeline(source, lightCount), source, false, eventAppearance, isBoostAt,
                aggregateSameTimeBoxes);
            return;
        }
        if (!TryGetFollowingColorTransition(source, out var transition, out var followingEvent))
        {
            // LogRibbonDiagnostic(source, followingEvent, null, null);
            controller.SetVisible(false);
            return;
        }

        var sourceBoost = isBoostAt(source.JsonTime);
        var transitionBoost = isBoostAt(transition.JsonTime);
        var startColor = GetLightColor(source, sourceBoost, eventAppearance);
        var endColor = GetLightColor(transition, transitionBoost, eventAppearance);
        // LogRibbonDiagnostic(source, followingEvent, startColor, endColor);
        // RibbonGradientUsesColorEasingOverIntervalEasing: the ribbon follows the color track's
        // customData.colorEasing override rather than the interval's own transition curve.
        var gradient = new ChromaLightGradient(
            startColor,
            endColor,
            transition.SongBpmTime - source.SongBpmTime,
            Easing.InternalNameForID(transition.ChromaColorEasing ?? transition.Easing));
        var startFrequency = GetStrobeFrequency(source);
        var endFrequency = GetStrobeFrequency(transition);
        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips uploads all endpoint phases before the shader chooses the temporal normal or strobe color for every fragment.
        controller.UpdateColorTransitionDistribution(
            source,
            transition,
            lightCount,
            sourceBoost,
            transitionBoost,
            eventAppearance);
        // StrobingTransitionRibbonUsesDestinationEasedPhaseColor keeps either endpoint trigger active and composes strobe brightness before temporal easing.
        var strobeGradient = startFrequency > 0f || endFrequency > 0f
            ? new ChromaLightGradient(
                GetLightStrobeColor(source, sourceBoost, eventAppearance),
                GetLightStrobeColor(transition, transitionBoost, eventAppearance),
                gradient.Duration)
            : null;
        controller.SetVisible(true);
        // RibbonGradientUsesAheadNodeHsvEasingType: the ahead node owns the transition's color space,
        // so its customData.easingType picks the ribbon's angular HSV branch.
        // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint uses the transition target's fade mode, matching LightColorGroupEffect's interpolated-event state.
        // Passing null easeType keeps the ribbon on the gradient's resolved colorEasing, matching RibbonGradientUsesColorEasingOverIntervalEasing.
        controller.UpdateGradientData(
            gradient,
            BasicEventColorLerp.FromGlsEasingType(transition.CustomLerpType),
            strobeGradient,
            null,
            startFrequency,
            endFrequency,
            transition.StrobeFade == 1);
        controller.UpdateDuration(gradient.Duration);
    }

    // InnerFirstNodeHasIncomingRibbonFromA adds only cross-group predecessors; same-group intervals retain their existing forward owner.
    public static void UpdateIncomingColorTransitionRibbon(
        LightGradientController controller, BaseLightColorBase target, EventAppearanceSO appearance,
        Func<float, bool> isBoostAt, int lightCount) =>
        controller.UpdateColorTimeline(GetColorTimeline(target, lightCount), target, true, appearance, isBoostAt);

    private static bool TryGetFollowingColorTransition(
        BaseLightColorBase source,
        out BaseLightColorBase transition,
        out BaseLightColorBase followingEvent)
    {
        transition = null;
        followingEvent = null;
        var filter = source.EventBoxData?.IndexFilter;
        var sourceGroup = source.EventBoxGroupData as BaseLightColorEventBoxGroup;
        // Unity singletons need explicit null checks before reaching map-owned GLS groups.
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (filter == null || sourceGroup == null || map == null)
        {
            return false;
        }

        EnsureColorTransitionCache(map);
        if (colorTransitionCaches.TryGetValue(sourceGroup.ID, out var groupCache))
        {
            groupCache.TryGetFollowingEvent(source, out followingEvent);
        }

        transition = followingEvent is { UsePrevious: 0 } && followingEvent.Easing != (int)EaseType.None
            ? followingEvent
            : null;
        return transition != null;
    }

    // Add only the inserted group's nodes and rewire the neighboring matching-filter events.
    public static void AddColorTransitionGroup(BaseLightColorEventBoxGroup group)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        if (!ReferenceEquals(cachedColorTransitionMap, map))
        {
            EnsureColorTransitionCache(map);
            return;
        }

        if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
        {
            groupCache = new ColorTransitionGroupCache();
            colorTransitionCaches.Add(group.ID, groupCache);
        }

        groupCache.AddGroup(group);
        // Refresh only edited IDs before the next render or indexed viewport query.
        dirtyColorTimelines.Add(group.ID);
    }

    // Remove only the deleted group's nodes and reconnect the closest matching-filter neighbors.
    public static void RemoveColorTransitionGroup(BaseLightColorEventBoxGroup group)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        if (!ReferenceEquals(cachedColorTransitionMap, map))
        {
            EnsureColorTransitionCache(map);
            return;
        }

        if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
        {
            return;
        }

        groupCache.RemoveGroup(group);
        // A removed target can reconnect an earlier source across a different filter.
        dirtyColorTimelines.Add(group.ID);
        if (groupCache.IsEmpty)
        {
            // The final group's removal has no later cache query to retire its previous indexed spans.
            groupCache.ClearTimelineIntervals();
            colorTransitionCaches.Remove(group.ID);
        }
    }

    // Expose the matched transition endpoint so inner pooling can retain an offscreen ribbon source.
    public static bool TryGetColorTransitionEndTime(BaseLightColorBase source, out float endTime)
    {
        endTime = 0f;
        // A source can finish at different nodes per light; retention ends only after its last physical strip.
        if (source.EventBoxGroupData != null && colorLightCounts.TryGetValue(source.EventBoxGroupData.ID, out var count))
        {
            var timeline = GetColorTimeline(source, count);
            return timeline != null && timeline.TryGetBounds(source, out _, out endTime);
        }
        if (!TryGetFollowingColorTransition(source, out var transition, out _))
            return false;

        endTime = transition.SongBpmTime;
        return true;
    }

    // Inner cross-group predecessors may begin before the target node; keep that target alive while its incoming ribbon is visible.
    public static bool TryGetColorRibbonBounds(BaseLightColorBase node, int lightCount, out float start, out float end)
    {
        start = node.SongBpmTime;
        end = start;
        var timeline = GetColorTimeline(node, lightCount);
        return timeline != null && colorTransitionCaches[node.EventBoxGroupData.ID].TryGetInnerBounds(node, out start, out end);
    }

    // GLSColorEasingInputTest.ColorRibbon*: a GLS color transition ribbon is empty interval space
    // between nodes, so only color-typed containers can own one; Basic Event and non-color GLS
    // containers keep their authored-node hit behavior. This mirrors the appearance SOs, which
    // attach ribbons only where the represented data is BaseLightColorBase.
    private static bool IsColorTransitionContainer(ObjectContainer container) =>
        container != null
        && (container is GLSEventContainer { EventData: BaseLightColorBase }
            or GLSGroupContainer { PreviewEventData: BaseLightColorBase });

    // GLSEasingTypeRibbonInputTest: a hovered ribbon is identified by the physical hit landing on a
    // LightGradientController child of the resolved container, matching the Basic Event ribbon check.
    // GLSColorEasingInputTest.ColorRibbon*: the container must also be color-typed so placement and
    // group-entry consumers can distinguish a ribbon from the source node's body.
    public static bool IsColorTransitionRibbonHit(ObjectContainer container) =>
        container != null
        && IsColorTransitionRibbonHit()
        && ReferenceEquals(classifiedRibbonOwner, container);

    // ColorRibbonHitClassificationRefreshesAfterOwnerIsRebound: cache the stable hierarchy, not the
    // pooled container's data type, which can change between consecutive hits on the same object.
    private static GameObject classifiedRibbonHit;
    private static ObjectContainer classifiedRibbonOwner;
    // Incoming ribbons are target-owned, so retain the already-resolved controller for their easing callback.
    private static LightGradientController classifiedRibbonController;

    // GLSColorEasingInputTest.ColorRibbon*: placement CanPlace gates see only the shared raycast hit,
    // so this overload resolves the ribbon's owning container from it without a supplied target.
    public static bool IsColorTransitionRibbonHit()
    {
        if (!ReferenceEquals(classifiedRibbonHit, BeatmapRaycastCache.FirstHit))
        {
            classifiedRibbonHit = BeatmapRaycastCache.FirstHit;
            var ribbon = classifiedRibbonHit != null
                ? classifiedRibbonHit.GetComponentInParent<LightGradientController>()
                : null;
            classifiedRibbonOwner = ribbon != null ? ribbon.GetComponentInParent<ObjectContainer>() : null;
            classifiedRibbonController = ribbon;
        }

        // ColorRibbonHitClassificationRefreshesAfterOwnerIsRebound requires inspecting the current
        // represented event while all placement and easing consumers reuse the same hierarchy lookup.
        return IsColorTransitionContainer(classifiedRibbonOwner);
    }

    // GLSEasingTypeRibbonInputTest: ribbon chords mutate the transition's ahead node, which owns the
    // interval's color easing, strobe easings, and easingType; the ribbon's source node is only the anchor.
    public static bool TryGetColorTransitionTarget(
        ObjectContainer container,
        BaseLightColorBase source,
        out BaseLightColorBase transition)
    {
        transition = null;
        if (source == null || !IsColorTransitionRibbonHit(container))
            return false;
        if (source.EventBoxGroupData != null && colorLightCounts.TryGetValue(source.EventBoxGroupData.ID, out var count))
        {
            var timeline = GetColorTimeline(source, count);
            if (timeline != null)
            {
                // RibbonHoverResolvesThePhysicalLightDestination picks the same UV strip and active clock interval as the shader.
                if (BeatmapRaycastCache.FirstHitPoint is { } point && classifiedRibbonController.ColorTimelineDuration > 0f)
                {
                    var uv = classifiedRibbonController.GetHitUv(point);
                    var light = count - 1 - Mathf.Clamp(Mathf.FloorToInt(uv.y * count), 0, count - 1);
                    var time = classifiedRibbonController.ColorTimelineStart + (uv.x * classifiedRibbonController.ColorTimelineDuration);
                    var incoming = classifiedRibbonController.IsIncomingColorTransition;
                    LightColorEventStateData state;
                    // Hover on a combined outer ribbon follows the hit light's box, just like its uploaded strip.
                    var found = incoming
                        ? timeline.TryGetIncoming(source, light, out state)
                        : classifiedRibbonController.AggregatesSameTimeBoxes
                            ? timeline.TryGetOutgoingAtGroupTime(source, light, out state)
                            : timeline.TryGetOutgoing(source, light, out state);
                    if (!found || state.EndTime == float.MaxValue || time < state.StartTime || time > state.EndTime
                        || (incoming && ReferenceEquals(state.Base.EventBoxGroupData, source.EventBoxGroupData)))
                    {
                        return false;
                    }
                    transition = incoming ? source : state.Next.Base;
                    return true;
                }
                // Inner incoming strips belong to the node being approached, never its next outgoing endpoint.
                if (classifiedRibbonController.IsIncomingColorTransition)
                {
                    transition = source;
                    return true;
                }
                for (var light = 0; light < count; light++)
                {
                    // Programmatic hover callers without a hit point still use the same timestamp aggregation policy.
                    LightColorEventStateData state;
                    var found = classifiedRibbonController.AggregatesSameTimeBoxes
                        ? timeline.TryGetOutgoingAtGroupTime(source, light, out state)
                        : timeline.TryGetOutgoing(source, light, out state);
                    if (!found || state.EndTime == float.MaxValue)
                        continue;
                    transition = state.Next.Base;
                    return true;
                }
                return false;
            }
        }
        return TryGetFollowingColorTransition(source, out transition, out _);
    }

    // A masked strip is still a ribbon hit, not a node-body fallback; null targets make its hover chords no-ops.
    public static bool IsColorRibbonHover(ObjectContainer container, BaseLightColorBase source, out BaseLightColorBase target)
    {
        target = null;
        if (!IsColorTransitionRibbonHit(container))
            return false;
        TryGetColorTransitionTarget(container, source, out target);
        return true;
    }

    // Find offscreen source groups whose ribbons cross a pool boundary in either scroll direction.
    public static void GetColorTransitionSourceGroupsAt(
        float boundary,
        string trackFilter,
        ISet<BaseLightColorEventBoxGroup> sourceGroups)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        EnsureColorTransitionCache(map);
        colorTransitionQueryResults.Clear();
        // Known groups use their per-light bounds; unknown environments retain the previous fallback.
        GetColorRibbonSourcesAt(boundary, colorTransitionQueryResults, false);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var sourceGroup = colorTransitionQueryResults[sourceIndex].EventBoxGroupData as BaseLightColorEventBoxGroup;
            if (sourceGroup != null && sourceGroup.HasMatchingTrack(trackFilter))
            {
                sourceGroups.Add(sourceGroup);
            }
        }
    }

    // Query indexed source intervals for the inner GLS editor without walking its complete event list.
    public static void GetColorTransitionSourcesAt(
        float boundary,
        BaseEventBoxGroup group,
        string trackFilter,
        List<BaseLightColorBase> sources)
    {
        sources.Clear();
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        if (map == null)
        {
            return;
        }

        EnsureColorTransitionCache(map);
        // The inner index includes first nodes whose incoming ribbon starts in a preceding group.
        GetColorRibbonSourcesAt(boundary, sources, true);
        for (var sourceIndex = sources.Count - 1; sourceIndex >= 0; sourceIndex--)
        {
            var source = sources[sourceIndex];
            if (!ReferenceEquals(source.EventBoxGroupData, group) || !source.HasMatchingTrack(trackFilter))
            {
                sources.Remove(source);
            }
        }
    }

    // Merge only interval query results, not entire groups, and suppress the identical-filter fallback when real light counts are known.
    private static void GetColorRibbonSourcesAt(float boundary, List<BaseLightColorBase> sources, bool inner)
    {
        colorTransitionIntervals.GetSourcesAt(boundary, sources);
        for (var index = sources.Count - 1; index >= 0; index--)
        {
            if (colorLightCounts.ContainsKey(sources[index].EventBoxGroupData.ID))
                sources.RemoveAt(index);
        }
        (inner ? colorInnerIntervals : colorOutgoingIntervals).GetSourcesAt(boundary, sources);
    }

    // Keep GLS transition qualification with its timeline owner while sharing the allocation-free interval tree.
    private static void ReplaceColorTransitionSequence(
        IList<BaseLightColorBase> events,
        IDictionary<BaseLightColorBase, BaseLightColorBase> followingEvents)
    {
        for (var eventIndex = 0; eventIndex < events.Count; eventIndex++)
        {
            colorTransitionIntervals.Remove(events[eventIndex]);
        }

        foreach (var followingEvent in followingEvents)
        {
            var transition = followingEvent.Value;
            if (transition.UsePrevious == 0 && transition.Easing != (int)EaseType.None)
            {
                colorTransitionIntervals.AddOrReplace(
                    followingEvent.Key,
                    followingEvent.Key.SongBpmTime,
                    transition.SongBpmTime);
            }
        }
    }

    // Build one chronological sequence per group ID and equivalent filter, including overlapping group ranges.
    private static void EnsureColorTransitionCache(BaseDifficulty map)
    {
        if (ReferenceEquals(cachedColorTransitionMap, map))
        {
            // Viewport updates pay only for pending edit invalidations, never a repeated map scan.
            RefreshDirtyColorTimelines(map);
            return;
        }

        colorTransitionCaches.Clear();
        colorTransitionIntervals.Clear();
        // Physical-light indexes share the map lifetime with the existing fallback sequences.
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        dirtyColorTimelines.Clear();
        foreach (var group in map.LightColorEventBoxGroups)
        {
            if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
            {
                groupCache = new ColorTransitionGroupCache();
                colorTransitionCaches.Add(group.ID, groupCache);
            }

            groupCache.AddGroupWithoutRewiring(group);
        }

        foreach (var entry in colorTransitionCaches)
        {
            entry.Value.RebuildAllTransitions();
            // Build known physical groups once even if all their ribbon source nodes begin offscreen.
            if (colorLightCounts.TryGetValue(entry.Key, out var lightCount))
                entry.Value.GetTimeline(map, lightCount);
        }

        cachedColorTransitionMap = map;
    }

    // Add/remove and environment changes queue IDs; an unchanged frame has no timeline work here.
    private static void RefreshDirtyColorTimelines(BaseDifficulty map)
    {
        foreach (var id in dirtyColorTimelines)
        {
            if (!colorTransitionCaches.TryGetValue(id, out var cache))
                continue;
            if (colorLightCounts.TryGetValue(id, out var count))
                cache.GetTimeline(map, count);
            else
                cache.EnsureLegacyTimeline();
        }
        dirtyColorTimelines.Clear();
    }

    private static int CompareColorEvents(BaseLightColorBase left, BaseLightColorBase right)
    {
        var comparison = left.JsonTime.CompareTo(right.JsonTime);
        if (comparison != 0)
        {
            return comparison;
        }

        comparison = left.BoxIndex.CompareTo(right.BoxIndex);
        return comparison != 0
            ? comparison
            : left.EventBoxGroupData.JsonTime.CompareTo(right.EventBoxGroupData.JsonTime);
    }

    private sealed class ColorTransitionGroupCache
    {
        private readonly List<ColorFilterSequence> sequences = new();
        // Exact group identities support constant-time edit membership; ordering is materialized only for initial/count-change rebuilds.
        private readonly Dictionary<BaseLightColorEventBoxGroup, long> groups = new();
        private readonly List<BaseLightColorEventBoxGroup> orderedGroups = new();
        private readonly HashSet<BaseLightColorBase> pendingNodes = new();
        private readonly Dictionary<BaseLightColorBase, (float Start, float End)> innerBounds = new();
        private long nextGroupOrder;
        private GLSColorTimeline timeline;
        private bool timelineDirty = true;
        private bool legacyDirty;
        // Resolve an event's filter timeline without scanning its sibling filters during ribbon rendering.
        private readonly Dictionary<BaseLightColorBase, ColorFilterSequence> sequenceByEvent = new();
        // A cache miss may occur during repeated appearance refreshes, so compare equivalent identities only once per requested clone.
        private readonly HashSet<BaseLightColorBase> identityMissDiagnosticSources = new();

        // Empty authored boxes still own their lights, so an ID is empty only after its last group is removed.
        public bool IsEmpty => groups.Count == 0;

        // TimelineEditsRetainUnchangedLightsAndRefreshBounds preserves the schedule and reindexes only changed source/target identities.
        public GLSColorTimeline GetTimeline(BaseDifficulty map, int lightCount)
        {
            if (timelineDirty || timeline == null || timeline.LightCount != lightCount)
            {
                ClearTimelineIntervals();
                timeline = new GLSColorTimeline(map, lightCount, GetOrderedGroups());
                timelineDirty = false;
                pendingNodes.Clear();
                foreach (var source in timeline.Sources)
                {
                    pendingNodes.Add(source);
                    for (var light = 0; light < lightCount; light++)
                    {
                        if (timeline.TryGetOutgoing(source, light, out var state) && state.EndTime < float.MaxValue)
                            pendingNodes.Add(state.Next.Base);
                    }
                }
            }
            RefreshChangedIntervals();
            return timeline;
        }

        // Stable equal-beat ordering preserves serialized insertion precedence without sorting all groups on ordinary edits.
        private IReadOnlyList<BaseLightColorEventBoxGroup> GetOrderedGroups()
        {
            orderedGroups.Clear();
            orderedGroups.AddRange(groups.Keys);
            orderedGroups.Sort((left, right) =>
            {
                var time = left.JsonTime.CompareTo(right.JsonTime);
                return time != 0 ? time : groups[left].CompareTo(groups[right]);
            });
            return orderedGroups;
        }

        // Retired identities lose both indexes; unchanged lights and intervals are never traversed by this edit path.
        private void RefreshChangedIntervals()
        {
            foreach (var node in pendingNodes)
            {
                colorOutgoingIntervals.Remove(node);
                colorInnerIntervals.Remove(node);
                innerBounds.Remove(node);
                if (timeline.TryGetBounds(node, out var start, out var end))
                {
                    colorOutgoingIntervals.AddOrReplace(node, start, end);
                    IncludeInnerBounds(node, start, end);
                }
                for (var light = 0; light < timeline.LightCount; light++)
                {
                    if (timeline.TryGetIncoming(node, light, out var previous)
                        && !ReferenceEquals(previous.Base.EventBoxGroupData, node.EventBoxGroupData))
                    {
                        IncludeInnerBounds(node, previous.StartTime, previous.EndTime);
                    }
                }
                if (innerBounds.TryGetValue(node, out var bounds))
                    colorInnerIntervals.AddOrReplace(node, bounds.Start, bounds.End);
            }
            pendingNodes.Clear();
        }

        public bool TryGetInnerBounds(BaseLightColorBase node, out float start, out float end)
        {
            var found = innerBounds.TryGetValue(node, out var bounds);
            start = bounds.Start;
            end = bounds.End;
            return found;
        }

        private void IncludeInnerBounds(BaseLightColorBase node, float start, float end)
        {
            if (innerBounds.TryGetValue(node, out var previous))
            {
                start = Mathf.Min(start, previous.Start);
                end = Mathf.Max(end, previous.End);
            }
            innerBounds[node] = (start, end);
        }

        public void InvalidateTimeline() => timelineDirty = true;

        // Include retired sources from the index snapshot even if the mutable timeline no longer lists them.
        public void ClearTimelineIntervals()
        {
            foreach (var node in innerBounds.Keys)
            {
                colorOutgoingIntervals.Remove(node);
                colorInnerIntervals.Remove(node);
            }
            innerBounds.Clear();
        }

        public void AddGroupWithoutRewiring(BaseLightColorEventBoxGroup group)
        {
            // Preserve group ownership before assembling the legacy fallback for environments without a known light count.
            groups.Add(group, nextGroupOrder++);
            timelineDirty = true;
            AppendLegacyGroup(group);
        }

        // Initial map load and a later unknown-environment fallback share one event registration path.
        private void AppendLegacyGroup(BaseLightColorEventBoxGroup group)
        {
            foreach (var box in group.Boxes)
            {
                var sequence = FindOrCreateSequence(box.IndexFilter);
                sequence.Events.AddRange(box.Events);
                foreach (var evt in box.Events)
                {
                    sequenceByEvent.Add(evt, sequence);
                }
            }
        }

        public void AddGroup(BaseLightColorEventBoxGroup group)
        {
            // Cache initialization may already include this just-spawned identity; registration is idempotent.
            if (!groups.TryAdd(group, nextGroupOrder++))
                return;
            if (timeline != null)
            {
                if (!timelineDirty)
                {
                    timeline.AddGroup(group);
                    pendingNodes.UnionWith(timeline.ChangedNodes);
                }
                legacyDirty = true;
                return;
            }
            AddLegacyGroup(group);
        }

        // Unknown light counts retain the prior filter-only path without imposing its scans on real per-light edits.
        private void AddLegacyGroup(BaseLightColorEventBoxGroup group)
        {
            var modifiedSequences = new Dictionary<ColorFilterSequence, TimeRange>();
            foreach (var box in group.Boxes)
            {
                var sequence = FindOrCreateSequence(box.IndexFilter);
                foreach (var evt in box.Events)
                {
                    var index = sequence.Events.BinarySearch(evt, colorEventComparer);
                    sequence.Events.Insert(index >= 0 ? index : ~index, evt);
                    sequenceByEvent.Add(evt, sequence);
                    AddModifiedTime(modifiedSequences, sequence, evt.JsonTime);
                }
            }

            RewireModifiedSequences(modifiedSequences);
        }

        public void RemoveGroup(BaseLightColorEventBoxGroup group)
        {
            // Remove only this claimant and its affected neighbors, preserving the other lights' prepared states.
            if (!groups.Remove(group))
                return;
            if (timeline != null)
            {
                if (!timelineDirty)
                {
                    timeline.RemoveGroup(group);
                    pendingNodes.UnionWith(timeline.ChangedNodes);
                }
                legacyDirty = true;
                return;
            }
            RemoveLegacyGroup(group);
        }

        // Keep the fallback implementation isolated from the known-light incremental schedule.
        private void RemoveLegacyGroup(BaseLightColorEventBoxGroup group)
        {
            var modifiedSequences = new Dictionary<ColorFilterSequence, TimeRange>();
            foreach (var box in group.Boxes)
            {
                var sequence = FindSequence(box.IndexFilter);
                if (sequence == null)
                {
                    continue;
                }

                foreach (var evt in box.Events)
                {
                    // Removed nodes are absent from the rewired sequence, so clear their old viewport-retention intervals first.
                    colorTransitionIntervals.Remove(evt);
                    if (!sequence.Events.Remove(evt))
                    {
                        continue;
                    }

                    sequence.FollowingEvents.Remove(evt);
                    sequenceByEvent.Remove(evt);
                    AddModifiedTime(modifiedSequences, sequence, evt.JsonTime);
                }

                if (sequence.Events.Count == 0)
                {
                    sequences.Remove(sequence);
                }
            }

            RewireModifiedSequences(modifiedSequences);
        }

        public void RebuildAllTransitions()
        {
            foreach (var sequence in sequences)
            {
                sequence.Events.Sort(CompareColorEvents);
                sequence.RewireAll();
                ReplaceColorTransitionSequence(sequence.Events, sequence.FollowingEvents);
            }
        }

        // Rebuild filter-only fallback data only if that fallback is actually requested after a physical-light edit.
        public void EnsureLegacyTimeline()
        {
            if (!legacyDirty)
                return;
            foreach (var sequence in sequences)
            {
                foreach (var node in sequence.Events)
                    colorTransitionIntervals.Remove(node);
            }
            sequences.Clear();
            sequenceByEvent.Clear();
            foreach (var group in GetOrderedGroups())
                AppendLegacyGroup(group);
            legacyDirty = false;
            RebuildAllTransitions();
        }

        public bool TryGetFollowingEvent(BaseLightColorBase source, out BaseLightColorBase followingEvent)
        {
            // Unknown environments must never use stale fallback identities after real-light edits.
            EnsureLegacyTimeline();
            var sourceFound = sequenceByEvent.TryGetValue(source, out var sequence);
            if (sourceFound)
            {
                if (sequence.FollowingEvents.TryGetValue(source, out followingEvent))
                {
                    return true;
                }
            }

            // An equivalent event is only an identity miss when the requested source itself is absent, not merely the final node in a sequence.
            if (sourceFound)
            {
                followingEvent = null;
                return false;
            }

            if (!identityMissDiagnosticSources.Add(source))
            {
                followingEvent = null;
                return false;
            }

            followingEvent = null;
            return false;
        }

        private static void AddModifiedTime(
            Dictionary<ColorFilterSequence, TimeRange> modifiedSequences,
            ColorFilterSequence sequence,
            float time)
        {
            if (modifiedSequences.TryGetValue(sequence, out var range))
            {
                range.Include(time);
                modifiedSequences[sequence] = range;
            }
            else
            {
                modifiedSequences.Add(sequence, new TimeRange(time));
            }
        }

        private static void RewireModifiedSequences(Dictionary<ColorFilterSequence, TimeRange> modifiedSequences)
        {
            foreach (var modifiedSequence in modifiedSequences)
            {
                // Only the edited range and its immediately preceding matching-filter timestamp can change successor links.
                modifiedSequence.Key.RewireRange(modifiedSequence.Value.Minimum, modifiedSequence.Value.Maximum);
                // Reindex only changed filter timelines so scrolling never pays for edit-time cache maintenance.
                ReplaceColorTransitionSequence(
                    modifiedSequence.Key.Events,
                    modifiedSequence.Key.FollowingEvents);
            }
        }

        private ColorFilterSequence FindOrCreateSequence(BaseIndexFilter filter)
        {
            var sequence = FindSequence(filter);
            if (sequence != null)
            {
                return sequence;
            }

            sequence = new ColorFilterSequence(filter);
            sequences.Add(sequence);
            return sequence;
        }

        private ColorFilterSequence FindSequence(BaseIndexFilter filter)
        {
            foreach (var sequence in sequences)
            {
                if (IndexFiltersMatch(sequence.Filter, filter))
                {
                    return sequence;
                }
            }

            return null;
        }
    }

    private sealed class ColorFilterSequence
    {
        public ColorFilterSequence(BaseIndexFilter filter)
        {
            Filter = filter;
        }

        public BaseIndexFilter Filter { get; }
        public List<BaseLightColorBase> Events { get; } = new();
        public Dictionary<BaseLightColorBase, BaseLightColorBase> FollowingEvents { get; } = new();

        public void RewireAll()
        {
            FollowingEvents.Clear();
            RewireRange(float.NegativeInfinity, float.PositiveInfinity);
        }

        public void RewireRange(float minimumTime, float maximumTime)
        {
            if (Events.Count == 0)
            {
                return;
            }

            var firstChangedIndex = LowerBound(minimumTime);
            var startIndex = firstChangedIndex > 0
                ? LowerBound(Events[firstChangedIndex - 1].JsonTime)
                : firstChangedIndex;
            var endIndex = UpperBound(maximumTime);
            var following = endIndex < Events.Count ? Events[endIndex] : null;

            for (var eventIndex = endIndex - 1; eventIndex >= startIndex; eventIndex--)
            {
                var current = Events[eventIndex];
                FollowingEvents.Remove(current);
                if (following != null && following.JsonTime > current.JsonTime)
                {
                    FollowingEvents[current] = following;
                }

                if (following == null || current.JsonTime <= following.JsonTime)
                {
                    following = current;
                }
            }
        }

        private int LowerBound(float time)
        {
            var low = 0;
            var high = Events.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (Events[middle].JsonTime < time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }

        private int UpperBound(float time)
        {
            var low = 0;
            var high = Events.Count;
            while (low < high)
            {
                var middle = low + ((high - low) / 2);
                if (Events[middle].JsonTime <= time)
                {
                    low = middle + 1;
                }
                else
                {
                    high = middle;
                }
            }

            return low;
        }
    }

    private struct TimeRange
    {
        public TimeRange(float time)
        {
            Minimum = time;
            Maximum = time;
        }

        public float Minimum;
        public float Maximum;

        public void Include(float time)
        {
            Minimum = Mathf.Min(Minimum, time);
            Maximum = Mathf.Max(Maximum, time);
        }
    }

    // Index filters are cloned per GLS group, so compare their serialized matching parameters instead of references.
    private static bool IndexFiltersMatch(BaseIndexFilter left, BaseIndexFilter right) =>
        right != null
        && left.Type == right.Type
        && left.Param0 == right.Param0
        && left.Param1 == right.Param1
        && left.Reverse == right.Reverse
        && left.Chunks == right.Chunks
        && left.Random == right.Random
        && left.Seed == right.Seed
        && Mathf.Approximately(left.Limit, right.Limit)
        && left.LimitAffectsType == right.LimitAffectsType;
}

// ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights keeps each rendered node's CPU lookup tables and GPU texture together across pooled appearance refreshes.
public sealed class GLSColorDistributionPreview : IDisposable
{
    // Shader property IDs and upload storage stay node-local or static so appearance refreshes allocate only when the physical group size changes.
    private static readonly int distributionPreviewDepthRangeId = Shader.PropertyToID("_DistributionPreviewDepthRange");
    private static readonly int distributionPreviewEnabledId = Shader.PropertyToID("_DistributionPreviewEnabled");
    private static readonly int distributionPreviewTextureId = Shader.PropertyToID("_DistributionPreviewTex");

    private Texture2D texture;
    private Color[] textureColors = Array.Empty<Color>();

    public Color[] PerLightColors { get; private set; } = Array.Empty<Color>();
    public Color[] PerLightStrobeColors { get; private set; } = Array.Empty<Color>();
    public float[] PerLightDepthTable { get; private set; } = Array.Empty<float>();

    // ShiftedColorPreviewCachesSelectedLightsAndBlacksSkippedLights refreshes the owned lookup only when appearance data changes, leaving fragment work to one indexed texture sample.
    public void Update(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        MaterialPropertyBlock properties)
    {
        EnsureCapacity(lightCount);
        var enabled = GLSEventCommon.PopulateColorDistributionPreview(
            evt,
            lightCount,
            boost,
            eventAppearance,
            PerLightColors,
            PerLightStrobeColors,
            PerLightDepthTable);
        properties.SetFloat(distributionPreviewEnabledId, enabled ? 1f : 0f);
        if (!enabled)
        {
            return;
        }

        // DistributionTextureUsesFullWidthEndpointSections expands the cached center range by half a texel at both ends so every point-sampled light gets equal post-chamfer width.
        var halfLightSection = 0.5f / lightCount;
        properties.SetVector(
            distributionPreviewDepthRangeId,
            new Vector4(
                PerLightDepthTable[0] + halfLightSection,
                PerLightDepthTable[lightCount - 1] - halfLightSection,
                0f,
                0f));
        // MainStrobeBrightnessAndFShiftsUseSharedNodePreviewCurve sends s, sb, f, and HDR value through the same display conversion as the rest of the node.
        for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
        {
            textureColors[lightIndex] = GLSEventCommon.GetNodePreviewColor(
                PerLightColors[lightIndex],
                eventAppearance);
            textureColors[lightCount + lightIndex] = GLSEventCommon.GetNodePreviewColor(
                PerLightStrobeColors[lightIndex],
                eventAppearance);
        }

        texture.SetPixels(textureColors);
        texture.Apply(false, false);
        properties.SetTexture(distributionPreviewTextureId, texture);
    }

    // Pooled GLS renderers retain their property blocks, so every non-color or inactive node must explicitly suppress an earlier distribution texture.
    public static void Disable(MaterialPropertyBlock properties) =>
        properties.SetFloat(distributionPreviewEnabledId, 0f);

    // SourceDistributionTexelRendersIdenticallyToMainNodeSurface keeps normalized display colors in sRGB storage so texture sampling matches shader Color-property conversion.
    private void EnsureCapacity(int lightCount)
    {
        if (lightCount <= 0 || PerLightColors.Length == lightCount)
        {
            return;
        }

        ReleaseTexture();
        PerLightColors = new Color[lightCount];
        PerLightStrobeColors = new Color[lightCount];
        PerLightDepthTable = new float[lightCount];
        textureColors = new Color[lightCount * 2];
        texture = new Texture2D(lightCount, 2, TextureFormat.RGBA32, false, false)
        {
            name = "GLS Color Distribution Preview",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
    }

    // Runtime-created preview textures are not asset-owned, so node destruction must release them rather than leaking one texture per formerly visible GLS node.
    public void Dispose()
    {
        ReleaseTexture();
        PerLightColors = Array.Empty<Color>();
        PerLightStrobeColors = Array.Empty<Color>();
        PerLightDepthTable = Array.Empty<float>();
        textureColors = Array.Empty<Color>();
    }

    private void ReleaseTexture()
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(texture);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        texture = null;
    }
}

// LightIdTransitionRibbonSplitsIntoPerLightShiftStrips owns one point-sampled endpoint table per pooled ribbon so the fragment shader performs only indexed texture reads.
public sealed class GLSColorTransitionPreview : IDisposable
{
    private const int SourceNormalRow = 0;
    private const int TransitionNormalRow = 1;
    private const int SourceStrobeRow = 2;
    private const int TransitionStrobeRow = 3;

    private static readonly int lightDistributionTextureId = Shader.PropertyToID("_LightDistributionTex");
    private static readonly int lightDistributionWidthId = Shader.PropertyToID("_LightDistributionWidth");
    private static readonly int useLightDistributionId = Shader.PropertyToID("_UseLightDistribution");
    // Collider wave ribbons upload the preview tween's per-light clocks and separate easing tracks once per refresh.
    private static readonly int useLightTimelineId = Shader.PropertyToID("_UseLightTimeline");
    private static readonly int lightTimelineDurationId = Shader.PropertyToID("_LightTimelineDuration");
    private static readonly Dictionary<Func<float, float>, int> shaderIds = CreateShaderIds();
    private LightColorEventStateData[] timelineStates = Array.Empty<LightColorEventStateData>();
    private LightColorTween[] timelineTweens = Array.Empty<LightColorTween>();

    private Texture2D texture;
    private Color[] sourceColors = Array.Empty<Color>();
    private Color[] sourceStrobeColors = Array.Empty<Color>();
    private Color[] transitionColors = Array.Empty<Color>();
    private Color[] transitionStrobeColors = Array.Empty<Color>();
    private Color[] textureColors = Array.Empty<Color>();

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips computes four renderer-level endpoint rows during appearance refresh, not per pixel.
    public void Update(
        BaseLightColorBase source,
        BaseLightColorBase transition,
        int lightCount,
        bool sourceBoost,
        bool transitionBoost,
        EventAppearanceSO eventAppearance,
        MaterialPropertyBlock properties)
    {
        properties.SetFloat(useLightDistributionId, 0f);
        properties.SetFloat(lightDistributionWidthId, 0f);
        // Unknown-count fallback ribbons must not retain a pooled nine-row timeline payload.
        properties.SetFloat(useLightTimelineId, 0f);
        if (lightCount <= 0)
        {
            return;
        }

        var sourceDistributed = GLSEventCommon.HasColorTransitionDistribution(source, lightCount);
        var transitionDistributed = GLSEventCommon.HasColorTransitionDistribution(transition, lightCount);
        if (!sourceDistributed && !transitionDistributed)
        {
            return;
        }

        EnsureCapacity(lightCount);
        // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips still populates a plain endpoint's selected lights so its strip colors can blend into the shifted endpoint.
        GLSEventCommon.PopulateColorTransitionEndpoint(
            source,
            lightCount,
            sourceBoost,
            eventAppearance,
            sourceColors,
            sourceStrobeColors);
        GLSEventCommon.PopulateColorTransitionEndpoint(
            transition,
            lightCount,
            transitionBoost,
            eventAppearance,
            transitionColors,
            transitionStrobeColors);

        for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
        {
            // Physical light zero renders on the ribbon's positive-width edge to match the reversed front-to-back order used by node distribution strips.
            var textureX = lightCount - lightIndex - 1;
            textureColors[textureX] = sourceColors[lightIndex];
            textureColors[(TransitionNormalRow * lightCount) + textureX] = transitionColors[lightIndex];
            textureColors[(SourceStrobeRow * lightCount) + textureX] = sourceStrobeColors[lightIndex];
            textureColors[(TransitionStrobeRow * lightCount) + textureX] = transitionStrobeColors[lightIndex];
        }

        texture.SetPixels(textureColors);
        texture.Apply(false, false);
        properties.SetTexture(lightDistributionTextureId, texture);
        properties.SetFloat(lightDistributionWidthId, lightCount);
        properties.SetFloat(useLightDistributionId, 1f);
    }

    // Resolve delegates once so packing a visible strip never scans the easing registry.
    private static Dictionary<Func<float, float>, int> CreateShaderIds()
    {
        var result = new Dictionary<Func<float, float>, int>();
        var index = 0;
        foreach (var easing in Easing.ByName)
            result[easing.Value] = index++;
        return result;
    }

    // The source and target slots are the same LightColorEventStateData instances used to prepare preview light tweens.
    public bool UpdateTimeline(
        GLSColorTimeline timeline, BaseLightColorBase owner, bool incoming,
        EventAppearanceSO appearance, Func<float, bool> isBoostAt, MaterialPropertyBlock properties,
        out float start, out float end, bool aggregateSameTimeBoxes = false)
    {
        start = incoming ? float.PositiveInfinity : owner.SongBpmTime;
        end = float.NegativeInfinity;
        properties.SetFloat(useLightTimelineId, 0f);
        if (timeline == null)
            return false;
        var count = timeline.LightCount;
        EnsureCapacity(count, 9);
        Array.Clear(textureColors, 0, textureColors.Length);
        for (var light = 0; light < count; light++)
        {
            LightColorEventStateData state;
            // A deduplicated outer body must not mask lights whose winning source lives in another same-time box.
            var found = incoming
                ? timeline.TryGetIncoming(owner, light, out state)
                : aggregateSameTimeBoxes
                    ? timeline.TryGetOutgoingAtGroupTime(owner, light, out state)
                    : timeline.TryGetOutgoing(owner, light, out state);
            // A terminal sentinel keeps playback held but is not a drawable interval extending to infinity.
            if (!found || state.EndTime == float.MaxValue
                || (incoming && ReferenceEquals(state.Base.EventBoxGroupData, owner.EventBoxGroupData)))
            {
                state = null;
            }
            timelineStates[light] = state;
            if (state != null)
            {
                start = Mathf.Min(start, state.StartTime);
                end = Mathf.Max(end, state.EndTime);
            }
        }
        if (!(end > start))
            return false;
        for (var light = 0; light < count; light++)
        {
            var x = count - light - 1;
            var state = timelineStates[light];
            if (state == null)
            {
                // Preserve the existing black endpoint-table contract; the invalid time range independently masks the strip.
                for (var row = 0; row < 4; row++)
                    textureColors[(row * count) + x] = Color.black;
                textureColors[(4 * count) + x] = new Color(0f, -1f, 0f, -1f);
                continue;
            }
            var tween = timelineTweens[light];
            timeline.ConfigureTween(tween, state, appearance, isBoostAt);
            textureColors[x] = BasicEventColorLerp.ApplyBrightness(tween.StartColor, tween.StartAlpha);
            textureColors[count + x] = BasicEventColorLerp.ApplyBrightness(tween.EndColor, tween.EndAlpha);
            textureColors[(2 * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.StartStrobeColor, tween.StartStrobeBrightness);
            textureColors[(3 * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.EndStrobeColor, tween.EndStrobeBrightness);
            textureColors[(4 * count) + x] = new Color(tween.StartTimeAlpha - start, tween.EndTimeAlpha - start,
                tween.StartTimeColor - start, tween.EndTimeColor - start);
            textureColors[(5 * count) + x] = new Color(tween.StartStrobeFrequency, tween.EndStrobeFrequency,
                tween.StartAlpha, tween.EndAlpha);
            textureColors[(6 * count) + x] = new Color(tween.StartStrobeBrightness, tween.EndStrobeBrightness,
                tween.StartColor.a, tween.EndColor.a);
            textureColors[(7 * count) + x] = new Color(tween.StartStrobeColor.a, tween.EndStrobeColor.a,
                (int)tween.ColorLerpType, (tween.StrobeFade ? 1f : 0f) + (tween.ComposeAlphaAtColorEndpoints ? 2f : 0f));
            textureColors[(8 * count) + x] = new Color(
                shaderIds.GetValueOrDefault(tween.Easing), shaderIds.GetValueOrDefault(tween.ColorEasing ?? tween.Easing),
                shaderIds.GetValueOrDefault(tween.StrobeColorEasing ?? tween.Easing),
                shaderIds.GetValueOrDefault(tween.StrobeEasing ?? Easing.Cubic.InOut));
        }
        texture.SetPixels(textureColors);
        texture.Apply(false, false);
        properties.SetTexture(lightDistributionTextureId, texture);
        properties.SetFloat(lightDistributionWidthId, count);
        properties.SetFloat(useLightDistributionId, 1f);
        properties.SetFloat(useLightTimelineId, 1f);
        properties.SetFloat(lightTimelineDurationId, end - start);
        return true;
    }

    // LightIdTransitionRibbonSplitsIntoPerLightShiftStrips reuses pooled endpoint storage and reallocates only when the physical GLS group size changes.
    private void EnsureCapacity(int lightCount, int rows = 4)
    {
        if (sourceColors.Length == lightCount && texture != null && texture.height == rows)
        {
            return;
        }

        ReleaseTexture();
        sourceColors = new Color[lightCount];
        sourceStrobeColors = new Color[lightCount];
        transitionColors = new Color[lightCount];
        transitionStrobeColors = new Color[lightCount];
        // Timings need float precision on long maps; allocate tween scratch only when a pooled ribbon changes physical width.
        textureColors = new Color[lightCount * rows];
        timelineStates = new LightColorEventStateData[lightCount];
        timelineTweens = new LightColorTween[lightCount];
        for (var light = 0; light < lightCount; light++)
            timelineTweens[light] = new LightColorTween();
        texture = new Texture2D(lightCount, rows, rows == 4 ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat, false, true)
        {
            name = "GLS Light Transition Preview",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
    }

    public void Dispose()
    {
        ReleaseTexture();
        sourceColors = Array.Empty<Color>();
        sourceStrobeColors = Array.Empty<Color>();
        transitionColors = Array.Empty<Color>();
        transitionStrobeColors = Array.Empty<Color>();
        textureColors = Array.Empty<Color>();
        // Retired ribbon controllers must release linked state references instead of retaining a previous map through their scratch buffers.
        timelineStates = Array.Empty<LightColorEventStateData>();
        timelineTweens = Array.Empty<LightColorTween>();
    }

    private void ReleaseTexture()
    {
        if (texture == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(texture);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }

        texture = null;
    }
}
