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

    [SerializeField] private LightControllerContainer[] idToContainer = Array.Empty<LightControllerContainer>();

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
            var state = container.EventContainer.CurrentState;

            foreach (var controller in container.Lights)
            {
                controller.StartColor = ColorScheme.GetColorFrom((LightColor)state.Base.Color, false);
                controller.EndColor = ColorScheme.GetColorFrom((LightColor)state.Next.Base.Color, false);
                controller.UpdateBoostState(boost);
            }
        }
    }

    public override void Initialize()
    {
        idToContainer = new LightControllerContainer[Count];
        foreach (var entry in lightEntries)
        {
            if (idToContainer[entry.ID] is null)
            {
                var groupContainer =
                    new StateChunksContainer<LightColorGroupStateData,
                        BaseLightColorEventBoxGroup<BaseLightColorEventBox>>();
                var eventContainer = new StateChunksContainer<LightColorEventStateData, BaseLightColorBase>();
                idToContainer[entry.ID] = new(groupContainer, eventContainer);

                var startEvent = new LightColorEventStateData(new BaseLightColorBase(), short.MinValue);
                var endEvent = new LightColorEventStateData(
                    new BaseLightColorBase { UsePrevious = 1 },
                    float.MaxValue);
                eventContainer.GenerateChunk(Atsc);

                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;
                startEvent.EndTime = endEvent.StartTime;

                eventContainer.Chunks[0].Add(startEvent);
                eventContainer.Chunks[^1].Add(endEvent);

                var start = CreateState(new() { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                start.LocalStartTime = start.StartTime;

                var end = CreateState(new() { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseLightColorEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<BaseLightColorBase>()
                };
                end.LocalStartTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                eventContainer.SetStateAt(0);

                InitializeStates(groupContainer, start, end);
            }

            idToContainer[entry.ID].Lights.Add(entry.Controller);
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var container in idToContainer) UpdateObject(container, time);
    }

    private void UpdateObject(LightControllerContainer container, float time)
    {
        var updateRequired = container.EventContainer.IsCurrentOrFindState(time, Atsc.IsPlaying);
        foreach (var controller in container.Lights)
        {
            if (updateRequired)
            {
                var state = container.EventContainer.CurrentState;

                controller.StartTimeAlpha = controller.StartTimeColor = state.StartTime;
                var startState = state;
                while (startState.Base.UsePrevious == 1) startState = startState.Previous;
                controller.StartAlpha = startState.Base.Brightness;
                controller.StartColor = ColorScheme.GetColorFrom((LightColor)startState.Base.Color, false);
                controller.StartStrobeFrequency = startState.Base.Frequency;
                controller.StartStrobeBrightness = startState.Base.StrobeBrightness;

                controller.EndTimeAlpha = controller.EndTimeColor = state.EndTime;
                var endState = state;
                if (endState.Base.UsePrevious == 1) endState = state;
                controller.EndAlpha = endState.Next.Base.Brightness;
                controller.EndColor = ColorScheme.GetColorFrom((LightColor)endState.Next.Base.Color, false);
                controller.EndStrobeFrequency = endState.Next.Base.Frequency;
                controller.EndStrobeBrightness = endState.Next.Base.StrobeBrightness;

                controller.StrobeFade = endState.Next.Base.StrobeFade == 1;
                controller.Easing = Easing.FromID(endState.Next.Base.Easing);
            }

            controller.UpdateTime(time);
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
            var beatStep = box.Events.Length > 0
                ? DistributionHelper.GetBeatStep(
                    DistributionHelper.GetCount(indexFilter),
                    (DistributionType)box.BeatDistributionType,
                    box.BeatDistribution,
                    box.Events.Last().JsonTime)
                : 0f;
            foreach (var (element, durationOrder, distributionOrder) in indexFilter)
            {
                if (!taken.Add(element)) continue;

                var state = new LightColorGroupStateData(data);

                state.StartTime = data.SongBpmTime;
                state.LocalStartTime =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                        data.JsonTime + (beatStep * durationOrder));

                state.BeatStep = beatStep;
                state.Box = box;

                state.ElementID = element;
                state.DurationOrder = durationOrder;
                state.DistributionOrder = distributionOrder;

                var container = idToContainer[element].GroupContainer;
                HandleInsertState(container, state);
            }
        }
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        LightColorGroupStateData newState,
        LightColorGroupStateData prevState,
        LightColorGroupStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);

        RemoveEvents(prevState);
        RemoveEvents(nextState);

        RegenerateEvents(prevState, newState.LocalStartTime);
        RegenerateEvents(newState, nextState.LocalStartTime);
    }

    private void RemoveEvents(LightColorGroupStateData state)
    {
        foreach (var evt in state.Events) HandleRemoveState(idToContainer[state.ElementID].EventContainer, evt);
    }

    private LightColorEventStateData HandleRemoveState(
        StateChunksContainer<LightColorEventStateData, BaseLightColorBase> container,
        LightColorEventStateData stateToRemove)
    {
        var (_, currChunk) = container.GetChunk(stateToRemove.StartTime);
        var (_, _, prevState) = container.GetPreviousStateFrom(stateToRemove);
        var (_, _, nextState) = container.GetNextStateFrom(stateToRemove);

        OnRemoveUpdatePreviousAndNextState(stateToRemove, prevState, nextState);
        currChunk.Remove(stateToRemove);

        return stateToRemove;
    }

    private void OnRemoveUpdatePreviousAndNextState(
        LightColorEventStateData stateToRemove,
        LightColorEventStateData prevState,
        LightColorEventStateData nextState)
    {
        prevState.EndTime = nextState.StartTime;

        prevState.Next = nextState;
        nextState.Previous = prevState;
    }

    private void RegenerateEvents(LightColorGroupStateData state, float maxTime)
    {
        var indexFilter = IndexFilterHelper.Convert(state.Box.IndexFilter, Count);
        var distributionOffset = DistributionHelper.GetValueStep(
            state.DistributionOrder,
            DistributionHelper.GetCount(indexFilter),
            (DistributionType)state.Box.BrightnessDistributionType,
            state.Box.BrightnessDistribution,
            (EaseType)state.Box.Easing);
        state.Events = state
            .Box
            .Events.Select((x, i) =>
                {
                    var affected = !(i == 0 && state.Box.BrightnessAffectFirst != 1);
                    var d = new LightColorEventStateData(
                        x,
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                            state.Base.JsonTime + x.JsonTime + (state.BeatStep * state.DurationOrder)),
                        affected ? distributionOffset : 0f);
                    return d;
                }
            )
            .Where(x => x.StartTime <= maxTime)
            .ToArray();
        foreach (var data in state.Events) HandleInsertState(idToContainer[state.ElementID].EventContainer, data);
    }

    private void HandleInsertState(
        StateChunksContainer<LightColorEventStateData, BaseLightColorBase> container,
        LightColorEventStateData newState)
    {
        var (prevChunk, prevIndex, prevState) = container.GetOverlappingStateFrom(newState);
        var (nextChunk, _, nextState) = container.GetNextStateFrom(newState);

        prevState.Next = newState;
        newState.Previous = prevState;
        newState.Next = nextState;
        nextState.Previous = newState;

        prevState.EndTime = newState.StartTime;
        newState.EndTime = nextState.StartTime;

        var (_, chunk) = container.GetChunk(newState.StartTime);
        if (prevChunk != chunk)
            chunk.Insert(0, newState);
        else if (nextChunk != chunk)
            chunk.Add(newState);
        else
            chunk.Insert(prevIndex + 1, newState);
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
    public float LocalStartTime;
    public float BeatStep;

    public int ElementID;
    public int DurationOrder;
    public int DistributionOrder;

    public BaseLightColorEventBox Box;
    public LightColorEventStateData[] Events = Array.Empty<LightColorEventStateData>();

    public LightColorGroupStateData(BaseLightColorEventBoxGroup<BaseLightColorEventBox> data) : base(data)
    {
    }
}

[Serializable]
public class LightColorEventStateData : StateData<BaseLightColorBase>
{
    public LightColorEventStateData Previous;
    public LightColorEventStateData Next;

    // public LightColor StartColor;
    // public float StartBrightness;
    // public EaseType StartEaseType;
    // public bool StartUsePrevious;
    // public int StartStrobeFrequency;
    // public float StartStrobeBrightness;
    // public bool StartStrobeFade;
    //
    // public LightColor EndColor;
    // public float EndBrightness;
    // public EaseType EndEaseType;
    // public bool EndUsePrevious;
    // public int EndStrobeFrequency;
    // public float EndStrobeBrightness;
    // public bool EndStrobeFade;

    public LightColorEventStateData(BaseLightColorBase data, float startTime, float offset = 0f) : base(data)
    {
        StartTime = startTime;
        // StartColor = (LightColor)data.Color;
        // StartBrightness = data.Brightness + offset;
        // StartEaseType = (EaseType)data.Easing;
        // StartUsePrevious = data.UsePrevious == 1;
        // StartStrobeFrequency = data.Frequency;
        // StartStrobeBrightness = data.StrobeBrightness;
        // StartStrobeFade = data.StrobeFade == 1;
    }
}

[Serializable]
public class LightControllerContainer
{
    public StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
        GroupContainer;

    public StateChunksContainer<LightColorEventStateData, BaseLightColorBase> EventContainer;

    public List<BaseLightController> Lights;

    public LightControllerContainer(
        StateChunksContainer<LightColorGroupStateData, BaseLightColorEventBoxGroup<BaseLightColorEventBox>>
            groupContainer,
        StateChunksContainer<LightColorEventStateData, BaseLightColorBase> eventContainer)
    {
        GroupContainer = groupContainer;
        EventContainer = eventContainer;
        Lights = new List<BaseLightController>();
    }
}
