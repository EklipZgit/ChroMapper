using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class
    LightRotationGroupEffect : StateManager<LightRotationGroupStateData,
    BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
{
    [SerializeField] private List<TransformEntry> transformEntries = new();
    [SerializeField] public int Count;

    private readonly Dictionary<(Axis axis, int index), RotationTransformContainer>
        idToContainer = new();

    private readonly List<RotationTransformContainer> transformContainers = new();

    public void Register(int id, Axis axis, bool mirrored, Transform tr) =>
        transformEntries.Add(new() { ID = id, Transform = tr, Axis = axis, Mirrored = mirrored });

    public void Unregister(int id, Axis axis) => transformEntries.RemoveAll(e => e.ID == id && e.Axis == axis);

    public void Unregister(Transform tr) => transformEntries.RemoveAll(e => e.Transform == tr);

    public override void Initialize()
    {
        idToContainer.Clear();
        transformContainers.Clear();
        foreach (var entry in transformEntries)
        {
            if (idToContainer.ContainsKey((entry.Axis, entry.ID))) continue;

            var container = new RotationTransformContainer(
                entry.Transform,
                entry.Axis,
                entry.Mirrored
            );

            var start = CreateState(new());
            var end = CreateState(new());
            start.Events = new[]
            {
                new LightRotationGroupStateData.LightRotationEvent(new BaseLightRotationBase(), short.MinValue),
                new LightRotationGroupStateData.LightRotationEvent(new BaseLightRotationBase(), 0f)
            };
            end.Events = new[]
            {
                new LightRotationGroupStateData.LightRotationEvent(
                    new BaseLightRotationBase { EaseType = (int)EaseType.None },
                    float.MaxValue)
            };
            InitializeStates(container.Container, start, end);

            idToContainer[(entry.Axis, entry.ID)] = container;
            transformContainers.Add(container);
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var container in transformContainers)
        {
            container.Container.IsCurrentOrFindState(time, Atsc.IsPlaying);
            UpdateObject(container, time);
        }
    }

    private void UpdateObject(RotationTransformContainer container, float time)
    {
        var state = container.Container.CurrentState;

        var (currentEventTime, currentEvent) = GetCurrentOrPreviousEvent(state, time);
        var nextEvent = GetNextEvent(state, currentEventTime);

        if (nextEvent.UsePrevious) nextEvent = currentEvent;

        var startAngle = Mathf.Repeat(currentEvent.Rotation, 360f);

        var targetAngle = Mathf.Repeat(nextEvent.Rotation, 360f);

        var nextAngle = ComputeTargetAngle(
            startAngle,
            targetAngle,
            nextEvent.Loop,
            nextEvent.Direction);

        var tNorm = Mathf.InverseLerp(currentEventTime, nextEvent.ActualSongBpmTime, time);
        var easing = Easing.FromID((int)nextEvent.EaseType);

        var val = Mathf.LerpUnclamped(startAngle, nextAngle, easing(tNorm));

        SetRotation(container.Transform, Mathf.Repeat(val, 360f), container.Axis, container.Mirrored);
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

    private static (float time, LightRotationGroupStateData.LightRotationEvent evt) GetCurrentOrPreviousEvent(
        LightRotationGroupStateData state,
        float time)
    {
        while (true)
        {
            var idx = Array.FindLastIndex(
                state.Events,
                x => x.ActualSongBpmTime <= time && x.ActualSongBpmTime < state.EndTime);
            if (idx != -1)
            {
                var evt = state.Events[idx];
                if (!evt.UsePrevious) return (evt.ActualSongBpmTime, evt);

                var previous = GetPreviousEvent(state, evt.ActualSongBpmTime);
                return (evt.ActualSongBpmTime, previous);
            }

            // if (state.Previous == null) return (-1f, null);
            state = state.Previous;
        }
    }

    private static LightRotationGroupStateData.LightRotationEvent GetPreviousEvent(
        LightRotationGroupStateData state,
        float time)
    {
        while (true)
        {
            var idx = Array.FindLastIndex(
                state.Events,
                x => x.ActualSongBpmTime < time && x.ActualSongBpmTime <= state.EndTime && !x.UsePrevious);
            if (idx != -1)
            {
                var evt = state.Events[idx];
                return evt;
            }

            // if (state.Previous == null) return (-1f, null);
            state = state.Previous;
        }
    }

    private static LightRotationGroupStateData.LightRotationEvent GetNextEvent(
        LightRotationGroupStateData state,
        float time)
    {
        while (true)
        {
            var idx = Array.FindIndex(
                state.Events,
                x => x.ActualSongBpmTime > time && x.ActualSongBpmTime <= state.EndTime);
            if (idx != -1)
            {
                var evt = state.Events[idx];
                return evt;
            }

            // if (state.Next == null) return (-1f, null);
            state = state.Next;
        }
    }

    public override void BuildFromData(IEnumerable<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>> dataList)
    {
        Initialize();
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data)
    {
        var taken = new HashSet<(int, Axis)>();
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
                if (!taken.Add((element, (Axis)box.Axis))) continue;
                if (!idToContainer.ContainsKey(((Axis)box.Axis, element))) continue;

                var state = new LightRotationGroupStateData(data);

                var distributionOffset = DistributionHelper.GetValueStep(
                    distributionOrder,
                    DistributionHelper.GetCount(indexFilter),
                    (DistributionType)box.RotationDistributionType,
                    box.RotationDistribution,
                    (EaseType)box.Easing);

                state.StartTime = data.SongBpmTime;
                state.Events = box
                    .Events.Select((x, i) =>
                        {
                            var affected = !(i == 0 && box.RotationAffectFirst != 1);
                            var d = new LightRotationGroupStateData.LightRotationEvent(
                                x,
                                (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                                    data.JsonTime + x.JsonTime + (timeStep * durationOrder)),
                                box.Flip == 1 ? -1f : 1f,
                                affected ? distributionOffset : 0f);
                            return d;
                        }
                    )
                    .ToArray();

                var container = idToContainer[((Axis)box.Axis, element)].Container;
                HandleInsertState(container, state);
            }
        }
    }

    protected override void OnInsertUpdateToPreviousState(
        LightRotationGroupStateData newState,
        LightRotationGroupStateData prevState)
    {
        base.OnInsertUpdateToPreviousState(newState, prevState);
        prevState.Next = newState;
    }

    protected override void OnInsertUpdateToNextState(
        LightRotationGroupStateData newState,
        LightRotationGroupStateData nextState)
    {
        base.OnInsertUpdateToNextState(newState, nextState);
        nextState.Previous = newState;
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        LightRotationGroupStateData newState,
        LightRotationGroupStateData prevState,
        LightRotationGroupStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        newState.Previous = prevState;
        newState.Next = nextState;
    }

    public override void RemoveData(
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data,
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> original) =>
        throw new NotImplementedException();

    protected override LightRotationGroupStateData CreateState(
        BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) =>
        new(data);
}

public class LightRotationGroupStateData : StateData<BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
{
    public LightRotationGroupStateData Previous;
    public LightRotationGroupStateData Next;

    public LightRotationEvent[] Events;

    public LightRotationGroupStateData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) : base(data)
    {
    }

    public readonly struct LightRotationEvent
    {
        public readonly float ActualSongBpmTime;

        public readonly float Rotation;
        public readonly LightRotationDirection Direction;
        public readonly EaseType EaseType;
        public readonly int Loop;
        public readonly bool UsePrevious;

        public LightRotationEvent(BaseLightRotationBase data, float time, float direction = 1f, float offset = 0f)
        {
            var x = Mathf.FloorToInt(Mathf.Abs(offset) / 360f);
            offset = Mathf.Abs(offset) % 360f * Mathf.Sign(offset);

            ActualSongBpmTime = time;
            Rotation = (data.Rotation + offset) * direction;
            Direction = (LightRotationDirection)data.Direction;
            EaseType = (EaseType)data.EaseType;
            Loop = data.Loop + x;
            UsePrevious = data.UsePrevious == 1;
        }
    }
}

[Serializable]
public readonly struct RotationTransformContainer
{
    public readonly StateChunksContainer<LightRotationGroupStateData,
            BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
        Container;

    public readonly Transform Transform;
    public readonly Axis Axis;
    public readonly bool Mirrored;

    public RotationTransformContainer(Transform transform, Axis axis, bool mirrored)
    {
        Container = new();
        Transform = transform;
        Axis = axis;
        Mirrored = mirrored;
    }
}

[Serializable]
public struct TransformEntry
{
    public int ID;
    public Transform Transform;
    public Axis Axis;
    public bool Mirrored;
}
