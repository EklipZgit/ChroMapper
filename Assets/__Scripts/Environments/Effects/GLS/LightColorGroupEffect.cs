using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightColorGroupEffect : EventGroupEffect<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup<BaseLightColorEventBox>,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [SerializeField] private List<LightControllerEntry> lightEntries = new();
    private LightColorGroupContainer[] idToContainer = Array.Empty<LightColorGroupContainer>();

    [NonSerialized] public ColorSchemeSO ColorScheme;

    public void Start() => ColorBoostEffect.OnStateChanged += HandleBoostChange;
    public void OnDestroy() => ColorBoostEffect.OnStateChanged -= HandleBoostChange;

    public void Register(int group, int id, LightController controller) =>
        lightEntries.Add(new() { Type = group, ID = id, Controller = controller });

    public void Unregister(int group, int id) =>
        lightEntries.Remove(lightEntries.Find(e => e.Type == group && e.ID == id));

    public void Unregister(LightController controller) =>
        lightEntries.Remove(lightEntries.Find(e => e.Controller == controller));

    private void HandleBoostChange(bool boost)
    {
        foreach (var container in idToContainer.Where(c => c is not null))
        {
            var state = container.EventContainer.CurrentState;

            container.Tween.StartColor = ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
            container.Tween.EndColor = ColorScheme.GetColorFrom((LightColor)state.Next.Base.Color, false);
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightColorGroupContainer[Count];
        foreach (var entry in lightEntries)
        {
            if (idToContainer[entry.ID] is null)
            {
                idToContainer[entry.ID] = new();
                var container = idToContainer[entry.ID];

                var startEvent = new LightColorEventStateData(new BaseLightColorBase(), short.MinValue);
                var endEvent = new LightColorEventStateData(
                    new BaseLightColorBase { UsePrevious = 1 },
                    float.MaxValue);
                container.EventContainer.GenerateChunk(Atsc);

                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;

                container.EventContainer.Chunks[0].Add(startEvent);
                container.EventContainer.Chunks[^1].Add(endEvent);

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

            idToContainer[entry.ID].Lights.Add(entry.Controller);
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var container in idToContainer.Where(c => c is not null))
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, Atsc.IsPlaying))
            {
                var state = container.EventContainer.CurrentState;
                var tween = container.Tween;

                tween.StartTimeAlpha = tween.StartTimeColor = state.StartTime;
                var startState = (LightColorEventStateData)(state.UsePrevious ? state.Previous : state);
                tween.StartAlpha = startState.Brightness;
                tween.StartColor = ColorScheme.GetColorFrom((LightColor)startState.Base.Color, false);
                tween.StartStrobeFrequency = startState.Base.Frequency;
                tween.StartStrobeBrightness = startState.Base.StrobeBrightness;

                tween.EndTimeAlpha = tween.EndTimeColor = state.EndTime;
                var endState = (LightColorEventStateData)(state.Next.UsePrevious ? startState : state.Next);
                tween.EndAlpha = endState.Brightness;
                tween.EndColor = ColorScheme.GetColorFrom((LightColor)endState.Base.Color, false);

                if (endState.Base.Easing == (int)EaseType.None)
                {
                    tween.EndStrobeFrequency = startState.Base.Frequency;
                    tween.EndStrobeBrightness = startState.Base.StrobeBrightness;
                }
                else
                {
                    tween.EndStrobeFrequency = endState.Base.Frequency;
                    tween.EndStrobeBrightness = endState.Base.StrobeBrightness;
                }

                tween.StrobeFade = endState.Base.StrobeFade == 1;
                tween.Easing = Easing.FromID(endState.Base.Easing);
            }

            if (!container.Tween.UpdateTime(time)) continue;
            foreach (var controller in container.Lights) controller.UpdateColor(container.Tween.Color);
        }
    }

    protected override LightColorGroupStateData CreateState(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) =>
        new(data);

    protected override Axis GetAxis(BaseLightColorEventBox box) => Axis.X;

    protected override
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
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
        IEnumerable<(StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
            groupContainer, StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer)>
        GetContainers() =>
        idToContainer.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseLightColorEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseLightColorEventBox box) => box.Events[^1].JsonTime;

    protected override float GetDistribution(
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
        float maxJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
                {
                    var affected = !(i == 0 && state.Box.BrightnessAffectFirst != 1);
                    var d = new LightColorEventStateData(
                        x,
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                            state.Base.JsonTime + x.JsonTime + (state.DurationOrder * state.BeatStep)),
                        affected ? distributionOffset : 0f);
                    return d;
                }
            )
            .Where(x => state.Base.JsonTime + x.Base.JsonTime + (state.DurationOrder * state.BeatStep) <= maxJsonTime)
            .ToArray();
}

public class LightColorGroupStateData : EventGroupStateData<
    BaseLightColorEventBoxGroup<BaseLightColorEventBox>,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public LightColorGroupStateData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) : base(data)
    {
    }
}

[Serializable]
public class LightColorEventStateData : EventGroupEventStateData<BaseLightColorBase>
{
    public readonly float Brightness;

    public LightColorEventStateData(BaseLightColorBase data, float startTime, float offset = 0f) : base(
        data,
        startTime,
        data.Easing,
        data.UsePrevious) =>
        Brightness = data.Brightness + offset;
}

public record LightColorGroupContainer : EventGroupContainer<
    LightColorGroupStateData,
    LightColorEventStateData,
    BaseLightColorEventBoxGroup<BaseLightColorEventBox>,
    BaseLightColorEventBox,
    BaseLightColorBase>
{
    public readonly LightColorTween Tween = new();
    public readonly List<BaseLightController> Lights = new();
}
