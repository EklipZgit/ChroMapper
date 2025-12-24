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

    [SerializeField] public ColorBoostEffect ColorBoostEffect;
    [SerializeField] public int Count;

    private LightControllerContainer[] idToContainer = Array.Empty<LightControllerContainer>();

    public ColorSchemeSO ColorScheme;

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
        foreach (var container in idToContainer)
        {
            var state = container.Container.CurrentState;

            var (_, currentEvent) = GetCurrentOrPreviousEvent(state, Atsc.CurrentSongBpmTime);
            var nextEvent = GetNextEvent(state, Atsc.CurrentSongBpmTime);

            if (nextEvent.UsePrevious) nextEvent = currentEvent;

            foreach (var controller in container.Lights)
            {
                controller.StartColor = ColorScheme.GetColorFrom(currentEvent.Color, false);
                controller.EndColor = ColorScheme.GetColorFrom(nextEvent.Color, false);
                controller.UpdateBoostState(boost);
            }
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightControllerContainer[Count];
        foreach (var entry in lightEntries)
        {
            if (idToContainer[entry.ID] == null)
            {
                var container =
                    new StateChunksContainer<LightColorGroupStateData,
                        BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();
                var start = CreateState(new());
                var end = CreateState(new() { songBpmTime = float.MaxValue });

                start.Events = new[]
                {
                    new LightColorGroupStateData.LightColorEvent(new BaseLightColorBase(), short.MinValue),
                    new LightColorGroupStateData.LightColorEvent(new BaseLightColorBase(), 0f),
                };
                end.Events = new[]
                {
                    new LightColorGroupStateData.LightColorEvent(
                        new BaseLightColorBase { Easing = (int)EaseType.None },
                        float.MaxValue)
                };

                start.Next = end;
                end.Previous = start;

                InitializeStates(container, start, end);
                idToContainer[entry.ID] = new(container);
            }

            var c = idToContainer[entry.ID];
            c.Lights.Add(entry.Controller);
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var container in idToContainer)
        {
            container.Container.IsCurrentOrFindState(time, Atsc.IsPlaying);
            UpdateObject(container, time);
        }
    }

    private void UpdateObject(LightControllerContainer container, float time)
    {
        var state = container.Container.CurrentState;
        var (currentEventTime, currentEvent) = GetCurrentOrPreviousEvent(state, time);

        var nextEvent = GetNextEvent(state, time);
        if (nextEvent.UsePrevious) nextEvent = currentEvent;

        var requireUpdate = !currentEvent.Equals(container.StartEvent) || !nextEvent.Equals(container.EndEvent);

        foreach (var controller in container.Lights)
        {
            if (requireUpdate)
            {
                controller.StartTimeAlpha = currentEventTime;
                controller.StartTimeColor = currentEventTime;
                controller.StartAlpha = currentEvent.Brightness;
                controller.StartColor = ColorScheme.GetColorFrom(currentEvent.Color, false);
                controller.StartStrobeFrequency = currentEvent.StrobeFrequency;
                controller.StartStrobeBrightness = currentEvent.StrobeBrightness;

                controller.EndTimeAlpha = nextEvent.ActualSongBpmTime;
                controller.EndTimeColor = nextEvent.ActualSongBpmTime;
                controller.EndAlpha = nextEvent.Brightness;
                controller.EndColor = ColorScheme.GetColorFrom(nextEvent.Color, false);
                controller.EndStrobeFrequency = nextEvent.StrobeFrequency;
                controller.EndStrobeBrightness = nextEvent.StrobeBrightness;

                controller.StrobeFade = nextEvent.StrobeFade;
                controller.Easing = Easing.FromID((int)nextEvent.EaseType);
            }

            controller.UpdateTime(time);
        }

        container.StartEvent = currentEvent;
        container.EndEvent = nextEvent;
    }

    private static (float time, LightColorGroupStateData.LightColorEvent evt) GetCurrentOrPreviousEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            if (!state.Skip)
            {
                for (var i = state.Events.Length - 1; i >= 0; i--)
                {
                    var evt = state.Events[i];
                    if (!(evt.ActualSongBpmTime <= time) || !(evt.ActualSongBpmTime < state.EndTime)) continue;

                    if (!evt.UsePrevious) return (evt.ActualSongBpmTime, evt);
                    var previous = GetPreviousEvent(state, evt.ActualSongBpmTime);
                    return (evt.ActualSongBpmTime, previous);
                }
            }

            // if (state.Previous == null) return (-1f, null);
            state = state.Previous;
        }
    }

    private static LightColorGroupStateData.LightColorEvent GetPreviousEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            if (!state.Skip)
            {
                for (var i = state.Events.Length - 1; i >= 0; i--)
                {
                    var evt = state.Events[i];
                    if (!(evt.ActualSongBpmTime < time) || !(evt.ActualSongBpmTime <= state.EndTime) || evt.UsePrevious)
                        continue;
                    return evt;
                }
            }

            // if (state.Previous == null) return (-1f, null);
            state = state.Previous;
        }
    }

    private static LightColorGroupStateData.LightColorEvent GetNextEvent(
        LightColorGroupStateData state,
        float time)
    {
        while (true)
        {
            if (!state.Skip)
            {
                for (var i = 0; i < state.Events.Length; i++)
                {
                    var evt = state.Events[i];
                    if (!(evt.ActualSongBpmTime > time) || !(evt.ActualSongBpmTime <= state.EndTime)) continue;
                    return evt;
                }
            }

            // if (state.Next == null) return state.Events[^1];
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
            var indexFilter = IndexFilterHelper.Convert(box.IndexFilter, Count);
            var timeStep = DistributionHelper.GetBeatStep(
                DistributionHelper.GetCount(indexFilter),
                (DistributionType)box.BeatDistributionType,
                box.BeatDistribution,
                box.Events.Last().JsonTime);
            foreach (var (element, durationOrder, distributionOrder) in indexFilter)
            {
                if (!taken.Add(element)) continue;

                var state = new LightColorGroupStateData(data);

                var distributionOffset = DistributionHelper.GetValueStep(
                    distributionOrder,
                    DistributionHelper.GetCount(indexFilter),
                    (DistributionType)box.BrightnessDistributionType,
                    box.BrightnessDistribution,
                    (EaseType)box.Easing);

                state.StartTime =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                        data.JsonTime + (timeStep * durationOrder));
                state.Events = box
                    .Events.Select((x, i) =>
                        {
                            var affected = !(i == 0 && box.BrightnessAffectFirst != 1);
                            var d = new LightColorGroupStateData.LightColorEvent(
                                x,
                                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                                    data.JsonTime + x.JsonTime + (timeStep * durationOrder)),
                                affected ? distributionOffset : 0f);
                            return d;
                        }
                    )
                    .ToArray();

                var container = idToContainer[element].Container;
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
        nextState.Skip = nextState.Base.SongBpmTime < newState.StartTime;
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
    public bool Skip;

    public LightColorGroupStateData Previous;
    public LightColorGroupStateData Next;

    public LightColorEvent[] Events;

    public LightColorGroupStateData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) : base(data)
    {
    }

    public readonly struct LightColorEvent : IEquatable<LightColorEvent>
    {
        private static long id;
        private readonly long instanceId;

        public readonly float ActualSongBpmTime;

        public readonly LightColor Color;
        public readonly float Brightness;
        public readonly EaseType EaseType;
        public readonly bool UsePrevious;
        public readonly int StrobeFrequency;
        public readonly float StrobeBrightness;
        public readonly bool StrobeFade;

        public LightColorEvent(BaseLightColorBase data, float time, float offset = 0f)
        {
            instanceId = id++;
            ActualSongBpmTime = time;
            Color = (LightColor)data.Color;
            Brightness = data.Brightness + offset;
            EaseType = (EaseType)data.Easing;
            UsePrevious = data.UsePrevious == 1;
            StrobeFrequency = data.Frequency;
            StrobeBrightness = data.StrobeBrightness;
            StrobeFade = data.StrobeFade == 1;
        }

        public bool Equals(LightColorEvent other) => instanceId == other.instanceId;

        public override bool Equals(object obj) => obj is LightColorEvent other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(
                ActualSongBpmTime,
                (int)Color,
                Brightness,
                (int)EaseType,
                UsePrevious,
                StrobeFrequency,
                StrobeBrightness,
                StrobeFade);
    }
}

public class LightControllerContainer
{
    public readonly StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
        Container;

    public readonly List<BaseLightController> Lights;

    public LightColorGroupStateData.LightColorEvent StartEvent;
    public LightColorGroupStateData.LightColorEvent EndEvent;

    public LightControllerContainer(
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>> container)
    {
        Container = container;
        Lights = new List<BaseLightController>();
    }
}
