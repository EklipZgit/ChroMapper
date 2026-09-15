using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public class
    LightColorGroupEffect : EventGroupEffect<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [SerializeField] public ColorSchemeProvider ColorSchemeProvider;

    [SerializeField] private List<LightController> lightEntries = new();
    private LightColorGroupContainer[] idToContainer = Array.Empty<LightColorGroupContainer>();
    protected LightColorGroupContainer[] activeContainers = Array.Empty<LightColorGroupContainer>();

    public void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChange;
    public void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChange;

    public void Register(LightController controller) => lightEntries.Add(controller);

    public void Unregister(LightController controller) => lightEntries.Remove(controller);

    protected virtual void HandleBoostChange(bool boost)
    {
        var time = Atsc.CurrentSongBpmTime;
        for (var i = 0; i < activeContainers.Length; i++)
        {
            var container = activeContainers[i];
            var state = container.EventContainer.CurrentState;
            var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
            var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);

            // Resolve default GLS colors through the color scheme injected by the dev effect manager.
            // ModeBColorShiftsUseDenseAffectedChunkOrder recomputes boost-dependent authored endpoints while retaining each light's cached spatial progress.
            var startColor = ResolveNormalColor(startState);
            var endColor = ResolveNormalColor(endState);

            container.Tween.StartColor = startColor;
            container.Tween.EndColor = endColor;
            container.Tween.StartStrobeColor = ResolveStrobeColor(startState);
            container.Tween.EndStrobeColor = ResolveStrobeColor(endState);

            // A paused preview has no subsequent time tick to apply the retinted GLS tween to its controllers. Apply the color change immediately.
            container.Tween.UpdateTime(time);
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightColorGroupContainer[Count];
        foreach (var elementId in lightEntries.Select(x => x.ID).Distinct())
        {
            if (elementId < 0 || elementId >= Count)
            {
                Debug.LogError($"Element {elementId} is outside the supported range for group {ID}:{Count}.");
                continue;
            }

            if (idToContainer[elementId] is null)
            {
                idToContainer[elementId] = new LightColorGroupContainer { ElementId = elementId };
                var container = idToContainer[elementId];

                var startEvent = new LightColorEventStateData(new BaseLightColorBase(), short.MinValue);
                var endEvent = new LightColorEventStateData(
                    new BaseLightColorBase { UsePrevious = 1 },
                    float.MaxValue);
                container.EventContainer.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));

                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;

                container.EventContainer.AddState(startEvent);
                container.EventContainer.AddState(endEvent);

                var start = CreateState(
                    new BaseLightColorEventBoxGroup { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                start.LocalJsonTime = start.StartTime;

                var end = CreateState(
                    new BaseLightColorEventBoxGroup { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new BaseIndexFilter { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                end.LocalJsonTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                container.EventContainer.SetStateAt(0);

                InitializeStates(container.GroupContainer, start, end);
            }
        }

        foreach (var entry in lightEntries.Where(x => 0 <= x.ID && x.ID < Count))
            idToContainer[entry.ID]?.Lights.Add(entry);

        activeContainers = idToContainer.Where(x => x is not null).ToArray();
    }

    public override void Refresh()
    {
        var time = Atsc.CurrentSongBpmTime;
        foreach (var container in activeContainers)
        {
            container.EventContainer.SetStateAt(time);
            UpdateObject(container);
            container.Tween.UpdateTime(time);
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var container in activeContainers)
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, isPlaying)) UpdateObject(container);
            if (!container.Tween.UpdateTime(time)) continue;
            foreach (var controller in container.Lights)
                controller.SetColor(container.Tween.Color, container.EventContainer.CurrentState, time);
        }
    }

    // Resolve values before shared preparation so per-light state changes allocate no resolver delegates.
    protected virtual void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var start = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        var end = (LightColorEventStateData)(state.Next.UsePrevious ? start : state.Next);
        // GLSColorTimeline shares the exact endpoint preparation in ConfigureTween; playback injects
        // color-scheme values while the data-only timeline injects appearance values.
        ConfigureTween(container.Tween, state, ResolveNormalColor(start), ResolveNormalColor(end),
            ResolveStrobeColor(start), ResolveStrobeColor(end));
    }

    // Single source of truth for tween endpoint state so the data-only GLSColorTimeline and playback
    // can never diverge on colors, strobes, easings, or phase metadata.
    public static void ConfigureTween(
        LightColorTween tween,
        LightColorEventStateData state,
        Color startColor,
        Color endColor,
        Color startStrobeColor,
        Color endStrobeColor)
    {
        tween.StartTimeAlpha = tween.StartTimeColor = state.StartTime;
        var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartAlpha = startState.Brightness;
        // Spatial shifts are baked into transition endpoints; the event easing below remains solely responsible for temporal interpolation.
        tween.StartColor = startColor;
        // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint keeps playback and ribbon phase on the same shared frequency conversion.
        tween.StartStrobeFrequency = GLSEventCommon.GetStrobeFrequency(startState.Base);
        tween.StartStrobeBrightness = startState.Base.StrobeBrightness;
        tween.StartStrobeColor = startStrobeColor;
        // NoStrobeTransitionConvergesBeforeItsBoundary: a zero effective frequency endpoint contributes
        // its primary color and brightness instead of its unused strobeColor/sb pair, so both phase
        // branches converge on the same primary without touching the linear frequency integration.
        if (tween.StartStrobeFrequency <= 0f)
        {
            tween.StartStrobeBrightness = tween.StartAlpha;
            tween.StartStrobeColor = tween.StartColor;
        }

        tween.EndTimeAlpha = tween.EndTimeColor = state.EndTime;
        var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndAlpha = endState.Brightness;
        tween.EndColor = endColor;

        // StrobeFadeTransitionRibbonMatchesLightTweenAtMidpoint shares both held and interpolated endpoint frequency conversion with the ribbon phase model.
        if (endState.Base.Easing == (int)EaseType.None)
        {
            tween.EndStrobeFrequency = GLSEventCommon.GetStrobeFrequency(startState.Base);
            tween.EndStrobeBrightness = startState.Base.StrobeBrightness;
            tween.EndStrobeColor = tween.StartStrobeColor;
            tween.StrobeFade = startState.Base.StrobeFade == 1;
            // GLSColorEasingInputTest: an instant endpoint keeps its own fade curve but has no strobe interval to ease.
            tween.StrobeEasing = EasingFromId(startState.Base.ChromaStrobeEasing);
            tween.StrobeColorEasing = null;
        }
        else
        {
            tween.EndStrobeFrequency = GLSEventCommon.GetStrobeFrequency(endState.Base);
            tween.EndStrobeBrightness = endState.Base.StrobeBrightness;
            tween.EndStrobeColor = endStrobeColor;
            // shouldn't we fade between no strobe fade and strobe fade...? What does the game even do?
            // COutgoingPulseUsesItsAuthoredStrobeEasing: pulse shape belongs to the active node, while RGB transition easing belongs to the destination.
            tween.StrobeFade = startState.Base.StrobeFade == 1;
            tween.StrobeEasing = EasingFromId(startState.Base.ChromaStrobeEasing);
            tween.StrobeColorEasing = EasingFromId(endState.Base.ChromaStrobeColorEasing);
        }

        // NoStrobeTransitionConvergesBeforeItsBoundary: an end whose effective frequency is zero (or an
        // Instant endpoint holding a zero-frequency start) contributes its primary color/brightness, so
        // the strobe band cannot snap to an unused strobeColor at the boundary.
        if (tween.EndStrobeFrequency <= 0f)
        {
            tween.EndStrobeBrightness = tween.EndAlpha;
            tween.EndStrobeColor = tween.EndColor;
        }

        tween.Easing = Easing.FromID(endState.Base.Easing);
        // InstantDestinationKeepsHeldRibbonWithoutApplyingStoredEasing ignores transition-only metadata on a step destination.
        tween.ColorEasing = endState.Base.Easing == (int)EaseType.None
            ? null
            : EasingFromId(endState.Base.ChromaColorEasing);
        // GLSEasingTypeRibbonInputTest: the ahead node's customData.easingType picks the transition's color
        // space for both the normal and strobe color tracks.
        // A held interval must not convert its source HDR color through an instant destination's unused HSV mode.
        tween.ColorLerpType = endState.Base.Easing == (int)EaseType.None
            ? BasicEventColorLerpType.RGB
            : BasicEventColorLerp.FromGlsEasingType(endState.Base.CustomLerpType);
    }

    // Per-track easing keys are optional; absent metadata falls back to the tween's interval easing.
    private static Func<float, float> EasingFromId(int? id) =>
        id is { } value ? Easing.FromID(value) : null;

    protected static float StrobeFrequencyFor(BaseLightColorBase lightColorBase)
    {
        // A 0-light-level node with no strobe flash is not a strobe, regardless of strobeInterval or strobeColor.
        if (lightColorBase.Brightness <= 0f && lightColorBase.StrobeBrightness <= 0f) return 0f;

        // customData.strobeInterval is the period in beats per strobe cycle; the in-editor tween expects cycles per beat.
        return lightColorBase.ChromaStrobeInterval is { } interval && interval > 0f
            ? 1f / interval
            : lightColorBase.Frequency;
    }

    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases supplies both cached playback coordinates while retaining box-before-event normal composition.
    private Color ResolveNormalColor(LightColorEventStateData state)
    {
        var color = state.Base.CustomColor
            ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
        // PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases prevents playback from reverting l instructions to the legacy chunk-only overload.
        return GLSColorShift.ApplyNormal(
            color,
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);
    }

    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases supplies the same coordinate pair to independent strobe lists while retaining the authored main-color fallback.
    private Color ResolveStrobeColor(LightColorEventStateData state)
    {
        var mainColor = state.Base.CustomColor
            ?? ColorSchemeProvider.ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
        // PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases requires independent strobe instructions to receive the same cached coordinate pair as normal instructions.
        return GLSColorShift.ApplyStrobe(
            mainColor,
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);
    }

    protected override LightColorGroupStateData CreateState(BaseLightColorEventBoxGroup data) => new(data);

    protected override
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>
        GetGroupContainer((Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<LightColorEventStateData, BaseLightColorBase> GetEventContainer(
        (Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup>
            groupContainer, StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer)>
        GetContainers() =>
        idToContainer.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseLightColorEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseLightColorEventBox box) => box.Events[^1].RelativeJsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseLightColorEventBox box,
        int order) =>
        GetBrightnessStep(indexFilter, box, order);

    // GLSColorTimeline shares this exact brightness offset so data-only preview endpoints track playback.
    internal static float GetBrightnessStep(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseLightColorEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.BrightnessDistributionType,
            box.BrightnessDistribution,
            (EaseType)box.Easing);

    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases converts the two dense playback orders to the requested 0..1 chunk and affected-light coordinates once per generated state.
    protected override LightColorEventStateData[] GenerateEvents(
        LightColorGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime) =>
        // GLSColorTimeline passes its own map so the data-only path never reads the playback singleton.
        GenerateColorEvents(
            BeatSaberSongContainer.Instance.Map,
            state,
            distributionOffset,
            maxRelativeJsonTime);

    // Shared with GLSColorTimeline so preview timelines generate identical per-light states (times,
    // brightness offsets, and dense progress coordinates) without duplicating the color-specific rules.
    internal static LightColorEventStateData[] GenerateColorEvents(
        BaseDifficulty map,
        LightColorGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime)
    {
        var box = state.Box;
        var events = box.Events;
        var durationOffset = state.DurationOrder * state.BeatStep;
        // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases normalizes dense orders once per
        // generated endpoint, keeping filter work out of playback ticks.
        var affectedChunkProgress = state.AffectedChunkOrder / (float)Mathf.Max(state.AffectedChunkCount - 1, 1);
        var affectedLightProgress = state.AffectedLightOrder / (float)Mathf.Max(state.AffectedLightCount - 1, 1);
        var brightnessAffectsFirst = box.BrightnessAffectFirst == 1;

        // DifferentAllLightsGroupInterruptsAndCancelsFilteredEventsAtOrAfterGroupStart mirrors
        // LightColorBeatmapEventDataBox.Unpack's strict nodeBeat < next ElementData start boundary.
        // Two passes over the box's small event array avoid the LINQ enumerator and resized-copy allocs.
        var count = 0;
        for (var i = 0; i < events.Length; i++)
        {
            if (state.Base.JsonTime + events[i].RelativeJsonTime + durationOffset < maxRelativeJsonTime)
            {
                count++;
            }
        }

        var generated = new LightColorEventStateData[count];
        var write = 0;
        for (var i = 0; i < events.Length; i++)
        {
            var x = events[i];
            if (state.Base.JsonTime + x.RelativeJsonTime + durationOffset >= maxRelativeJsonTime) continue;
            generated[write++] = new LightColorEventStateData(
                x,
                (float)map.JsonTimeToSongBpmTime(
                    state.Base.JsonTime + x.RelativeJsonTime + durationOffset),
                i == 0 && !brightnessAffectsFirst ? 0f : distributionOffset,
                box,
                affectedChunkProgress,
                affectedLightProgress);
        }

        return generated;
    }
}

public class LightColorGroupStateData : EventGroupStateData<
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public LightColorGroupStateData(BaseLightColorEventBoxGroup data) : base(data)
    {
    }
}

[Serializable]
public class LightColorEventStateData : EventGroupEventStateData<BaseLightColorBase>
{
    public readonly float Brightness;
    // PerLightShiftPreviewUsesAffectedLightsAcrossBoxAndEventPhases caches the owning box and both spatial coordinates once when the state is generated, outside per-frame tween updates.
    public readonly BaseLightColorEventBox Box;
    public readonly float DistributionProgress;
    public readonly float AffectedLightProgress;

    // PerLightPlaybackEndpointsMatchPreviewAtBothStrobePhases retains both coordinates so boost changes and tween endpoint updates use the same instruction-specific progress.
    public LightColorEventStateData(
        BaseLightColorBase data,
        float startTime,
        float offset = 0f,
        BaseLightColorEventBox box = null,
        float distributionProgress = 0f,
        float affectedLightProgress = 0f) : base(
        data,
        startTime,
        data.Easing,
        data.UsePrevious)
    {
        Brightness = data.Brightness + offset;
        Box = box;
        DistributionProgress = distributionProgress;
        AffectedLightProgress = affectedLightProgress;
    }
}

public record LightColorGroupContainer : EventGroupContainer<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public int ElementId;
    public readonly LightColorTween Tween = new();
    public readonly List<LightController> Lights = new();
}
