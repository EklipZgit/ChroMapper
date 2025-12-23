using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightColorGroupEffect : StateManager<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
{
    [SerializeField] private List<LightControllerEntry> lightEntries = new();

    private readonly Dictionary<int, Dictionary<int, (
            StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
            container,
            List<BaseLightController> lights)>>
        lightsByGroupAndId = new();

    private readonly List<(
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
        container,
        List<BaseLightController> lights)> activeLights = new();

    public ColorSchemeSO ColorScheme;

    public void Register(int group, int id, LightController controller) =>
        lightEntries.Add(new() { Type = group, ID = id, Controller = controller });

    public void Unregister(int group, int id) =>
        lightEntries.Remove(lightEntries.Find(e => e.Type == group && e.ID == id));

    public void Unregister(LightController controller) =>
        lightEntries.Remove(lightEntries.Find(e => e.Controller == controller));

    public override void Initialize()
    {
        activeLights.Clear();
        lightsByGroupAndId.Clear();
        foreach (var entry in lightEntries)
        {
            lightsByGroupAndId.TryAdd(entry.Type, new());
            if (!lightsByGroupAndId[entry.Type].ContainsKey(entry.ID))
            {
                var container =
                    new StateChunksContainer<LightColorGroupStateData,
                        BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();
                var start = CreateState(new());
                var end = CreateState(new());
                start.Box = new() { Events = new[] { new BaseLightColorBase() } };
                end.Box = new() { Events = new[] { new BaseLightColorBase() } };
                InitializeStates(container, start, end);
                lightsByGroupAndId[entry.Type][entry.ID] = (container, new List<BaseLightController>());
            }

            var active = lightsByGroupAndId[entry.Type][entry.ID];
            active.lights.Add(entry.Controller);
            activeLights.Add(active);
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var (container, lights) in activeLights)
        {
            container.IsCurrentOrFindState(time, Atsc.IsPlaying);
            foreach (var controller in lights)
            {
                UpdateObject(controller, container.CurrentState, time);
                controller.UpdateTime(time);
            }
        }
    }

    private void UpdateObject(BaseLightController lightController, LightColorGroupStateData state, float time)
    {
        var (previousTime, previousOffset, previousEvent) = GetCurrentOrPreviousEvent(state, time);
        if (previousEvent == null) return;

        var (nextTime, nextOffset, nextEvent) = GetNextEvent(state, previousTime);
        if (nextEvent == null) return;

        if (nextEvent.UsePrevious == 1)
        {
            nextOffset = previousOffset;
            nextEvent = previousEvent;
        }

        lightController.StartTimeAlpha = previousTime;
        lightController.StartTimeColor = previousTime;
        lightController.StartAlpha = previousEvent.Brightness + previousOffset;
        lightController.StartColor = ColorScheme.GetColorFrom((LightColor)previousEvent.Color, false);
        lightController.StartStrobeFrequency = previousEvent.Frequency;
        lightController.StartStrobeBrightness = previousEvent.StrobeBrightness;

        lightController.EndTimeAlpha = nextTime;
        lightController.EndTimeColor = nextTime;
        lightController.EndAlpha = nextEvent.Brightness + nextOffset;
        lightController.EndColor = ColorScheme.GetColorFrom((LightColor)nextEvent.Color, false);
        lightController.EndStrobeFrequency = nextEvent.Frequency;
        lightController.EndStrobeBrightness = nextEvent.StrobeBrightness;

        lightController.StrobeFade = nextEvent.StrobeFade == 1;
        lightController.Easing = Easing.FromID(nextEvent.Easing);
    }

    private static (float time, float offset, BaseLightColorBase evt) GetCurrentOrPreviousEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            var localTime = time - state.StartTime;
            var idx = Array.FindLastIndex(
                state.Box.Events,
                x => x.JsonTime <= localTime && state.StartTime + x.JsonTime < state.EndTime);
            if (idx != -1)
            {
                var evt = state.Box.Events[idx];
                if (evt.UsePrevious != 1)
                {
                    return (state.StartTime + evt.JsonTime,
                        state.Box.BrightnessAffectFirst == 0 && idx == 0 ? 0f : state.Offset, evt);
                }

                var previous = GetPreviousEvent(state, evt.JsonTime);
                return (state.StartTime + evt.JsonTime, previous.offset, previous.evt);
            }

            if (state.Previous == null) return (-1f, 0f, null);
            state = state.Previous;
        }
    }

    private static (float time, float offset, BaseLightColorBase evt) GetPreviousEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            var localTime = time - state.StartTime;
            var idx = Array.FindLastIndex(
                state.Box.Events,
                x => x.JsonTime < localTime && state.StartTime + x.JsonTime < state.EndTime);
            if (idx != -1)
            {
                var evt = state.Box.Events[idx];
                return (state.StartTime + evt.JsonTime,
                    state.Box.BrightnessAffectFirst == 0 && idx == 0 ? 0f : state.Offset, evt);
            }

            if (state.Previous == null) return (-1f, 0f, null);
            state = state.Previous;
        }
    }

    private static (float time, float offset, BaseLightColorBase evt) GetNextEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            var localTime = time - state.StartTime;
            var idx = Array.FindIndex(
                state.Box.Events,
                x => x.JsonTime > localTime && state.StartTime + x.JsonTime < state.EndTime);
            if (idx != -1)
            {
                var evt = state.Box.Events[idx];
                return (state.StartTime + evt.JsonTime,
                    state.Box.BrightnessAffectFirst == 0 && idx == 0 ? 0f : state.Offset, evt);
            }

            if (state.Next == null) return (-1f, 0f, null);
            state = state.Next;
        }
    }

    public override void BuildFromData(IEnumerable<BaseLightColorEventBoxGroup<BaseLightColorEventBox>> dataList)
    {
        Initialize();
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data)
    {
        var taken = new HashSet<int>();
        foreach (var box in data.Boxes.Where(b => b.Events.Length > 0))
        {
            foreach (var (element, durationOrder, distributionOrder) in IndexFilterHelper.Convert(
                box.IndexFilter,
                lightsByGroupAndId[data.ID].Count))
            {
                if (!taken.Add(element)) continue;

                var state = new LightColorGroupStateData(data) { Box = box, };
                var offset = 0f;
                if (box.BeatDistributionType == (int)DistributionType.Wave)
                {
                    var durationNorm = durationOrder / (float)(lightsByGroupAndId[data.ID].Count - 1);
                    offset = Mathf.Max(
                            0f,
                            box.BeatDistribution - box.Events.Max(x => x.JsonTime))
                        * durationNorm;
                }
                else if (box.BeatDistributionType == (int)DistributionType.Step)
                    offset = box.BeatDistribution * durationOrder;

                var distributionOffset = 0f;
                if (box.BrightnessDistributionType == (int)DistributionType.Wave)
                {
                    var distributionNorm = distributionOrder / (float)(lightsByGroupAndId[data.ID].Count - 1);
                    distributionOffset = box.BrightnessDistribution * distributionNorm;
                }
                else if (box.BrightnessDistributionType == (int)DistributionType.Step)
                    distributionOffset = box.BrightnessDistribution * distributionOrder;

                state.StartTime = data.SongBpmTime + offset;
                state.Offset = distributionOffset;

                var container = lightsByGroupAndId[data.ID][element].container;
                HandleInsertState(container, state);
            }
        }
    }

    protected override void OnInsertUpdateToPreviousState(
        LightColorGroupStateData newState,
        LightColorGroupStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.Next = newState;
    }

    protected override void OnInsertUpdateToNextState(
        LightColorGroupStateData newState,
        LightColorGroupStateData nextState)
    {
        base.OnInsertUpdateToNextState(newState, nextState);
        nextState.Previous = newState;
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        LightColorGroupStateData newState,
        LightColorGroupStateData prevState,
        LightColorGroupStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        newState.Previous = prevState;
        newState.Next = nextState;
    }

    public override void RemoveData(
        BaseLightColorEventBoxGroup<BaseLightColorEventBox> data,
        BaseLightColorEventBoxGroup<BaseLightColorEventBox> original) =>
        throw new System.NotImplementedException();

    protected override LightColorGroupStateData CreateState(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) =>
        new(data);
}

public class LightColorGroupStateData : StateData<BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
{
    public float Offset;

    public LightColorGroupStateData Previous;
    public LightColorGroupStateData Next;

    public BaseLightColorEventBox Box;

    public LightColorGroupStateData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) : base(data)
    {
    }
}
