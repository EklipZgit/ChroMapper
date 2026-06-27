using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using ZLinq;

public class LaneRotationProvider : StateManager<RotationEventStateData, BaseObject>
{
    [Header("State")] public float EditRotation;
    public float PlaybackRotation;
    public float LastSmoothRotation;

    [SerializeField] private float smoothing = 0.5f;
    [SerializeField] public float SmoothRotation;
    [SerializeField] private float smoothSpeed;
    private float timeSinceChange;

    public event Action<float> OnEditChanged;
    public event Action<float> OnPlaybackChanged;
    public event Action<float> OnSmoothedPlaybackChanged;

    private readonly RotationEventStateChunksContainer container = new();

    protected void Start() => Atsc.OnPlayToggled += HandlePlayToggle;

    protected void OnDestroy() => Atsc.OnPlayToggled -= HandlePlayToggle;

    public void LateUpdate()
    {
        timeSinceChange = Mathf.Clamp01(timeSinceChange + (Time.deltaTime * 2f));
        var rotation = Settings.Instance.Reduced360RotationMotion
            ? Mathf.LerpAngle(LastSmoothRotation, PlaybackRotation, timeSinceChange)
            : Mathf.SmoothDampAngle(SmoothRotation, PlaybackRotation, ref smoothSpeed, smoothing);
        if (rotation == SmoothRotation) return;
        SmoothRotation = rotation;
        OnSmoothedPlaybackChanged?.Invoke(rotation);
    }

    private void HandlePlayToggle(bool _)
    {
        if (PlaybackRotation == SmoothRotation) return;
        SmoothRotation = PlaybackRotation;
        OnSmoothedPlaybackChanged?.Invoke(PlaybackRotation);
    }

    public override void Initialize()
    {
        InitializeStates(
            container,
            CreateState(new BaseRotationEvent()),
            CreateState(new BaseRotationEvent()));
        InsertData(new BaseRotationEvent());
    }

    public override void UpdateTime(bool isPlaying, float time)
    {
        container.IsCurrentOrFindState(time, Atsc.IsPlaying);
        UpdateState(time);
    }

    public void UpdateState(float time)
    {
        var state = container.CurrentState;
        var prevRotation = SmoothRotation;
        if (state.Absolute)
        {
            if (Mathf.Approximately(state.NextAbsoluteRotation, PlaybackRotation)) return;
            PlaybackRotation = state.NextAbsoluteRotation;
        }
        else if (Mathf.Approximately(time, state.StartTime))
        {
            if (Mathf.Approximately(state.EarlyRotation, PlaybackRotation)) return;
            PlaybackRotation = state.EarlyRotation;
        }
        else
        {
            if (Mathf.Approximately(state.LateRotation, PlaybackRotation)) return;
            PlaybackRotation = state.LateRotation;
        }

        LastSmoothRotation = prevRotation;
        timeSinceChange = 0f;
        OnPlaybackChanged?.Invoke(PlaybackRotation);
    }

    public void SetEditRotation(int rotation)
    {
        if (Mathf.Approximately(rotation, EditRotation)) return;
        EditRotation = Mathf.Repeat(rotation, 360f);
        OnEditChanged?.Invoke(EditRotation);
    }

    public float GetRotationAt(float time)
    {
        var state = container.GetStateAt(time).state;
        return time == state.StartTime ? state.EarlyRotation : state.LateRotation;
    }

    private static void ApplyFromTo(
        RotationEventStateData fromState,
        RotationEventStateData toState)
    {
        // early + late rotation is combined in late rotation
        // if early rotation happens after another rotation event, take late rotation

        // this to ensure early rotation is always present with correct rotation from previous non-same time
        if (!Mathf.Approximately(fromState.StartTime, toState.StartTime))
            toState.EarlyRotation = fromState.LateRotation;

        if (toState.ExecutionTime == ExecutionTime.Early)
        {
            if (Mathf.Approximately(fromState.StartTime, toState.StartTime))
                toState.EarlyRotation = fromState.EarlyRotation + toState.Rotation;
            else
                toState.EarlyRotation = fromState.LateRotation + toState.Rotation;
        }

        toState.LateRotation = fromState.LateRotation + toState.Rotation;
    }

    private void ReapplyNext(RotationEventStateData currState)
    {
        var enumerator = container.Collection.EnumerateAfter(currState);
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            ApplyFromTo(currState, nextState);
            currState = nextState;
        }
    }

    public IEnumerable<RotationEventStateData> EnumerateFromLeft(float time)
    {
        var bucket = container.Collection.GetBucketFrom(time);
        var idx = container.Collection.BinarySearchLeft(bucket, time);
        if (idx == -1) yield break;
        while (idx < bucket.Count && Mathf.Approximately(bucket[idx].StartTime, time)) yield return bucket[idx++];
    }

    public RotationEventStateData EnumerateToRightGetNextTime(float time)
    {
        var enumerator = container.Collection.EnumerateFrom(time);
        while (enumerator.MoveNext())
        {
            var nextState = enumerator.Current;
            if (!Mathf.Approximately(time, nextState!.StartTime)) return nextState;
        }

        return null;
    }

    private void ReapplyAbsolute(RotationEventStateData state)
    {
        ApplyAbsolute(container.GetStateAt(state.StartTime - 0.001f).state, state);
        ApplyAbsolute(
            container.GetStateAt(state.StartTime).state,
            EnumerateToRightGetNextTime(state.StartTime));
    }

    private void ApplyAbsolute(RotationEventStateData prevState, RotationEventStateData nextState)
    {
        if (prevState == null || nextState == null) return;

        var dataAtTime = EnumerateFromLeft(nextState.StartTime)
            .AsValueEnumerable()
            .Select(x => x.Rotation)
            .Distinct()
            .ToArray();
        if (dataAtTime.Length == 0) return;

        var sum = dataAtTime.AsValueEnumerable().Sum();
        var average = sum / dataAtTime.Length;

        prevState.NextAbsoluteRotation = average;
    }

    protected override void OnInsertUpdateFromPreviousStateAndNextState(
        RotationEventStateData newState,
        RotationEventStateData prevState,
        RotationEventStateData nextState)
    {
        base.OnInsertUpdateFromPreviousStateAndNextState(newState, prevState, nextState);
        prevState.NextAbsoluteRotation = newState.Rotation;
        newState.NextAbsoluteRotation = nextState.Rotation;
        if (newState.Absolute) return;

        ApplyFromTo(prevState, newState);
        ApplyFromTo(newState, nextState);
    }

    public override void InsertData(BaseObject data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;

        HandleInsertState(container, state);
        if (state.Absolute)
            ReapplyAbsolute(state);
        else
            ReapplyNext(state);
    }

    protected override void OnRemoveUpdatePreviousAndNextState(
        RotationEventStateData currState,
        RotationEventStateData prevState,
        RotationEventStateData nextState)
    {
        base.OnRemoveUpdatePreviousAndNextState(currState, prevState, nextState);
        prevState.NextAbsoluteRotation = nextState.Rotation;
        if (currState.Absolute) return;

        ApplyFromTo(prevState, nextState);
    }

    public override void RemoveData(BaseObject reference, BaseObject original)
    {
        var state = container.GetStateFrom(reference, original);
        HandleRemoveState(container, state);
        if (state.Absolute)
            ReapplyAbsolute(state);
        else
            ReapplyNext(container.GetStateAt(state.StartTime).state);

        if (state == container.CurrentState) container.SetStateAt(reference.SongBpmTime);
    }

    public override void Refresh() => UpdateState(Atsc.CurrentSongBpmTime);

    protected override RotationEventStateData CreateState(BaseObject data) => new(data);
}

public class RotationEventStateChunksContainer : StateChunksContainer<RotationEventStateData, BaseObject>
{
}

public class RotationEventStateData : StateData<BaseObject>
{
    public readonly bool Absolute;
    public readonly ExecutionTime ExecutionTime;
    public readonly float Rotation;

    public float EarlyRotation;
    public float LateRotation;

    public float NextAbsoluteRotation;

    public RotationEventStateData(BaseObject obj) : base(obj)
    {
        switch (obj)
        {
            case BaseGrid grid:
                Absolute = true;
                Rotation = grid.Rotation;
                break;
            case BaseRotationEvent evt:
                ExecutionTime = evt.ExecutionTime;
                Rotation = evt.Rotation;
                break;
        }
    }
}
