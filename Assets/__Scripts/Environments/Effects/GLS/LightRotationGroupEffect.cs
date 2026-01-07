using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightRotationGroupEffect : EventGroupEffect<
    LightRotationGroupStateData,
    LightRotationEventStateData,
    BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>,
    BaseLightRotationEventBox,
    BaseLightRotationBase>
{
    [SerializeField] private List<TransformEntry> transformEntries = new();

    private readonly Dictionary<(Axis axis, int index), LightRotationGroupContainer>
        idToContainer = new();

    public void Register(int id, Axis axis, bool mirrored, Transform tr) =>
        transformEntries.Add(new() { ID = id, Transform = tr, Axis = axis, Mirrored = mirrored });

    public void Unregister(int id, Axis axis) => transformEntries.RemoveAll(e => e.ID == id && e.Axis == axis);

    public void Unregister(Transform tr) => transformEntries.RemoveAll(e => e.Transform == tr);

    public override void Initialize()
    {
        idToContainer.Clear();
        foreach (var entry in transformEntries)
        {
            if (idToContainer.ContainsKey((entry.Axis, entry.ID))) continue;

            idToContainer[(entry.Axis, entry.ID)] = new(entry.Transform, entry.Axis, entry.Mirrored);
            var container = idToContainer[(entry.Axis, entry.ID)];

            var startEvent = new LightRotationEventStateData(new BaseLightRotationBase(), short.MinValue);
            var endEvent = new LightRotationEventStateData(
                new BaseLightRotationBase { UsePrevious = 1 },
                float.MaxValue);
            container.EventContainer.GenerateChunk(Atsc);

            startEvent.EndTime = endEvent.StartTime;
            startEvent.Next = endEvent;
            endEvent.Previous = startEvent;

            container.EventContainer.Chunks[0].Add(startEvent);
            container.EventContainer.Chunks[^1].Add(endEvent);

            var start = CreateState(new() { songBpmTime = short.MinValue, JsonTime = short.MinValue });
            start.Box = new BaseLightRotationEventBox
            {
                Axis = (int)entry.Axis,
                IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                Events = Array.Empty<BaseLightRotationBase>()
            };
            start.LocalJsonTime = start.StartTime;

            var end = CreateState(new() { songBpmTime = float.MaxValue, JsonTime = float.MaxValue });
            end.Box = new BaseLightRotationEventBox
            {
                Axis = (int)entry.Axis,
                IndexFilter = new() { Type = (int)IndexFilterType.Division, Param0 = 1 },
                Events = Array.Empty<BaseLightRotationBase>()
            };
            end.LocalJsonTime = end.StartTime = end.EndTime;

            RegenerateEvents(start, float.MaxValue);
            RegenerateEvents(end, float.MaxValue);

            InitializeStates(container.GroupContainer, start, end);

            container.GroupContainer.SetStateAt(0);
            container.EventContainer.SetStateAt(0);
        }
    }

    public override void Refresh()
    {
        foreach (var container in idToContainer.Values.Where(c => c is not null))
        {
            // if (!container.EventContainer.IsCurrentOrFindState(time, Atsc.IsPlaying)) UpdateObject(container);
            // if (container.Tween.UpdateTime(time))
            SetRotation(container.Transform, container.Tween.Current, container.Axis, container.Mirrored);
        }
    }

    public override void UpdateTime(float time)
    {
        foreach (var container in idToContainer.Values.Where(c => c is not null))
        {
            if (!container.EventContainer.IsCurrentOrFindState(time, Atsc.IsPlaying)) UpdateObject(container);
            if (container.Tween.UpdateTime(time))
                SetRotation(container.Transform, container.Tween.Current, container.Axis, container.Mirrored);
        }
    }

    private void UpdateObject(LightRotationGroupContainer container)
    {
        var state = container.EventContainer.CurrentState;
        var tween = container.Tween;

        tween.StartTime = state.StartTime;
        var startState = (LightRotationEventStateData)(state.UsePrevious ? state.Previous : state);
        var startAngle = Mathf.Repeat(startState.Rotation, 360f);

        tween.EndTime = state.EndTime;
        var endState = (LightRotationEventStateData)(state.Next.UsePrevious ? startState : state.Next);
        var endAngle = Mathf.Repeat(endState.Rotation, 360f);

        var targetAngle = ComputeTargetAngle(startAngle, endAngle, endState.Loop, endState.Direction);

        tween.StartValue = startAngle;
        tween.EndValue = targetAngle;
        tween.Easing = Easing.FromID((int)endState.EaseType);
    }

    private static void SetRotation(Transform tr, float rotation, Axis axis, bool mirrored)
    {
        if (mirrored) rotation *= -1f;
        tr.localRotation = axis switch
        {
            Axis.X => Quaternion.AngleAxis(rotation, Vector3.right),
            Axis.Y => Quaternion.AngleAxis(rotation, Vector3.up),
            Axis.Z => Quaternion.AngleAxis(rotation, Vector3.forward),
            _ => Quaternion.identity,
        };
    }

    private static float ComputeTargetAngle(
        float startAngle,
        float targetAngle,
        int loopCount,
        LightRotationDirection rotationOrientation)
    {
        var angle = 0f;
        var loopAngle = 0f;
        var delta = Mathf.DeltaAngle(startAngle, targetAngle);
        switch (rotationOrientation)
        {
            case LightRotationDirection.Automatic:
                angle = startAngle + delta;
                loopAngle = Mathf.Sign(delta) * loopCount * 360f;
                break;
            case LightRotationDirection.Clockwise:
                angle = !(delta >= 0f) ? startAngle + delta + 360f : startAngle + delta;
                loopAngle = loopCount * 360f;
                break;
            case LightRotationDirection.CounterClockwise:
                angle = !(delta <= 0f) ? startAngle + delta - 360f : startAngle + delta;
                loopAngle = -loopCount * 360f;
                break;
        }

        return angle + loopAngle;
    }

    protected override LightRotationGroupStateData CreateState(
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) =>
        new(data);

    protected override Axis GetAxis(BaseLightRotationEventBox box) => (Axis)box.Axis;

    protected override
        StateChunksContainer<LightRotationGroupStateData, BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
        GetGroupContainer((Axis axis, int element) key)
    {
        return idToContainer.TryGetValue(key, out var value)
            ? value?.GroupContainer
            : null;
    }

    protected override StateChunksContainer<LightRotationEventStateData, BaseLightRotationBase> GetEventContainer(
        (Axis axis, int element) key)
    {
        return idToContainer.TryGetValue(key, out var value)
            ? value?.EventContainer
            : null;
    }

    protected override
        IEnumerable<(
            StateChunksContainer<LightRotationGroupStateData, BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
            groupContainer, StateChunksContainer<LightRotationEventStateData, BaseLightRotationBase> eventContainer)>
        GetContainers() =>
        idToContainer.Values.Select(x => (x.GroupContainer, x.EventContainer));

    protected override int GetEventCount(BaseLightRotationEventBox box) => box.Events.Length;

    protected override float GetLastEventTime(BaseLightRotationEventBox box) => box.Events[^1].JsonTime;

    protected override float GetDistribution(
        IndexFilterHelper.IndexFilter indexFilter,
        BaseLightRotationEventBox box,
        int order) =>
        DistributionHelper.GetValueStep(
            order,
            DistributionHelper.GetDistributionCount(indexFilter),
            (DistributionType)box.RotationDistributionType,
            box.RotationDistribution,
            (EaseType)box.Easing);

    protected override LightRotationEventStateData[] GenerateEvents(
        LightRotationGroupStateData state,
        float distributionOffset,
        float maxJsonTime) =>
        state
            .Box
            .Events
            .Select((x, i) =>
                {
                    var distribution = state.Box.RotationAffectFirst != 1 && i == 0 ? 0f : distributionOffset;
                    var d = new LightRotationEventStateData(
                        x,
                        (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                            state.Base.JsonTime + x.JsonTime + (state.DurationOrder * state.BeatStep)),
                        state.Box.Flip == 1 ? -1 : 1,
                        distribution);
                    return d;
                }
            )
            .Where(x => state.Base.JsonTime + x.Base.JsonTime + (state.DurationOrder * state.BeatStep) <= maxJsonTime)
            .ToArray();
}

public class LightRotationGroupStateData : EventGroupStateData<
    BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>,
    BaseLightRotationEventBox,
    BaseLightRotationBase>
{
    public LightRotationGroupStateData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) : base(data)
    {
    }
}

[Serializable]
public class LightRotationEventStateData : EventGroupEventStateData<BaseLightRotationBase>
{
    public readonly float Rotation;
    public readonly LightRotationDirection Direction;
    public readonly int Loop;

    public LightRotationEventStateData(
        BaseLightRotationBase data,
        float startTime,
        int direction = 1,
        float offset = 0f) : base(data, startTime, data.EaseType, data.UsePrevious)
    {
        var additionalLoop = Mathf.FloorToInt(Mathf.Abs(offset) / 360f);
        offset = Mathf.Abs(offset) % 360f * Mathf.Sign(offset);

        Rotation = (offset + data.Rotation) * direction;
        Direction = (LightRotationDirection)data.Direction;
        Loop = data.Loop + additionalLoop;
    }
}

public record LightRotationGroupContainer : EventGroupContainer<
    LightRotationGroupStateData,
    LightRotationEventStateData,
    BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>,
    BaseLightRotationEventBox,
    BaseLightRotationBase>
{
    public readonly Transform Transform;
    public readonly Axis Axis;
    public readonly bool Mirrored;

    public readonly FloatTween Tween = new();

    public LightRotationGroupContainer(Transform transform, Axis axis, bool mirrored)
    {
        Transform = transform;
        Axis = axis;
        Mirrored = mirrored;
    }
}
