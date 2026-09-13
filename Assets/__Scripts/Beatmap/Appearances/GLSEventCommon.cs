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

    // ColorNodeTwoColumnLayout splits the color face into a left easing column and a right strobe column:
    // the top-left transition icon keeps its band, strobeEasing sits middle-right, strobeColorEasing bottom-left.
    public const float ColorEasingIconHeight = 0.20f;
    public const float ColorStrobeIconHeight = -0.10f;
    public const float ColorTertiaryIconHeight = -0.17f;
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
    private const string ColorEasingAbbrevOffsetTag = "<voffset=0.1em>";
    private const string ColorStrobeValueOffsetTag = "<voffset=0.55em>";
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
        return ApplyBrightness(GetBaseColor(evt, boost, eventAppearance), evt.Brightness, eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        // Strobe corners use their own brightness, falling back to the main event color when no override exists.
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return ApplyBrightness(color, evt.StrobeBrightness, eventAppearance);
    }

    // Keep the strobe-band predicate tied to actual strobe timing, not its independently configurable dark brightness.
    public static bool IsStrobing(BaseLightColorBase evt)
        => evt.Frequency > 0 
            || (evt.ChromaStrobeInterval is { } interval && interval > 0f);

    // Keep the existing GLS dimness curve shared by main and strobe sections.
    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

    private static Color GetBaseColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
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

        sb.Append("</size></voffset>");
        sb.Append("</line-height>");
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

    // Render each transition ribbon forward from its preceding matching-filter node to the transition target.
    public static void UpdateColorTransitionRibbon(
        LightGradientController controller,
        BaseLightColorBase source,
        EventAppearanceSO eventAppearance,
        Func<float, bool> isBoostAt)
    {
        if (!TryGetFollowingColorTransition(source, out var transition, out var followingEvent))
        {
            // LogRibbonDiagnostic(source, followingEvent, null, null);
            controller.SetVisible(false);
            return;
        }

        var startColor = GetColor(source, isBoostAt(source.JsonTime), eventAppearance);
        var endColor = GetColor(transition, isBoostAt(transition.JsonTime), eventAppearance);
        // LogRibbonDiagnostic(source, followingEvent, startColor, endColor);
        // RibbonGradientUsesColorEasingOverIntervalEasing: the ribbon follows the color track's
        // customData.colorEasing override rather than the interval's own transition curve.
        var gradient = new ChromaLightGradient(
            startColor,
            endColor,
            transition.SongBpmTime - source.SongBpmTime,
            Easing.InternalNameForID(transition.ChromaColorEasing ?? transition.Easing));
        controller.SetVisible(true);
        // RibbonGradientUsesAheadNodeHsvEasingType: the ahead node owns the transition's color space,
        // so its customData.easingType picks the ribbon's angular HSV branch.
        controller.UpdateGradientData(gradient, BasicEventColorLerp.FromGlsEasingType(transition.CustomLerpType));
        controller.UpdateDuration(gradient.Duration);
    }

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
        if (groupCache.IsEmpty)
        {
            colorTransitionCaches.Remove(group.ID);
        }
    }

    // Expose the matched transition endpoint so inner pooling can retain an offscreen ribbon source.
    public static bool TryGetColorTransitionEndTime(BaseLightColorBase source, out float endTime)
    {
        endTime = 0f;
        if (!TryGetFollowingColorTransition(source, out var transition, out _))
            return false;

        endTime = transition.SongBpmTime;
        return true;
    }

    // GLSEasingTypeRibbonInputTest: a hovered ribbon is identified by the physical hit landing on a
    // LightGradientController child of the resolved container, matching the Basic Event ribbon check.
    public static bool IsColorTransitionRibbonHit(ObjectContainer container) =>
        container != null
        && BeatmapRaycastCache.FirstHit != null
        && BeatmapRaycastCache.FirstHit.GetComponentInParent<LightGradientController>() is { } ribbon
        && ribbon.transform.IsChildOf(container.transform);

    // GLSEasingTypeRibbonInputTest: ribbon chords mutate the transition's ahead node, which owns the
    // interval's color easing, strobe easings, and easingType; the ribbon's source node is only the anchor.
    public static bool TryGetColorTransitionTarget(
        ObjectContainer container,
        BaseLightColorBase source,
        out BaseLightColorBase transition)
    {
        transition = null;
        return source != null
            && IsColorTransitionRibbonHit(container)
            && TryGetFollowingColorTransition(source, out transition, out _);
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
        colorTransitionIntervals.GetSourcesAt(boundary, colorTransitionQueryResults);
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
        colorTransitionIntervals.GetSourcesAt(boundary, sources);
        for (var sourceIndex = sources.Count - 1; sourceIndex >= 0; sourceIndex--)
        {
            var source = sources[sourceIndex];
            if (!ReferenceEquals(source.EventBoxGroupData, group) || !source.HasMatchingTrack(trackFilter))
            {
                sources.Remove(source);
            }
        }
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
            return;
        }

        colorTransitionCaches.Clear();
        colorTransitionIntervals.Clear();
        foreach (var group in map.LightColorEventBoxGroups)
        {
            if (!colorTransitionCaches.TryGetValue(group.ID, out var groupCache))
            {
                groupCache = new ColorTransitionGroupCache();
                colorTransitionCaches.Add(group.ID, groupCache);
            }

            groupCache.AddGroupWithoutRewiring(group);
        }

        foreach (var groupCache in colorTransitionCaches.Values)
        {
            groupCache.RebuildAllTransitions();
        }

        cachedColorTransitionMap = map;
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
        // Resolve an event's filter timeline without scanning its sibling filters during ribbon rendering.
        private readonly Dictionary<BaseLightColorBase, ColorFilterSequence> sequenceByEvent = new();
        // A cache miss may occur during repeated appearance refreshes, so compare equivalent identities only once per requested clone.
        private readonly HashSet<BaseLightColorBase> identityMissDiagnosticSources = new();

        // Drop an ID cache once it contains no event identities, even if an empty authored box remains.
        public bool IsEmpty => sequenceByEvent.Count == 0;

        public void AddGroupWithoutRewiring(BaseLightColorEventBoxGroup group)
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

        public bool TryGetFollowingEvent(BaseLightColorBase source, out BaseLightColorBase followingEvent)
        {
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
