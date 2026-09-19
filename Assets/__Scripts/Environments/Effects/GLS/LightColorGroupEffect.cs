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
    [NonSerialized] public ColorSchemeSO ColorScheme;

    [SerializeField] private List<LightController> lightEntries = new();
    private LightColorGroupContainer[] idToContainer = Array.Empty<LightColorGroupContainer>();
    private LightColorGroupContainer[] activeContainers = Array.Empty<LightColorGroupContainer>();

    public void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChange;
    public void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChange;

    public void Register(LightController controller) => lightEntries.Add(controller);

    public void Unregister(LightController controller) => lightEntries.Remove(controller);

    private void HandleBoostChange(bool boost)
    {
        for (var i = 0; i < activeContainers.Length; i++)
        {
            var container = activeContainers[i];
            var state = container.EventContainer.CurrentState;
            var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
            var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);

            // Resolve default GLS colors through the color scheme injected by the dev effect manager.
            var startColor = ResolveNormalColor(startState);
            var endColor = ResolveNormalColor(endState);

            container.Tween.StartColor = startColor;
            container.Tween.EndColor = endColor;
            container.Tween.StartStrobeColor = ResolveStrobeColor(startState);
            container.Tween.EndStrobeColor = ResolveStrobeColor(endState);

            // A paused preview has no subsequent time tick to apply the retinted GLS tween to its controllers. Apply the color change immediately.
            container.Tween.UpdateTime(Atsc.CurrentSongBpmTime);
            foreach (var controller in container.Lights)
            {
                controller.SetColor(container.Tween.Color);
            }
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightColorGroupContainer[Count];
        foreach (var entry in lightEntries)
        {
            if (entry.ID >= Count)
            {
                Debug.LogError(
                    $"{entry}:{entry.ID} ID is larger than supported by group {ID}:{Count}, was the controller modified?");
                continue;
            }

            if (idToContainer[entry.ID] is null)
            {
                idToContainer[entry.ID] = new();
                var container = idToContainer[entry.ID];

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

                var start = CreateState(new() { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                start.LocalJsonTime = start.StartTime;

                var end = CreateState(new() { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                end.LocalJsonTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                container.EventContainer.SetStateAt(0);

                InitializeStates(container.GroupContainer, start, end);
            }

            idToContainer[entry.ID].Lights.Add(entry);
        }

        activeContainers = idToContainer.Where(x => x is not null).ToArray();
    }

    public override void Refresh()
    {
        foreach (var container in activeContainers)
        {
            container.EventContainer.SetStateAt(Atsc.CurrentSongBpmTime);
            UpdateObject(container);
            container.Tween.UpdateTime(Atsc.CurrentSongBpmTime);
            foreach (var controller in container.Lights) controller.SetColor(container.Tween.Color);
        }
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        foreach (var container in activeContainers)
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, isPlaying)) UpdateObject(container);
            if (!container.Tween.UpdateTime(time)) continue;
            foreach (var controller in container.Lights) controller.SetColor(container.Tween.Color);
        }
    }

    private void UpdateObject(LightColorGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var start = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        var end = (LightColorEventStateData)(state.Next.UsePrevious ? start : state.Next);
        ConfigureTween(container.Tween, state, ResolveNormalColor(start), ResolveNormalColor(end),
            ResolveStrobeColor(start), ResolveStrobeColor(end), BeatSaberSongContainer.Instance.Map);
    }

    public static void ConfigureTween(
        LightColorTween tween,
        LightColorEventStateData state,
        Color startColor,
        Color endColor,
        Color startStrobeColor,
        Color endStrobeColor,
        BaseDifficulty map)
    {
        tween.StartTimeAlpha = tween.StartTimeColor = state.StartTime;
        var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartAlpha = startState.Brightness;
        tween.StartColor = startColor;
        tween.StartStrobeFrequency = GLSEventCommon.GetStrobeFrequency(startState.Base);
        tween.StartStrobeBrightness = startState.Base.StrobeBrightness;
        tween.StartStrobeColor = startStrobeColor;
        if (tween.StartStrobeFrequency <= 0f)
        {
            tween.StartStrobeBrightness = tween.StartAlpha;
            tween.StartStrobeColor = tween.StartColor;
        }

        tween.EndTimeAlpha = tween.EndTimeColor = state.EndTime;
        var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndAlpha = endState.Brightness;
        tween.EndColor = endColor;

        if (endState.Base.Easing == (int)EaseType.None)
        {
            tween.EndStrobeFrequency = GLSEventCommon.GetStrobeFrequency(startState.Base);
            tween.EndStrobeBrightness = startState.Base.StrobeBrightness;
            tween.EndStrobeColor = tween.StartStrobeColor;
            tween.StrobeFade = startState.Base.StrobeFade == 1;
            tween.StrobeEasing = EasingFromId(startState.Base.ChromaStrobeEasing);
            tween.StrobeColorEasing = null;
        }
        else
        {
            tween.EndStrobeFrequency = GLSEventCommon.GetStrobeFrequency(endState.Base);
            tween.EndStrobeBrightness = endState.Base.StrobeBrightness;
            tween.EndStrobeColor = endStrobeColor;
            // shouldn't we fade between no strobe fade and strobe fade...? What does the game even do?
            tween.StrobeFade = startState.Base.StrobeFade == 1;
            tween.StrobeEasing = EasingFromId(startState.Base.ChromaStrobeEasing);
            tween.StrobeColorEasing = EasingFromId(endState.Base.ChromaStrobeColorEasing);
        }

        if (tween.EndStrobeFrequency <= 0f)
        {
            tween.EndStrobeBrightness = tween.EndAlpha;
            tween.EndStrobeColor = tween.EndColor;
        }

        tween.Easing = Easing.FromID(endState.Base.Easing);
        tween.ColorEasing = endState.Base.Easing == (int)EaseType.None
            ? null
            : EasingFromId(endState.Base.ChromaColorEasing);
        tween.ColorLerpType = endState.Base.Easing == (int)EaseType.None
            ? BasicEventColorLerpType.RGB
            : endState.Base.CustomLerpType;

        var intervalEasingShaderId = Easing.EasingShaderId(endState.Base.Easing);
        tween.EasingShaderIds = new Vector4(
            intervalEasingShaderId,
            tween.ColorEasing != null
                ? Easing.EasingShaderId(endState.Base.ChromaColorEasing.Value)
                : intervalEasingShaderId,
            tween.StrobeColorEasing != null
                ? Easing.EasingShaderId(endState.Base.ChromaStrobeColorEasing.Value)
                : intervalEasingShaderId,
            tween.StrobeEasing != null
                ? Easing.EasingShaderId(startState.Base.ChromaStrobeEasing.Value)
                : Easing.EasingShaderId((int)EaseType.InOutCubic));

        var strobeScale = GLSEventCommon.GetStrobeFrequencyScale(map, state.StartTime);
        tween.StartStrobeFrequency *= strobeScale;
        tween.EndStrobeFrequency *= strobeScale;
    }

    private static Func<float, float> EasingFromId(int? id) =>
        id is { } value ? Easing.FromID(value) : null;

    private Color ResolveNormalColor(LightColorEventStateData state)
    {
        var color = state.Base.CustomColor
            ?? ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
        return GLSColorDistribution.ApplyNormal(
            color,
            state.Box,
            state.Base,
            state.DistributionProgress,
            state.AffectedLightProgress);
    }

    private Color ResolveStrobeColor(LightColorEventStateData state)
    {
        var mainColor = state.Base.CustomColor
            ?? ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
        return GLSColorDistribution.ApplyStrobe(
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

    protected override LightColorEventStateData[] GenerateEvents(
        LightColorGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime) =>
        GenerateColorEvents(
            BeatSaberSongContainer.Instance.Map,
            state,
            distributionOffset,
            maxRelativeJsonTime);

    internal static LightColorEventStateData[] GenerateColorEvents(
        BaseDifficulty map,
        LightColorGroupStateData state,
        float distributionOffset,
        float maxRelativeJsonTime)
    {
        var box = state.Box;
        var events = box.Events;
        var durationOffset = state.DurationOrder * state.BeatStep;
        var affectedChunkProgress = state.AffectedChunkOrder / (float)Mathf.Max(state.AffectedChunkCount - 1, 1);
        var affectedLightProgress = state.AffectedLightOrder / (float)Mathf.Max(state.AffectedLightCount - 1, 1);
        var brightnessAffectsFirst = box.BrightnessAffectFirst == 1;

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
    public readonly BaseLightColorEventBox Box;
    public readonly float DistributionProgress;
    public readonly float AffectedLightProgress;

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
    public readonly LightColorTween Tween = new();
    public readonly List<LightController> Lights = new();
}
