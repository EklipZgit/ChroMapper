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
    [SerializeField] private List<TransformEntry> lightEntries = new();

    private readonly Dictionary<int, Dictionary<(int index, Axis axis), (
            StateChunksContainer<LightRotationGroupStateData, BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
            container,
            bool mirrored,
            Transform transform)>>
        transformsByGroupAndId = new();

    private readonly List<(
        StateChunksContainer<LightRotationGroupStateData, BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>
        container,
        Axis axis,
        bool mirrored,
        Transform transform)> activeTransforms = new();

    public void Register(int group, int id, Axis axis, bool mirrored, Transform tr) =>
        lightEntries.Add(
            new()
            {
                Group = group,
                ID = id,
                Axis = axis,
                Mirrored = mirrored,
                Transform = tr
            });

    public void Unregister(int group, int id, Axis axis) =>
        lightEntries.RemoveAll(e => e.Group == group && e.ID == id && e.Axis == axis);

    public void Unregister(Transform tr) => lightEntries.RemoveAll(e => e.Transform == tr);

    public override void Initialize()
    {
        transformsByGroupAndId.Clear();
        activeTransforms.Clear();
        foreach (var entry in lightEntries)
        {
            transformsByGroupAndId.TryAdd(entry.Group, new());
            if (transformsByGroupAndId[entry.Group].ContainsKey((entry.ID, entry.Axis))) continue;

            var container =
                new StateChunksContainer<LightRotationGroupStateData,
                    BaseLightRotationEventBoxGroup<BaseLightRotationEventBox>>();
            var start = CreateState(new());
            var end = CreateState(new());
            start.Box = new() { Events = Array.Empty<BaseLightRotationBase>() };
            end.Box = new() { Events = Array.Empty<BaseLightRotationBase>() };
            InitializeStates(container, start, end);
            transformsByGroupAndId[entry.Group][(entry.ID, entry.Axis)] = (container, entry.Mirrored, entry.Transform);
            activeTransforms.Add((container, entry.Axis, entry.Mirrored, entry.Transform));
        }
    }

    public override void UpdateDirty() => throw new NotImplementedException();

    public override void UpdateTime(float time)
    {
        foreach (var (container, axis, mirrored, tr) in activeTransforms)
        {
            container.IsCurrentOrFindState(time, Atsc.IsPlaying);
            UpdateObject(tr, axis, mirrored, container.CurrentState, time);
        }
    }

    private void UpdateObject(Transform tr, Axis axis, bool mirrored, LightRotationGroupStateData state, float time)
    {
        var (previousTime, previousOffset, previousFlip, previousEvent) = GetCurrentOrPreviousEvent(state, time);
        if (previousEvent == null) return;

        var (nextTime, nextOffset, nextFlip, nextEvent) = GetNextEvent(state, previousTime);
        if (nextEvent == null) return;

        if (nextEvent.UsePrevious == 1)
        {
            nextOffset = previousOffset;
            nextFlip = previousFlip;
            nextEvent = previousEvent;
        }

        var prevVal = previousEvent.Rotation + previousOffset;
        prevVal = Mathf.Repeat(prevVal, 360f);
        if (previousFlip) prevVal *= -1f;

        var nextVal = ComputeTargetAngle(
            prevVal,
            Mathf.Repeat(nextEvent.Rotation + nextOffset, 360f) * (nextFlip ? -1f : 1f),
            nextEvent.Loop,
            (LightRotationDirection)nextEvent.Direction);

        var tNorm = (time - previousTime) / (nextTime - previousTime);
        var easing = Easing.FromID(nextEvent.EaseType);

        var val = Mathf.Lerp(prevVal, nextVal, easing(tNorm));
        SetRotation(tr, Mathf.Repeat(val, 360f), axis, mirrored);
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

    private static (float time, float offset, bool flip, BaseLightRotationBase evt) GetCurrentOrPreviousEvent(
        LightRotationGroupStateData state,
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
                        state.Box.RotationAffectFirst == 0 && idx == 0 ? 0f : state.Offset, state.Box.Flip == 1, evt);
                }

                var prev = GetPreviousEvent(state, state.StartTime + evt.JsonTime);
                return (state.StartTime + evt.JsonTime, prev.offset, prev.flip, prev.evt);
            }

            if (state.Previous == null) return (-1f, 0f, false, null);
            state = state.Previous;
        }
    }

    private static (float time, float offset, bool flip, BaseLightRotationBase evt) GetPreviousEvent(
        LightRotationGroupStateData state,
        float time)
    {
        while (true)
        {
            var localTime = time - state.StartTime;
            var idx = Array.FindLastIndex(
                state.Box.Events,
                x => x.JsonTime < localTime && state.StartTime + x.JsonTime < state.EndTime && x.UsePrevious != 1);
            if (idx != -1)
            {
                var evt = state.Box.Events[idx];
                return (state.StartTime + evt.JsonTime,
                    state.Box.RotationAffectFirst == 0 && idx == 0 ? 0f : state.Offset, state.Box.Flip == 1, evt);
            }

            if (state.Previous == null) return (-1f, 0f, false, null);
            state = state.Previous;
        }
    }

    private static (float time, float offset, bool flip, BaseLightRotationBase evt) GetNextEvent(
        LightRotationGroupStateData state,
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
                    state.Box.RotationAffectFirst == 0 && idx == 0 ? 0f : state.Offset, state.Box.Flip == 1, evt);
            }

            if (state.Next == null) return (-1f, 0f, false, null);
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
            foreach (var (element, durationOrder, distributionOrder) in IndexFilterHelper.Convert(
                box.IndexFilter,
                transformsByGroupAndId[data.ID].Count))
            {
                if (!taken.Add((element, (Axis)box.Axis))) continue;

                var state = new LightRotationGroupStateData(data) { Box = box };
                var durationOffset = 0f;
                if (box.BeatDistributionType == (int)DistributionType.Wave)
                {
                    var durationNorm = durationOrder / (float)(transformsByGroupAndId[data.ID].Count - 1);
                    durationOffset = Mathf.Max(
                            0f,
                            box.BeatDistribution - box.Events.Max(x => x.JsonTime))
                        * durationNorm;
                }
                else if (box.BeatDistributionType == (int)DistributionType.Step)
                    durationOffset = box.BeatDistribution * durationOrder;

                var distributionOffset = 0f;
                if (box.RotationDistributionType == (int)DistributionType.Wave)
                {
                    var distributionNorm = distributionOrder / (float)(transformsByGroupAndId[data.ID].Count - 1);
                    distributionOffset = box.RotationDistribution * distributionNorm;
                }
                else if (box.RotationDistributionType == (int)DistributionType.Step)
                    distributionOffset = box.RotationDistribution * distributionOrder;

                state.StartTime = data.SongBpmTime + durationOffset;
                state.Offset = distributionOffset;

                if (!transformsByGroupAndId[data.ID].ContainsKey((element, (Axis)box.Axis))) continue;
                var container = transformsByGroupAndId[data.ID][(element, (Axis)box.Axis)].container;
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
    public float Offset;

    public LightRotationGroupStateData Previous;
    public LightRotationGroupStateData Next;

    public BaseLightRotationEventBox Box;

    public LightRotationGroupStateData(BaseLightRotationEventBoxGroup<BaseLightRotationEventBox> data) : base(data)
    {
    }
}

[Serializable]
public class TransformEntry
{
    public int Group;
    public int ID;
    public Axis Axis;
    public bool Mirrored;
    public Transform Transform;
}
