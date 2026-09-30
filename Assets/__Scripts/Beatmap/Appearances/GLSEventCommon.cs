using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventCommon
{
    public const float RotationLayoutAdjustment = 1f / 30f;
    public const float RotationColumnHorizontalOffset = 0.25f - RotationLayoutAdjustment;
    public const float TransformEasingIconHeight = 0.29f - RotationLayoutAdjustment;
    public const float RotationDirectionIconHeight =
        (0.273333f - RotationLayoutAdjustment) -
        ((Beatmap.Containers.GLSEventIconView.RotationDirectionIconSize -
          Beatmap.Containers.GLSEventIconView.PreviousRotationDirectionIconSize) * 0.5f);

    public const float TransformTextVerticalOffset = -0.073232f;

    private const float TransformTextFaceWidth = 1.2f;
    private const float TransformTextColumnWidth = 0.3f;
    private const string TransformLabelSizeTag = "<size=58.8%>";
    private const string TransformLabelOffsetTag = "<voffset=0.585em>";
    private const string TransformLoopOffsetTag = "<voffset=0.635em>";
    private const string TransformValueTag = "<margin=0%><size=100%><voffset=-0.285em><align=center>";

    public const float ColorFaceVerticalOffset = -53f / 600f;
    public const float ColorEasingIconHeight = 0.20f + ColorFaceVerticalOffset - (1f / 10f);
    public const float ColorStrobeIconHeight = -0.10f + ColorFaceVerticalOffset + (1f / 15f) - (1f / 25f) + (1f / 50f);
    public const float ColorTertiaryIconHeight = -0.17f + ColorFaceVerticalOffset + (1f / 15f) - (1f / 10f) + (1f / 25f);
    private const float ColorTextColumnCenter =
        Beatmap.Containers.GLSEventIconView.StateIconHorizontalPosition;
    private const float ColorTextEmScale = 0.36f;
    private const string ColorLineHeightTag = "<line-height=55%>";
    private const string ColorBrightnessOffsetTag = "<voffset=0.2em>";
    private const string ColorStrobeValueOffsetTag = "<voffset=0.355556em>";
    private const string ColorStrobeRateOffsetTag = "<voffset=0.1em>";
    private const string ColorLabelSizeTag = "<size=52%>";
    private const string ColorStrobeSizeTag = "<size=66%>";
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;
    // Partition color-transition timelines by GLS group ID so unrelated light groups never share cache work.
    private static readonly Dictionary<int, ColorTransitionGroupCache> colorTransitionCaches = new();
    // Index only sources with active transition intervals so viewport retention never scans all color sequences.
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorTransitionIntervals = new();
    private static readonly List<BaseLightColorBase> colorTransitionQueryResults = new();
    private static readonly Comparer<BaseLightColorBase> colorEventComparer =
        Comparer<BaseLightColorBase>.Create(CompareColorEvents);
    private static BaseDifficulty cachedColorTransitionMap;
    private static readonly Dictionary<int, int> colorLightCounts = new();
    private static readonly HashSet<int> dirtyColorTimelines = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorOutgoingIntervals = new();
    private static readonly TransitionIntervalIndex<BaseLightColorBase> colorInnerIntervals = new();
    // Mutations aggregate into one changed-node set per batch so the collection refreshes
    // only the ribbons that rewired instead of every loaded GLS group.
    private static readonly HashSet<BaseLightColorBase> pendingColorRefreshNodes = new();
    private static readonly HashSet<int> mutatedColorGroupIds = new();
    private static readonly HashSet<int> emptiedColorGroupIds = new();
    private static bool colorRefreshCoversAll;

    public static Color GetColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return GetNodePreviewColor(GetLightColor(evt, boost, eventAppearance), eventAppearance);
    }

    public static Color GetStrobeColor(BaseLightColorBase evt, bool boost, EventAppearanceSO eventAppearance)
    {
        return GetNodePreviewColor(GetLightStrobeColor(evt, boost, eventAppearance), eventAppearance);
    }

    public static Color GetLightColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance) =>
        BasicEventColorLerp.ApplyBrightness(
            GetBaseColor(evt, boost, eventAppearance),
            evt.Brightness);

    public static Color GetLightStrobeColor(
        BaseLightColorBase evt,
        bool boost,
        EventAppearanceSO eventAppearance)
    {
        var color = evt.StrobeColor ?? GetBaseColor(evt, boost, eventAppearance);
        return BasicEventColorLerp.ApplyBrightness(color, evt.StrobeBrightness);
    }

    // Computes filter, distribution, and color-distribution results once per appearance refresh instead of per rendered pixel.
    public static bool PopulateColorTransitionEndpoint(
        BaseLightColorBase evt,
        int lightCount,
        bool boost,
        EventAppearanceSO eventAppearance,
        Color[] mainColors,
        Color[] strobeColors)
    {
        if (lightCount <= 0
            || mainColors == null
            || strobeColors == null
            || mainColors.Length < lightCount
            || strobeColors.Length < lightCount
            || evt.EventBoxData is not BaseLightColorEventBox box)
        {
            return false;
        }

        for (var lightIndex = 0; lightIndex < lightCount; lightIndex++)
        {
            mainColors[lightIndex] = Color.black;
            strobeColors[lightIndex] = Color.black;
        }

        var hasVariation = HasPerLightColorVariation(box, evt);

        var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, lightCount);
        if (indexFilter == null)
        {
            return hasVariation;
        }

        var baseColor = GetBaseColor(evt, boost, eventAppearance);
        var distributionCount = DistributionHelper.GetDistributionCount(indexFilter);
        var affectsFirst = box.BrightnessAffectFirst == 1
            || box.Events.Length == 0
            || !ReferenceEquals(box.Events[0], evt);

        var chunkProgressDenominator = (float)Mathf.Max(indexFilter.VisibleCount - 1, 1);
        var lightProgressDenominator = (float)Mathf.Max(indexFilter.AffectedLightCount - 1, 1);
        foreach (var entry in indexFilter)
        {
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
            var distributedColor = GLSColorDistribution.ApplyNormal(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            var distributedStrobeColor = GLSColorDistribution.ApplyStrobe(
                baseColor, box, evt, distributionProgress, affectedLightProgress);
            distributedColor.a *= evt.Brightness + brightnessOffset;
            distributedStrobeColor.a *= evt.StrobeBrightness;
            mainColors[entry.Element] = distributedColor;
            strobeColors[entry.Element] = distributedStrobeColor;
        }

        return hasVariation;
    }

    public static bool HasColorTransitionDistribution(BaseLightColorBase evt, int lightCount)
    {
        if (lightCount <= 0 || evt?.EventBoxData is not BaseLightColorEventBox box)
        {
            return false;
        }

        if (HasColorDistributionOrBrightnessDistribution(box, evt))
        {
            return true;
        }

        var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, lightCount);
        return indexFilter != null && indexFilter.AffectedLightCount < lightCount;
    }

    private static bool HasPerLightColorVariation(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) =>
        box.IndexFilter.Chunks != 0 || HasColorDistributionOrBrightnessDistribution(box, evt);

    private static bool HasColorDistributionOrBrightnessDistribution(
        BaseLightColorEventBox box,
        BaseLightColorBase evt) 
        => !Mathf.Approximately(box.BrightnessDistribution, 0f)
            || box.ParsedColorDistributions.Count > 0
            || box.ParsedStrobeColorDistributions.Count > 0
            || evt.ParsedColorDistributions.Count > 0
            || evt.ParsedStrobeColorDistributions.Count > 0;

    public static bool IsStrobing(BaseLightColorBase evt)
        => evt.Frequency > 0 
            || (evt.ChromaStrobeInterval is { } interval && interval > 0f);

    // ZeroBrightnessStrobeKeepsFrequencyIntoTransition: timing remains authored even while both phases are black, so a later transition retains the source pulse clock.
    public static float GetStrobeFrequency(BaseLightColorBase evt)
    {
        return evt.ChromaStrobeInterval is { } interval && interval > 0f
            ? 1f / interval
            : evt.Frequency;
    }

    // Scale by local BPM so tempo changes preserve the authored cycle length.
    internal static float GetStrobeFrequencyScale(BaseDifficulty map, float songBpmTime)
    {
        var baseBpm = map?.SongBpm;
        var localBpm = map?.BpmAtSongBpmTime(Mathf.Max(songBpmTime, 0f));
        return baseBpm is > 0f && localBpm is > 0f
            ? localBpm.Value / baseBpm.Value
            : 1f;
    }

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
        // Avoid applying renderer alpha twice while retaining the opaque node-color endpoint.
        rendererColor.a = 1f;
        return ApplyBrightness(rendererColor, effectiveBrightness, eventAppearance);
    }

    private static Color ApplyBrightness(Color color, float brightness, EventAppearanceSO eventAppearance)
    {
        var clampedOffColor = Color.Lerp(eventAppearance.OffColor, color, DimmedColorFraction);
        return Color.Lerp(clampedOffColor, color, brightness);
    }

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

    public static string GetColorInfo(BaseLightColorBase evt)
    {
        var sb = new StringBuilder(192);
        sb.Append(ColorLineHeightTag);
        sb.Append(ColorBrightnessOffsetTag);
        sb.Append("<align=center>");
        sb.Append((evt.Brightness * 100f).ToString(CultureInfo.InvariantCulture));
        sb.Append("</voffset>");
        sb.AppendLine();

        AppendEmColumn(sb, -ColorTextColumnCenter);
        sb.Append(ColorLabelSizeTag);
        sb.Append(' ');
        sb.Append("</size>");
        sb.AppendLine();

        sb.Append(ColorStrobeValueOffsetTag);
        AppendEmColumn(sb, ColorTextColumnCenter);
        sb.Append(ColorStrobeSizeTag);
        if (evt.StrobeBrightness > 0f || IsStrobing(evt))
        {
            sb.Append((evt.StrobeBrightness * 100f).ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            sb.Append(' ');
        }

        sb.Append("</size></voffset>");
        sb.AppendLine();

        AppendEmColumn(sb, ColorTextColumnCenter);
        sb.AppendLine(" ");

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
            sb.Append(' ');
        }

        sb.Append("</size></voffset>");
        sb.AppendLine();

        AppendEmColumn(sb, -ColorTextColumnCenter);
        sb.Append(ColorLabelSizeTag);
        sb.Append(' ');
        sb.Append("</size>");
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
        return GetTransformInfo(
            GLSEventTranslationCommand.IsYeet(evt.Translation)
                ? "YEET"
                : (evt.Translation * 100f).ToString(CultureInfo.InvariantCulture),
            Easing.IDToShortName.GetValueOrDefault(evt.EaseType));
    }

    private static string GetTransformInfo(string value, string easing, string loop = null)
    {
        var sb = new StringBuilder(192);
        sb.Append("<line-height=55%>");
        sb.Append(TransformLabelSizeTag);
        if (loop != null)
        {
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
            sb.Append(TransformLoopOffsetTag);
            sb.Append(" </voffset>");
            sb.AppendLine();
            sb.Append(TransformLabelOffsetTag);
            sb.Append("<align=center>");
        }

        sb.Append(easing);
        sb.AppendLine("</voffset></size>");
        sb.Append(TransformValueTag);
        sb.Append(value);
        sb.Append("</voffset></size></line-height>");
        return sb.ToString();
    }

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
        return GetTransformInfo(
            (evt.Value * 100f).ToString(CultureInfo.InvariantCulture),
            Easing.IDToShortName.GetValueOrDefault(evt.Easing));
    }

    public static void ResetColorTransitionLightCounts()
    {
        colorLightCounts.Clear();
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        // Every timeline rebuilds after a reset, so no per-node changed set exists for the next refresh.
        colorRefreshCoversAll = true;
        foreach (var entry in colorTransitionCaches)
        {
            entry.Value.InvalidateTimeline();
            dirtyColorTimelines.Add(entry.Key);
        }
    }

    public static void SetColorTransitionLightCount(int groupId, int lightCount)
    {
        var wasUnregistered = !colorLightCounts.TryGetValue(groupId, out var previous);
        if (lightCount > 0 && (wasUnregistered || previous != lightCount))
        {
            colorLightCounts[groupId] = lightCount;
            dirtyColorTimelines.Add(groupId);
            mutatedColorGroupIds.Add(groupId);
            // A first-time registration retires this ID's duplicated fallback index once; known IDs
            // answer through the physical timeline, and EnsureLegacyTimeline rebuilds the fallback
            // from the current ordered groups if ResetColorTransitionLightCounts downgrades it again.
            // LateRegisteredLightCountLinksPhysicallyOverlappingFilters / ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
            if (wasUnregistered && colorTransitionCaches.TryGetValue(groupId, out var upgradedCache))
            {
                upgradedCache.DropLegacyTimeline();
            }
        }
    }

    // A width-0 caller (a preview that cannot know the environment's light count) resolves the count
    // already registered for the node's group ID; an explicit positive count always wins, and an
    // unregistered group keeps 0 so its callers stay on the legacy scalar path.
    // KnownCountZeroWidthRibbonUsesPhysicalTimeline
    private static int ResolveColorTransitionLightCount(BaseLightColorBase node, int lightCount) =>
        lightCount > 0
            ? lightCount
            : node.EventBoxGroupData != null
                && colorLightCounts.TryGetValue(node.EventBoxGroupData.ID, out var registered)
                ? registered
                : 0;

    public static GLSColorTimeline GetColorTimeline(BaseLightColorBase node, int lightCount)
    {
        var song = BeatSaberSongContainer.Instance;
        if (song == null || song.Map == null || node.EventBoxGroupData == null)
            return null;
        lightCount = ResolveColorTransitionLightCount(node, lightCount);
        if (lightCount <= 0)
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
        if (!Settings.Instance.VisualizeGLSLightTransitions)
        {
            controller.SetVisible(false);
            return;
        }

        // Each light can have a different next event and distributed clock; a width-0 caller still
        // resolves the count registered for this group ID instead of dropping to the scalar path.
        // KnownCountZeroWidthRibbonUsesPhysicalTimeline
        var resolvedLightCount = ResolveColorTransitionLightCount(source, lightCount);
        if (resolvedLightCount > 0)
        {
            var timeline = GetColorTimeline(source, resolvedLightCount);
            controller.UpdateColorTimeline(
                timeline, 
                source,
                false,
                eventAppearance,
                isBoostAt,
                aggregateSameTimeBoxes);
            return;
        }
        if (!TryGetFollowingColorTransition(source, out var transition, out var followingEvent))
        {
            controller.SetVisible(false);
            return;
        }

        var sourceBoost = isBoostAt(source.JsonTime);
        var transitionBoost = isBoostAt(transition.JsonTime);
        var startColor = GetLightColor(source, sourceBoost, eventAppearance);
        var endColor = GetLightColor(transition, transitionBoost, eventAppearance);

        var gradient = new ChromaLightGradient(
            startColor,
            endColor,
            transition.SongBpmTime - source.SongBpmTime,
            Easing.InternalNameForID(transition.ChromaColorEasing ?? transition.Easing));

        var strobeScale = GetStrobeFrequencyScale(
            BeatSaberSongContainer.Instance != null ? BeatSaberSongContainer.Instance.Map : null,
            source.SongBpmTime);
        var startFrequency = GetStrobeFrequency(source) * strobeScale;
        var endFrequency = GetStrobeFrequency(transition) * strobeScale;

        controller.UpdateColorTransitionDistribution(
            source,
            transition,
            lightCount,
            sourceBoost,
            transitionBoost,
            eventAppearance);

        var strobeGradient = startFrequency > 0f || endFrequency > 0f
            ? new ChromaLightGradient(
                GetLightStrobeColor(source, sourceBoost, eventAppearance),
                GetLightStrobeColor(transition, transitionBoost, eventAppearance),
                gradient.Duration)
            : null;
        controller.SetVisible(true);

        controller.UpdateGradientData(
            gradient,
            transition.CustomLerpType,
            strobeGradient,
            null,
            startFrequency,
            endFrequency,
            transition.StrobeFade == 1);
        controller.UpdateDuration(gradient.Duration);
    }

    public static void UpdateIncomingColorTransitionRibbon(
        LightGradientController controller, BaseLightColorBase target, EventAppearanceSO appearance,
        Func<float, bool> isBoostAt, int lightCount, bool aggregateSameTimeBoxes = false)
    {
        // HiddenGlsLightTransitionsHideBothRibbonDirections keeps both ribbon directions hidden when visualization is off.
        if (!Settings.Instance.VisualizeGLSLightTransitions)
        {
            controller.SetVisible(false);
            return;
        }

        controller.UpdateColorTimeline(
            GetColorTimeline(target, lightCount), target, true, appearance, isBoostAt, aggregateSameTimeBoxes);
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
        dirtyColorTimelines.Add(group.ID);
        mutatedColorGroupIds.Add(group.ID);
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
        dirtyColorTimelines.Add(group.ID);
        mutatedColorGroupIds.Add(group.ID);
        // GLS edits clone+replace the same ID, so an emptied cache tears down only at the collect
        // boundary; committing here would rebuild every light's states on the very next add.
        if (groupCache.IsEmpty)
        {
            emptiedColorGroupIds.Add(group.ID);
        }
    }

    // Emptied caches commit at the collect boundary; a cache repopulated by a replacement add skips teardown.
    private static void CommitEmptiedColorCaches()
    {
        foreach (var groupId in emptiedColorGroupIds)
        {
            if (colorTransitionCaches.TryGetValue(groupId, out var cache) && cache.IsEmpty)
            {
                cache.ClearTimelineIntervals();
                colorTransitionCaches.Remove(groupId);
            }
        }

        emptiedColorGroupIds.Clear();
    }

    // Aggregates the per-mutation changed-node sets so one batched refresh touches only the ribbons
    // that rewired. Returns false when a mutation rebuilt (or must rebuild) a timeline or hit the
    // legacy fallback path — callers then keep the previous full refresh.
    public static bool TryCollectChangedColorTransitions(
        HashSet<BaseLightColorBase> changedNodes,
        Dictionary<BaseEventBoxGroup, HashSet<float>> changedAggregates)
    {
        var incremental = !colorRefreshCoversAll;
        foreach (var groupId in mutatedColorGroupIds)
        {
            if (!colorTransitionCaches.TryGetValue(groupId, out var cache)
                || !cache.HasIncrementalTimeline
                || cache.ConsumeRebuiltFlag())
            {
                incremental = false;
                continue;
            }

            cache.CollectChangedNodes(pendingColorRefreshNodes);
        }

        CommitEmptiedColorCaches();
        mutatedColorGroupIds.Clear();
        colorRefreshCoversAll = false;

        if (!incremental)
        {
            pendingColorRefreshNodes.Clear();
            return false;
        }

        changedNodes.UnionWith(pendingColorRefreshNodes);
        pendingColorRefreshNodes.Clear();
        foreach (var node in changedNodes)
        {
            // Same-time aggregate ribbons key off the owning group's relative beat, not node identity.
            var owner = node != null ? node.EventBoxGroupData : null;
            if (owner == null)
            {
                continue;
            }

            if (!changedAggregates.TryGetValue(owner, out var times))
            {
                changedAggregates[owner] = times = new HashSet<float>();
            }

            times.Add(node.RelativeJsonTime);
        }

        return true;
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

    public static bool TryGetColorRibbonBounds(BaseLightColorBase node, int lightCount, out float start, out float end)
    {
        start = node.SongBpmTime;
        end = start;
        var timeline = GetColorTimeline(node, lightCount);
        return timeline != null && colorTransitionCaches[node.EventBoxGroupData.ID].TryGetInnerBounds(node, out start, out end);
    }

    private static bool IsColorTransitionContainer(ObjectContainer container) =>
        container != null
        && (container is GLSEventContainer { EventData: BaseLightColorBase }
            or GLSGroupContainer { PreviewEventData: BaseLightColorBase });

    public static bool IsColorTransitionRibbonHit(ObjectContainer container) =>
        container != null
        && IsColorTransitionRibbonHit()
        && ReferenceEquals(classifiedRibbonOwner, container);

    // pooled containers can represent a different event on the next hit
    private static GameObject classifiedRibbonHit;
    private static ObjectContainer classifiedRibbonOwner;
    private static LightGradientController classifiedRibbonController;

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

        // Recheck the event type because the pooled owner may have been rebound since the hierarchy was cached.
        return IsColorTransitionContainer(classifiedRibbonOwner);
    }

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
                // Match hover selection to the shader's per-light strip and active interval.
                if (BeatmapRaycastCache.FirstHitPoint is { } point && classifiedRibbonController.ColorTimelineDuration > 0f)
                {
                    var uv = classifiedRibbonController.GetHitUv(point);
                    var light = count - 1 - Mathf.Clamp(Mathf.FloorToInt(uv.y * count), 0, count - 1);
                    var time = classifiedRibbonController.ColorTimelineStart + (uv.x * classifiedRibbonController.ColorTimelineDuration);
                    var incoming = classifiedRibbonController.IsIncomingColorTransition;
                    LightColorEventStateData state;
                    var found = incoming
                        ? classifiedRibbonController.AggregatesSameTimeBoxes
                            ? timeline.TryGetIncomingAtGroupTime(source, light, out state)
                            : timeline.TryGetIncomingSegment(source, light, out state)
                        : classifiedRibbonController.AggregatesSameTimeBoxes
                            ? timeline.TryGetOutgoingAtGroupTime(source, light, out state)
                            : timeline.TryGetOutgoing(source, light, out state);
                    if (!found)
                    {
                        return false;
                    }
                    if (incoming)
                    {
                        // InterpolatedFirstColorEventKeepsLightsOffUntilItsBeat: the incoming APIs
                        // already reject sentinel-owned spans, so hover sees only real predecessors
                        // and keeps the same dedupe and hit-interval filtering as before.
                        if (classifiedRibbonController.AggregatesSameTimeBoxes
                                || ReferenceEquals(
                                    state.Base.EventBoxGroupData, source.EventBoxGroupData)
                            || time > state.EndTime
                            || time < Mathf.Max(state.StartTime, timeline.HeadBound))
                        {
                            return false;
                        }
                        transition = state.Next != null ? state.Next.Base : null;
                        return transition != null;
                    }
                    if (state.EndTime == float.MaxValue || time < state.StartTime || time > state.EndTime)
                    {
                        return false;
                    }
                    transition = state.Next.Base;
                    return true;
                }

                if (classifiedRibbonController.IsIncomingColorTransition)
                {
                    transition = source;
                    return true;
                }

                for (var light = 0; light < count; light++)
                {
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

    // A masked strip is still a ribbon hit, not a node-body fallback. Null targets = no-ops
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
        ISet<BaseLightColorEventBoxGroup> sourceGroups) =>
        GetColorTransitionSourceGroupsAt(boundary, trackFilter, null, sourceGroups);

    public static void GetColorTransitionSourceGroupsAt(
        float boundary,
        string trackFilter,
        ISet<int> activeGroupIds,
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
        GetColorRibbonSourcesAt(boundary, colorTransitionQueryResults, inner: false);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var sourceGroup = colorTransitionQueryResults[sourceIndex].EventBoxGroupData as BaseLightColorEventBoxGroup;
            if (sourceGroup != null
                && (activeGroupIds == null || activeGroupIds.Contains(sourceGroup.ID))
                && sourceGroup.HasMatchingTrack(trackFilter))
            {
                sourceGroups.Add(sourceGroup);
            }
        }
    }

    public static void GetColorTransitionSourcesAt(
        float boundary,
        string trackFilter,
        ISet<BaseGLSEvent> sources) =>
        GetColorTransitionSourcesAt(boundary, trackFilter, null, sources);

    public static void GetColorTransitionSourcesAt(
        float boundary,
        string trackFilter,
        ISet<int> activeGroupIds,
        ISet<BaseGLSEvent> sources)
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
        GetColorRibbonSourcesAt(boundary, colorTransitionQueryResults, inner: true);
        for (var sourceIndex = 0; sourceIndex < colorTransitionQueryResults.Count; sourceIndex++)
        {
            var source = colorTransitionQueryResults[sourceIndex];
            if ((activeGroupIds == null || activeGroupIds.Contains(source.EventBoxGroupData.ID))
                && source.HasMatchingTrack(trackFilter))
            {
                sources.Add(source);
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
        GetColorRibbonSourcesAt(boundary, sources, inner: true);
        for (var sourceIndex = sources.Count - 1; sourceIndex >= 0; sourceIndex--)
        {
            var source = sources[sourceIndex];
            if (!ReferenceEquals(source.EventBoxGroupData, group) || !source.HasMatchingTrack(trackFilter))
            {
                sources.RemoveAt(sourceIndex);
            }
        }
    }

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
            RefreshDirtyColorTimelines(map);
            return;
        }

        colorTransitionCaches.Clear();
        colorTransitionIntervals.Clear();
        colorOutgoingIntervals.Clear();
        colorInnerIntervals.Clear();
        dirtyColorTimelines.Clear();
        pendingColorRefreshNodes.Clear();
        mutatedColorGroupIds.Clear();
        emptiedColorGroupIds.Clear();
        colorRefreshCoversAll = false;
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
            // Registered IDs own only the physical timeline; the duplicated filter-sequence index
            // stays deferred (legacyDirty) until ResetColorTransitionLightCounts asks for it again.
            // MixedKnownAndFallbackIdsQueryTogetherWithoutCrossIdLeak / ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
            if (colorLightCounts.TryGetValue(entry.Key, out var lightCount))
                entry.Value.GetTimeline(map, lightCount);
            else
                entry.Value.RebuildAllTransitions();
        }

        cachedColorTransitionMap = map;
    }

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

        public bool IsEmpty => groups.Count == 0;

        // False while a mutation must rebuild the timeline (first build, light-count change) or only
        // the legacy fallback exists; such edits cannot report a scoped changed set.
        public bool HasIncrementalTimeline => timeline != null && !timelineDirty;

        // A rebuild replaces every segment, so the batch that first observes it falls back to a full refresh.
        private bool rebuiltSinceCollect;
        public bool ConsumeRebuiltFlag()
        {
            var wasRebuilt = rebuiltSinceCollect;
            rebuiltSinceCollect = false;
            return wasRebuilt;
        }

        // Consumes the timeline's accumulated changed set once: pendingNodes keeps it for the interval
        // refresh inside GetTimeline while target reports it to this batch's ribbon refresh.
        public void CollectChangedNodes(ISet<BaseLightColorBase> target)
        {
            target.UnionWith(pendingNodes);
            if (timeline == null)
            {
                return;
            }

            timeline.EnsureUpdated();
            pendingNodes.UnionWith(timeline.ChangedNodes);
            target.UnionWith(timeline.ChangedNodes);
            timeline.ClearChangedNodes();
        }

        public GLSColorTimeline GetTimeline(BaseDifficulty map, int lightCount)
        {
            if (timelineDirty || timeline == null || timeline.LightCount != lightCount)
            {
                // Replacing a live incremental timeline invalidates any scoped changed set; the first
                // build has nothing loaded to rewire, so it stays incremental.
                rebuiltSinceCollect = timeline != null;
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
            // Changed-node consumption moved here from each Add/Remove so a remove+add replacement
            // merges into one interval refresh and one ribbon refresh set.
            timeline.EnsureUpdated();
            pendingNodes.UnionWith(timeline.ChangedNodes);
            pendingColorRefreshNodes.UnionWith(timeline.ChangedNodes);
            timeline.ClearChangedNodes();
            RefreshChangedIntervals();
            return timeline;
        }

        // Only called during full rebuilds.
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
                    if (!timeline.TryGetIncomingSegment(node, light, out var previous))
                    {
                        continue;
                    }

                    // InterpolatedFirstColorEventKeepsLightsOffUntilItsBeat: sentinel-owned
                    // spans are rejected by TryGetIncomingSegment, so only a cross-group
                    // predecessor still retains the target in the inner ribbon index.
                    if (!ReferenceEquals(previous.Base.EventBoxGroupData, node.EventBoxGroupData))
                    {
                        IncludeInnerBounds(
                            node,
                            Mathf.Max(previous.StartTime, timeline.HeadBound),
                            previous.EndTime);
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
            // A registered ID never materializes the duplicated fallback sequences; legacyDirty lets
            // EnsureLegacyTimeline build them from the ordered groups if the count is later reset.
            // ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
            if (colorLightCounts.ContainsKey(group.ID))
            {
                legacyDirty = true;
                return;
            }
            AppendLegacyGroup(group);
        }

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
            if (!groups.TryAdd(group, nextGroupOrder++))
                return;
            if (timeline != null)
            {
                if (!timelineDirty)
                {
                    timeline.AddGroup(group);
                }
                legacyDirty = true;
                return;
            }
            // A registered ID whose timeline has not materialized yet builds it from the ordered
            // groups; only unregistered IDs pay for the duplicated legacy sequences.
            // ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
            if (colorLightCounts.ContainsKey(group.ID))
            {
                legacyDirty = true;
                return;
            }
            AddLegacyGroup(group);
        }

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
            // Remove it + its affected neighbors, other lights unaffected
            if (!groups.Remove(group))
                return;
            if (timeline != null)
            {
                if (!timelineDirty)
                {
                    timeline.RemoveGroup(group);
                }
                legacyDirty = true;
                return;
            }
            // Registered IDs keep no legacy sequences to mutate; the pending physical timeline
            // rebuild reads the ordered groups and a later count reset rebuilds the fallback.
            // ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
            if (colorLightCounts.ContainsKey(group.ID))
            {
                legacyDirty = true;
                return;
            }
            RemoveLegacyGroup(group);
        }

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

        // First registration of a light count drops this ID's duplicated fallback index once; known
        // IDs never reach TryGetFollowingEvent, and a later ResetColorTransitionLightCounts rebuilds
        // the fallback from the current ordered groups because legacyDirty stays set.
        // LateRegisteredLightCountLinksPhysicallyOverlappingFilters / ResetAfterPhysicalEditRebuildsFallbackFromCurrentGroups
        public void DropLegacyTimeline()
        {
            foreach (var sequence in sequences)
            {
                foreach (var node in sequence.Events)
                {
                    colorTransitionIntervals.Remove(node);
                }
            }
            sequences.Clear();
            sequenceByEvent.Clear();
            legacyDirty = true;
        }

        // Rebuild filter-only fallback data only if that fallback is actually requested after a physical-light edit.
        // Not called on any hot path, only load
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
            EnsureLegacyTimeline();
            if (sequenceByEvent.TryGetValue(source, out var sequence)
                && sequence.FollowingEvents.TryGetValue(source, out followingEvent))
            {
                return true;
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
                modifiedSequence.Key.RewireRange(modifiedSequence.Value.Minimum, modifiedSequence.Value.Maximum);
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

            var firstChangedIndex = Events.AsSpan().LowerBoundBy(minimumTime, evt => evt.JsonTime);
            var startIndex = firstChangedIndex > 0
                ? Events.AsSpan().LowerBoundBy(Events[firstChangedIndex - 1].JsonTime, evt => evt.JsonTime)
                : firstChangedIndex;
            var endIndex = Events.AsSpan().UpperBoundBy(maximumTime, evt => evt.JsonTime);
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

// Each pooled ribbon owns a point-sampled endpoint or per-light timeline table.
public sealed class GLSColorTransitionPreview : IDisposable
{
    // Row zero is the source normal color; the remaining endpoint rows are
    // shared by legacy distributions and the first four rows of each tween.
    private const int TransitionNormalRow = 1;
    private const int SourceStrobeRow = 2;
    private const int TransitionStrobeRow = 3;

    private static readonly int lightDistributionTextureId = Shader.PropertyToID("_LightDistributionTex");
    private static readonly int lightDistributionWidthId = Shader.PropertyToID("_LightDistributionWidth");
    private static readonly int useLightDistributionId = Shader.PropertyToID("_UseLightDistribution");
    private static readonly int useLightTimelineId = Shader.PropertyToID("_UseLightTimeline");
    private static readonly int lightTimelineDurationId = Shader.PropertyToID("_LightTimelineDuration");
    // Shared lane coordinates compare absolute event times across owner quads.
    private static readonly int lightTimelineStartId = Shader.PropertyToID("_LightTimelineStart");
    // GreenMonstercatRibbonEdgesMatchSupersampling: shade the outer subpixel
    // fringe using the same quad, without adding a renderer or another draw.
    private static readonly int ribbonEdgePaddingId = Shader.PropertyToID("_RibbonEdgePadding");
    private static readonly int useRibbonPlaneId = Shader.PropertyToID("_UseRibbonPlane");
    private LightColorEventStateData[] timelineStates = Array.Empty<LightColorEventStateData>();
    private LightColorTween[] timelineTweens = Array.Empty<LightColorTween>();
    // RandomizedMonstercatRibbonEdgesMatchSupersampling: reuse one flat range buffer
    // for all columns; delayed neighbors can require more than one cached tween.
    private readonly List<LightColorEventStateData> timelineNeighborStates = new();
    private int[] timelineNeighborOffsets = Array.Empty<int>();

    private Texture2D texture;
    private Color[] sourceColors = Array.Empty<Color>();
    private Color[] sourceStrobeColors = Array.Empty<Color>();
    private Color[] transitionColors = Array.Empty<Color>();
    private Color[] transitionStrobeColors = Array.Empty<Color>();
    private Color[] textureColors = Array.Empty<Color>();
    // Scrubbing rebinds every pooled ribbon with identical data, so snapshot the last uploaded payload
    // and skip the synchronous SetPixels/Apply when a refresh produces the same bytes.
    private Color[] uploadedColors = Array.Empty<Color>();
    private bool uploadDirty = true;
    // Dense filters resolve the same authored endpoint times once per light; memoize the boost query
    // for the duration of one ribbon refresh instead of repeating a binary search per light.
    private readonly Dictionary<float, bool> boostCache = new();
    private Func<float, bool> cachedBoostResolver;
    private Func<float, bool> activeBoostResolver;

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
        properties.SetFloat(useLightTimelineId, 0f);
        // Returning to endpoint-only distribution must clear timeline geometry
        // and its canonical clock, including the undistributed early return.
        properties.SetFloat(ribbonEdgePaddingId, 0f);
        properties.SetFloat(useRibbonPlaneId, 0f);
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
            var textureX = lightCount - lightIndex - 1;
            textureColors[textureX] = sourceColors[lightIndex];
            textureColors[(TransitionNormalRow * lightCount) + textureX] = transitionColors[lightIndex];
            textureColors[(SourceStrobeRow * lightCount) + textureX] = sourceStrobeColors[lightIndex];
            textureColors[(TransitionStrobeRow * lightCount) + textureX] = transitionStrobeColors[lightIndex];
        }

        UploadIfChanged();
        properties.SetTexture(lightDistributionTextureId, texture);
        properties.SetFloat(lightDistributionWidthId, lightCount);
        properties.SetFloat(useLightDistributionId, 1f);
    }

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
        // GLSColorTimelineBoundsTest: empty ribbons need only the state buffer;
        // allocate texture rows and tweens after finding a visible span.
        if (timelineStates.Length != count)
        {
            timelineStates = new LightColorEventStateData[count];
        }
        // Required rows are overwritten below; headers exclude unused capacity.
        for (var light = 0; light < count; light++)
        {
            LightColorEventStateData state;
            var found = incoming
                ? aggregateSameTimeBoxes
                    ? timeline.TryGetIncomingAtGroupTime(owner, light, out state)
                    : timeline.TryGetIncomingSegment(owner, light, out state)
                : aggregateSameTimeBoxes
                    ? timeline.TryGetOutgoingAtGroupTime(owner, light, out state)
                    : timeline.TryGetOutgoing(owner, light, out state);
            if (found && incoming)
            {
                // InterpolatedFirstColorEventKeepsLightsOffUntilItsBeat: sentinel-owned spans are
                // rejected by the incoming APIs, so only real predecessors reach this point.
                // The aggregated outer body draws only its own shared timestamp, so real
                // predecessors own the span forward through their own outgoing ribbons; inner
                // lanes keep the cross-group ownership rule because another group's nodes are
                // not drawn inside the opened group.
                if (aggregateSameTimeBoxes
                    || ReferenceEquals(state.Base.EventBoxGroupData, owner.EventBoxGroupData))
                {
                    found = false;
                }
            }
            else if (found && state.EndTime == float.MaxValue)
            {
                var held = state.UsePrevious ? (LightColorEventStateData)state.Previous : state;
                if (!GLSColorTimeline.IsLit(held))
                {
                    found = false;
                }
            }
            if (!found)
            {
                state = null;
            }
            else
            {
                var segmentStart = Mathf.Max(state.StartTime, timeline.HeadBound);
                var segmentEnd = Mathf.Min(state.EndTime, timeline.TailBound);
                if (segmentEnd <= segmentStart)
                {
                    state = null;
                }
                else
                {
                    start = Mathf.Min(start, segmentStart);
                    end = Mathf.Max(end, segmentEnd);
                }
            }
            timelineStates[light] = state;
        }
        if (!(end > start))
            return false;
        // RandomizedMonstercatRibbonEdgesMatchSupersampling: collect only states
        // overlapping adjacent owned strips, using the timeline's indexed entry
        // point, so refresh work is bounded by the visible neighboring spans.
        timelineNeighborStates.Clear();
        if (timelineNeighborOffsets.Length != count + 1)
            timelineNeighborOffsets = new int[count + 1];
        var maximumNeighborCount = 0;
        for (var light = 0; light < count; light++)
        {
            timelineNeighborOffsets[light] = timelineNeighborStates.Count;
            var lowerOwned = light > 0 ? timelineStates[light - 1] : null;
            var upperOwned = light + 1 < count ? timelineStates[light + 1] : null;
            if (lowerOwned != null || upperOwned != null)
            {
                var neighborStart = lowerOwned != null ? lowerOwned.StartTime : upperOwned.StartTime;
                var neighborEnd = lowerOwned != null ? lowerOwned.EndTime : upperOwned.EndTime;
                if (upperOwned != null)
                {
                    neighborStart = Mathf.Min(neighborStart, upperOwned.StartTime);
                    neighborEnd = Mathf.Max(neighborEnd, upperOwned.EndTime);
                }
                timeline.AppendRibbonNeighborStates(owner, timelineStates[light], light,
                    Mathf.Max(start, neighborStart), Mathf.Min(end, neighborEnd),
                    aggregateSameTimeBoxes, timelineNeighborStates);
            }
            maximumNeighborCount = Mathf.Max(maximumNeighborCount,
                timelineNeighborStates.Count - timelineNeighborOffsets[light]);
        }
        timelineNeighborOffsets[count] = timelineNeighborStates.Count;
        EnsureCapacity(count, 10 + (9 * maximumNeighborCount));
        var boostResolver = BeginBoostMemoization(isBoostAt);
        for (var light = 0; light < count; light++)
        {
            var x = count - light - 1;
            var state = timelineStates[light];
            var neighborOffset = timelineNeighborOffsets[light];
            var neighborCount = timelineNeighborOffsets[light + 1] - neighborOffset;
            // AlternatingMonstercatRibbonEdgesMatchSupersampling: inactive primary
            // columns own no pixels. Their header still exposes same-lane neighbor
            // colors for the shader's single-owner strip boundary blend.
            if (state == null)
            {
                // Inactive endpoint rows retain the existing opaque-black data
                // contract; the invalid time interval alone suppresses emission.
                for (var row = 0; row < 4; row++)
                    textureColors[(row * count) + x] = Color.black;
                for (var row = 4; row < 9; row++)
                    textureColors[(row * count) + x] = default;
                textureColors[(4 * count) + x] = new Color(0f, -1f, 0f, -1f);
                textureColors[(7 * count) + x] = new Color(0f, 0f,
                    neighborCount > 0 ? 0.015625f : 0f, 0f);
            }
            else
            {
                PackTimelineState(timeline, state, appearance, boostResolver,
                    start, count, x, light, 0, neighborCount > 0,
                    owner, aggregateSameTimeBoxes);
            }
            textureColors[(9 * count) + x] = new Color(neighborCount, 0f, 0f, 0f);
            for (var neighbor = 0; neighbor < neighborCount; neighbor++)
            {
                PackTimelineState(timeline, timelineNeighborStates[neighborOffset + neighbor],
                    appearance, boostResolver, start, count, x, light, 10 + (9 * neighbor),
                    false, owner, aggregateSameTimeBoxes);
            }
        }
        UploadIfChanged();
        properties.SetTexture(lightDistributionTextureId, texture);
        properties.SetFloat(lightDistributionWidthId, count);
        properties.SetFloat(useLightDistributionId, 1f);
        properties.SetFloat(useLightTimelineId, 1f);
        properties.SetFloat(lightTimelineDurationId, end - start);
        properties.SetFloat(lightTimelineStartId, start);
        // The vertex fringe needs the unit XY mesh used by timeline ribbons.
        properties.SetFloat(ribbonEdgePaddingId, 1f);
        return true;
    }

    // RandomizedMonstercatRibbonEdgesMatchSupersampling: primary and sorted
    // neighbor records use identical clocks and color encoding, so evaluated
    // boundary colors agree across separately owned ribbon draws.
    private void PackTimelineState(
        GLSColorTimeline timeline, LightColorEventStateData state,
        EventAppearanceSO appearance, Func<float, bool> boostResolver,
        float start, int count, int x, int light, int rowOffset,
        bool hasNeighbors,
        BaseLightColorBase owner, bool aggregateSameTimeBoxes)
    {
        var tween = timelineTweens[light];
        timeline.ConfigureTween(tween, state, appearance, boostResolver);
        // DistributedNodeRibbonJoinDoesNotExposeBackground: adjacent emitting owners meet at
        // the same per-light time, so only true light-to-background ends need temporal AA.
        var startEmits = (tween.StartAlpha > 0f && tween.StartColor.maxColorComponent > 0f)
            || (tween.StartStrobeFrequency > 0f && tween.StartStrobeBrightness > 0f
                && tween.StartStrobeColor.maxColorComponent > 0f);
        var endEmits = (tween.EndAlpha > 0f && tween.EndColor.maxColorComponent > 0f)
            || (tween.EndStrobeFrequency > 0f && tween.EndStrobeBrightness > 0f
                && tween.EndStrobeColor.maxColorComponent > 0f);
        // AlternatingMonstercatRibbonEdgesMatchSupersampling: suppress endpoint
        // fading only when the preceding/following ribbon occupies this lane.
        // A sibling box in another inner lane must leave a background AA edge.
        var joinedStart = startEmits && timeline.IsStateVisibleInRibbonLane(
            (LightColorEventStateData)state.Previous, owner, aggregateSameTimeBoxes);
        var joinedEnd = endEmits && timeline.IsStateVisibleInRibbonLane(
            (LightColorEventStateData)state.Next, owner, aggregateSameTimeBoxes);
        textureColors[(rowOffset * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.StartColor, tween.StartAlpha);
        textureColors[((rowOffset + 1) * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.EndColor, tween.EndAlpha);
        textureColors[((rowOffset + 2) * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.StartStrobeColor, tween.StartStrobeBrightness);
        textureColors[((rowOffset + 3) * count) + x] = BasicEventColorLerp.ApplyBrightness(tween.EndStrobeColor, tween.EndStrobeBrightness);
        var rowEndAlpha = state.EndTime == float.MaxValue ? timeline.TailBound : tween.EndTimeAlpha;
        var rowEndColor = state.EndTime == float.MaxValue ? timeline.TailBound : tween.EndTimeColor;
        textureColors[((rowOffset + 4) * count) + x] = new Color(tween.StartTimeAlpha - start, rowEndAlpha - start,
            tween.StartTimeColor - start, rowEndColor - start);
        textureColors[((rowOffset + 5) * count) + x] = new Color(tween.StartStrobeFrequency, tween.EndStrobeFrequency,
            tween.StartAlpha, tween.EndAlpha);
        textureColors[((rowOffset + 6) * count) + x] = new Color(tween.StartStrobeBrightness, tween.EndStrobeBrightness,
            tween.StartColor.a, tween.EndColor.a);
        // AlternatingStrobeRibbonsBlendColorsAtSharedStripEdges: bank location
        // identifies borrowed colors; metadata needs only join and neighbor bits.
        textureColors[((rowOffset + 7) * count) + x] = new Color(tween.StartStrobeColor.a, tween.EndStrobeColor.a,
            (int)tween.ColorLerpType + (joinedStart ? 0.25f : 0f) + (joinedEnd ? 0.5f : 0f)
                + (hasNeighbors ? 0.015625f : 0f),
            (tween.StrobeFade ? 1f : 0f) + (tween.ComposeAlphaAtColorEndpoints ? 2f : 0f));
        textureColors[((rowOffset + 8) * count) + x] = tween.EasingShaderIds;
    }

    // Dense all-light filters resolve the same authored endpoint times for every light, so a
    // refresh-scoped memo turns the per-light boost lookups into dictionary hits. The cache is
    // cleared per call so a stale result can never outlive one ribbon update.
    private Func<float, bool> BeginBoostMemoization(Func<float, bool> isBoostAt)
    {
        boostCache.Clear();
        activeBoostResolver = isBoostAt;
        if (isBoostAt == null)
        {
            return null;
        }

        return cachedBoostResolver ??= CachedIsBoostAt;
    }

    private bool CachedIsBoostAt(float time)
    {
        if (boostCache.TryGetValue(time, out var boost))
        {
            return boost;
        }

        boost = activeBoostResolver(time);
        boostCache[time] = boost;
        return boost;
    }

    // Content-derived dirty check: a pooled ribbon rebind during scrubbing rebuilds byte-identical
    // pixels, so compare the packed payload and skip the synchronous texture upload when unchanged.
    // Exact float equality is correct here because identical inputs run through identical math.
    private bool UploadIfChanged()
    {
        if (!uploadDirty && uploadedColors.AsSpan().SequenceEqual(textureColors))
        {
            return false;
        }

        if (uploadedColors.Length != textureColors.Length)
        {
            uploadedColors = new Color[textureColors.Length];
        }

        Array.Copy(textureColors, uploadedColors, textureColors.Length);
        texture.SetPixels(textureColors);
        texture.Apply(false, false);
        uploadDirty = false;
        return true;
    }

    // Reallocate pooled endpoint storage only when the physical group size changes.
    private void EnsureCapacity(int lightCount, int rows = 4)
    {
        // RandomizedMonstercatRibbonEdgesMatchSupersampling: varying neighbor counts
        // reuse a pooled timeline texture's existing rows; per-column headers bound
        // every lookup, so unused capacity needs neither clearing nor reallocating.
        if (sourceColors.Length == lightCount && texture != null
            && (texture.height == rows || (rows >= 10 && texture.height >= rows)))
        {
            return;
        }

        ReleaseTexture();
        sourceColors = new Color[lightCount];
        sourceStrobeColors = new Color[lightCount];
        transitionColors = new Color[lightCount];
        transitionStrobeColors = new Color[lightCount];
        textureColors = new Color[lightCount * rows];
        // UpdateTimeline fills the states before this resize runs, so keep the already-matching buffer
        // instead of wiping its first-pass results.
        if (timelineStates.Length != lightCount)
        {
            timelineStates = new LightColorEventStateData[lightCount];
        }
        timelineTweens = new LightColorTween[lightCount];
        for (var light = 0; light < lightCount; light++)
            timelineTweens[light] = new LightColorTween();
        texture = new Texture2D(lightCount, rows, rows == 4 ? TextureFormat.RGBAHalf : TextureFormat.RGBAFloat, false, true)
        {
            name = "GLS Light Transition Preview",
            // Every fetch lands on an exact texel centre; the strip-edge AA blends evaluated colors, not texels.
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.DontSave
        };
        // A freshly allocated texture is blank, so the next pack must upload regardless of payload equality.
        uploadDirty = true;
    }

    public void Dispose()
    {
        ReleaseTexture();
        sourceColors = Array.Empty<Color>();
        sourceStrobeColors = Array.Empty<Color>();
        transitionColors = Array.Empty<Color>();
        transitionStrobeColors = Array.Empty<Color>();
        textureColors = Array.Empty<Color>();
        uploadedColors = Array.Empty<Color>();
        uploadDirty = true;
        boostCache.Clear();
        activeBoostResolver = null;
        timelineStates = Array.Empty<LightColorEventStateData>();
        timelineTweens = Array.Empty<LightColorTween>();
        // Release references to old maps when the pooled preview is disposed.
        timelineNeighborStates.Clear();
        timelineNeighborOffsets = Array.Empty<int>();
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
