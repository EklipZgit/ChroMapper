using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    FloatFxGroupEffect : EventGroupEffect<
    FloatFxGroupStateData,
    FloatFxEventStateData,
    BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>,
    BaseVfxEventEventBox,
    FloatFxEventBase>
{
    [SerializeField] public bool Trigger;

    [SerializeField] private List<FxEntry> fxEntries = new();
    private FloatFxGroupContainer[] idToContainer = Array.Empty<FloatFxGroupContainer>();
    private FloatFxGroupContainer[] activeContainers = Array.Empty<FloatFxGroupContainer>();

    public void Register(int id, FxTarget target) => fxEntries.Add(new() { ID = id, Target = target });
    public void Unregister(int id) => fxEntries.Remove(fxEntries.Find(e => e.ID == id));

    public override void Initialize()
    {
        idToContainer = new FloatFxGroupContainer[Count];
        foreach (var entry in fxEntries)
        {
            if (entry.ID >= Count)
            {
                Debug.LogError(
                    $"{entry}:{entry.ID} ID is larger than supported by group {ID}:{Count}, was the controller modified?");
                continue;
            }

            if (idToContainer[entry.ID] is null)
            {
                idToContainer[entry.ID] = new(entry.ID, entry.Target);
                var container = idToContainer[entry.ID];

                var startEvent = new FloatFxEventStateData(new FloatFxEventBase(), short.MinValue);
                var endEvent = new FloatFxEventStateData(
                    new FloatFxEventBase { UsePrevious = 1 },
                    float.MaxValue);
                container.EventContainer.Resize(Atsc.SongAudioSource.clip.length);

                startEvent.EndTime = endEvent.StartTime;
                startEvent.Next = endEvent;
                endEvent.Previous = startEvent;

                container.EventContainer.AddState(startEvent);
                container.EventContainer.AddState(endEvent);

                var start = CreateState(new() { songBpmTime = short.MinValue, JsonTime = short.MinValue });
                start.Box = new BaseVfxEventEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<FloatFxEventBase>()
                };
                start.LocalJsonTime = start.StartTime;

                var end = CreateState(new() { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
                end.Box = new BaseVfxEventEventBox
                {
                    IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                    Events = Array.Empty<FloatFxEventBase>()
                };
                end.LocalJsonTime = end.StartTime = end.EndTime;

                RegenerateEvents(start, float.MaxValue);
                RegenerateEvents(end, float.MaxValue);

                container.EventContainer.SetStateAt(0);

                InitializeStates(container.GroupContainer, start, end);
            }
        }

        activeContainers = idToContainer.Where(x => x is not null && x.Target != null).ToArray();
    }

    public override void Refresh()
    {
        foreach (var container in activeContainers)
        {
            if (Trigger)
                container.Target.TriggerValue(ID, container.Id, container.EventContainer.CurrentState.Value);
            else
                container.Target.SetValue(ID, container.Id, container.Tween.Current);
        }
    }

    public override void UpdateTime(float time)
    {
        foreach (var container in activeContainers)
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, Atsc.IsPlaying))
            {
                UpdateObject(container);
                if (Trigger)
                    container.Target.TriggerValue(ID, container.Id, container.EventContainer.CurrentState.Value);
            }

            if (!Trigger && !container.Tween.UpdateTime(time))
                container.Target.SetValue(ID, container.Id, container.EventContainer.CurrentState.Value);
        }
    }

    private void UpdateObject(FloatFxGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTime = state.StartTime;
        var startState = (FloatFxEventStateData)(state.UsePrevious ? state.Previous : state);
        tween.StartValue = startState.Value;

        tween.EndTime = state.EndTime;
        var endState = (FloatFxEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        tween.EndValue = endState.Value;

        tween.Easing = Easing.FromID(endState.Base.Easing);
    }

    protected override FloatFxGroupStateData CreateState(BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> data) =>
        new(data);

    protected override Axis GetAxis(BaseVfxEventEventBox box) => Axis.X;

    protected override
        StateChunksContainer<FloatFxGroupStateData, BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>>
        GetGroupContainer((Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<FloatFxEventStateData, FloatFxEventBase> GetEventContainer(
        (Axis axis, int element) key)
    {
        var id = key.element;
        return 0 <= id && id < idToContainer.Length
            ? idToContainer[id]?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(StateChunksContainer<FloatFxGroupStateData, BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>>
            groupContainer, StateChunksContainer<FloatFxEventStateData, FloatFxEventBase> eventContainer)>
        GetContainers() =>
        idToContainer.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseVfxEventEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseVfxEventEventBox box) => box.Events[^1].JsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseVfxEventEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.VfxDistributionType,
            box.VfxDistribution,
            (EaseType)box.Easing);

    protected override FloatFxEventStateData[] GenerateEvents(
        FloatFxGroupStateData state,
        float distributionOffset,
        float maxJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
                {
                    var affected = !(i == 0 && state.Box.VfxAffectFirst != 1);
                    var d = new FloatFxEventStateData(
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

public class FloatFxGroupStateData : EventGroupStateData<
    BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>,
    BaseVfxEventEventBox,
    FloatFxEventBase>
{
    public FloatFxGroupStateData(BaseVfxEventEventBoxGroup<BaseVfxEventEventBox> data) : base(data)
    {
    }
}

[Serializable]
public class FloatFxEventStateData : EventGroupEventStateData<FloatFxEventBase>
{
    public readonly float Value;

    public FloatFxEventStateData(FloatFxEventBase data, float startTime, float offset = 0f) : base(
        data,
        startTime,
        data.Easing,
        data.UsePrevious) =>
        Value = data.Value + offset;
}

public record FloatFxGroupContainer : EventGroupContainer<
    FloatFxGroupStateData,
    FloatFxEventStateData,
    BaseVfxEventEventBoxGroup<BaseVfxEventEventBox>,
    BaseVfxEventEventBox,
    FloatFxEventBase>
{
    public readonly FloatTween Tween = new();
    public readonly int Id;
    public readonly FxTarget Target;

    public FloatFxGroupContainer(int id, FxTarget target)
    {
        Id = id;
        Target = target;
    }
}

[Serializable]
public class FxEntry
{
    public int ID;
    public FxTarget Target;
}
