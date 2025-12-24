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
            var end = CreateState(new() { songBpmTime = float.MaxValue });

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

            start.Next = end;
            end.Previous = start;

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

        var nextEvent = GetNextEvent(state, time);
        if (nextEvent.UsePrevious) nextEvent = currentEvent;

        if (!container.StartEvent.Equals(currentEvent) || !container.EndEvent.Equals(nextEvent))
        {
            var startAngle = Mathf.Repeat(currentEvent.Rotation, 360f);
            var targetAngle = Mathf.Repeat(nextEvent.Rotation, 360f);
            var nextAngle = ComputeTargetAngle(
                startAngle,
                targetAngle,
                nextEvent.Loop,
                nextEvent.Direction);

            container.StartTime = currentEventTime;
            container.StartAngle = startAngle;

            container.EndTime = nextEvent.ActualSongBpmTime;
            container.EndAngle = nextAngle;

            container.Easing = Easing.FromID((int)nextEvent.EaseType);

            container.StartEvent = currentEvent;
            container.EndEvent = nextEvent;
        }

        var tNorm = Mathf.InverseLerp(container.StartTime, container.EndTime, time);
        var angle = Mathf.LerpUnclamped(container.StartAngle, container.EndAngle, container.Easing(tNorm));

        SetRotation(container.Transform, Mathf.Repeat(angle, 360f), container.Axis, container.Mirrored);
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

    private static LightRotationGroupStateData.LightRotationEvent GetPreviousEvent(
        LightRotationGroupStateData state,
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

    private static LightRotationGroupStateData.LightRotationEvent GetNextEvent(
        LightRotationGroupStateData state,
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

                state.StartTime =
                    (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(
                        data.JsonTime + (timeStep * durationOrder));
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
        nextState.Skip = nextState.Base.SongBpmTime < newState.StartTime;
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
    public bool Skip;

    public LightRotationGroupStateData Previous;
    public LightRotationGroupStateData Next;

    public LightRotationEvent[] Events;

    public LightRotationGroupStateData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) : base(data)
    {
    }

    public readonly struct LightRotationEvent : IEquatable<LightRotationEvent>
    {
        private static long id;
        private readonly long instanceId;

        public readonly float ActualSongBpmTime;

        public readonly float Rotation;
        public readonly LightRotationDirection Direction;
        public readonly EaseType EaseType;
        public readonly int Loop;
        public readonly bool UsePrevious;

        public LightRotationEvent(BaseLightRotationBase data, float time, float direction = 1f, float offset = 0f)
        {
            instanceId = id++;

            var x = Mathf.FloorToInt(Mathf.Abs(offset) / 360f);
            offset = Mathf.Abs(offset) % 360f * Mathf.Sign(offset);

            ActualSongBpmTime = time;
            Rotation = (data.Rotation + offset) * direction;
            Direction = (LightRotationDirection)data.Direction;
            EaseType = (EaseType)data.EaseType;
            Loop = data.Loop + x;
            UsePrevious = data.UsePrevious == 1;
        }

        public bool Equals(LightRotationEvent other) => instanceId == other.instanceId;

        public override bool Equals(object obj) => obj is LightRotationEvent other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine(ActualSongBpmTime, Rotation, (int)Direction, (int)EaseType, Loop, UsePrevious);
    }
}

[Serializable]
public class RotationTransformContainer
{
    public readonly StateChunksContainer<LightRotationGroupStateData,
            BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
        Container;

    public readonly Transform Transform;
    public readonly Axis Axis;
    public readonly bool Mirrored;

    public LightRotationGroupStateData.LightRotationEvent StartEvent;
    public float StartTime;
    public float StartAngle;

    public LightRotationGroupStateData.LightRotationEvent EndEvent;
    public float EndTime;
    public float EndAngle;

    public Func<float, float> Easing = global::Easing.Step;

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
