using System;
using System.Collections.Generic;
using System.Globalization;
using ZLinq;
using System.Text;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventCommon
{
    // Keep zero-brightness GLS sections 30% darker than the previous 25%-of-source off endpoint.
    private const float DimmedColorFraction = 0.175f;
    // Retain one diagnostic per source node while GLS color-ribbon matching is being verified.
    private static readonly HashSet<string> ribbonDiagnosticKeys = new();

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

    public static string GetColorInfo(BaseLightColorBase evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Brightness * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));
        if (evt.Frequency > 0) sb.Append($"1/{evt.Frequency}");
        if (evt.Frequency > 0 && evt.StrobeFade == 1) sb.Append(' ');
        if (evt.StrobeFade == 1) sb.Append('L');
        if ((evt.Frequency > 0 || evt.StrobeFade == 1) && evt.StrobeBrightness > 0f) sb.Append(' ');
        if (evt.StrobeBrightness > 0f) sb.Append((evt.StrobeBrightness * 100f).ToString(CultureInfo.InvariantCulture));

        return sb.ToString();
    }

    public static string GetRotationInfo(BaseLightRotationBase evt)
    {
        var sb = new StringBuilder();

        var direction = evt.Direction switch
        {
            (int)LightRotationDirection.Clockwise => "CW",
            (int)LightRotationDirection.CounterClockwise => "CCW",
            _ => "A"
        };

        sb.AppendLine(evt.Rotation.ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));
        sb.AppendLine($"{direction} <{evt.Loop}>");

        return sb.ToString();
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
        var sb = new StringBuilder();

        sb.AppendLine((evt.Translation * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.EaseType));

        return sb.ToString();
    }

    public static string GetFloatFXInfo(BaseFxEventFloat evt)
    {
        var sb = new StringBuilder();

        sb.AppendLine((evt.Value * 100f).ToString(CultureInfo.InvariantCulture));
        sb.AppendLine(Easing.IDToShortName.GetValueOrDefault(evt.Easing));

        return sb.ToString();
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
        var gradient = new ChromaLightGradient(
            startColor,
            endColor,
            transition.SongBpmTime - source.SongBpmTime);
        controller.SetVisible(true);
        controller.UpdateGradientData(gradient);
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
        var groups = songContainer != null
            ? songContainer.Map?.LightColorEventBoxGroups
            : null;
        if (filter == null || sourceGroup == null || groups == null) return false;

        // Order matching nodes by their absolute event time because event ranges from neighboring groups can overlap.
        followingEvent = groups
            .AsValueEnumerable()
            .Where(group => group.ID == sourceGroup.ID)
            .SelectMany(group => GetEventsForFilter(group, filter))
            .Where(evt => evt.JsonTime > source.JsonTime)
            .OrderBy(evt => evt.JsonTime)
            .ThenBy(evt => evt.BoxIndex)
            .FirstOrDefault();

        transition = followingEvent is { UsePrevious: 0 } && followingEvent.Easing != (int)EaseType.None
            ? followingEvent
            : null;
        return transition != null;
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

    // A group can contain multiple boxes with equivalent cloned filters, so treat their events as one filter sequence.
    private static IEnumerable<BaseLightColorBase> GetEventsForFilter(
        BaseLightColorEventBoxGroup group,
        BaseIndexFilter filter) =>
        group.Boxes
            .AsValueEnumerable()
            .Where(box => IndexFiltersMatch(filter, box.IndexFilter))
            .SelectMany(box => box.Events)
            .ToList();

    // // Keep the current match evidence in the Console until the GLS ribbon behavior is confirmed in-editor.
    // private static void LogRibbonDiagnostic(
    //     BaseLightColorBase source,
    //     BaseLightColorBase followingEvent,
    //     Color? startColor,
    //     Color? endColor)
    // {
    //     var key = $"{source.EventBoxGroupData?.ID}:{source.JsonTime:F4}:{source.BoxIndex}:{source.EventBoxData?.IndexFilter?.ToJson()}";
    //     if (!ribbonDiagnosticKeys.Add(key))
    //         return;

    //     Debug.Log(
    //         $"[GLS Ribbon] group:{source.EventBoxGroupData?.ID}, source=time:{source.JsonTime:F3}, easing:{source.Easing}, color:{source.Color}; " +
    //         $"following={(followingEvent == null ? "none" : $"time:{followingEvent.JsonTime:F3}, easing:{followingEvent.Easing}, usePrevious:{followingEvent.UsePrevious}, color:{followingEvent.Color}")}; " +
    //         $"gradient={startColor?.ToString() ?? "none"}->{endColor?.ToString() ?? "none"}.");
    // }

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
