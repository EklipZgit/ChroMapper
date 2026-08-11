using System;
using Beatmap.Base;
using UnityEngine;

// Base class for Basic Event effects that produce continuous visual movement
// (rotations, positions, etc.).  Instead of running live Update/FixedUpdate loops,
// we store a snapshot at the start of each event node and recompute/lerp on demand
// in UpdateTime, which keeps the preview static while the editor is paused and lets
// us resume from the last unaffected node after any edit.
public abstract class BasicMovementEffect<TState> : BasicEventEffect<TState> where TState : BasicMovementStateData
{
    protected BasicEventStateChunksContainer<TState> container;
    private TState startSentinel;
    private TState endSentinel;

    private float computedUpToTime = float.MinValue;
    private float dirtyFromTime = float.MinValue;
    private bool dirty;
    private TState appliedState;
    private TState appliedNextState;
    private float appliedTime = float.MinValue;

    // Compute the visual state at current.StartTime from the previous node.
    // prev is null when we are computing the t=0 start sentinel.
    protected abstract void ComputeSnapshot(TState previous, TState current);

    // Apply the visual state for an arbitrary time between current and next.
    // 'seconds' is the elapsed seconds since current.StartTime.
    protected abstract void ApplyVisual(float beat, float seconds, TState current, TState next);

    public override void Initialize()
    {
        container = new BasicEventStateChunksContainer<TState>();
        startSentinel = CreateState(new BaseEvent());
        endSentinel = CreateState(new BaseEvent());

        // Resize buckets the same way StateManager does so lookups stay fast.
        container.Resize(Atsc.GetBeatFromSeconds(Atsc.SongAudioSource.clip.length));
        endSentinel.StartTime = endSentinel.EndTime;

        container.AddState(startSentinel);
        container.AddState(endSentinel);
        container.SetStateAt(0);

        // The start sentinel is the t=0 state and carries the initial visual transform.
        ComputeSnapshot(null, startSentinel);
        startSentinel.SnapshotValid = true;
        computedUpToTime = startSentinel.StartTime;
        dirtyFromTime = startSentinel.StartTime;
        dirty = false;
        appliedState = null;
        appliedNextState = null;
        appliedTime = float.MinValue;
    }

    public override void Refresh()
    {
        // Force a recompute at the current playhead after the state has been rebuilt.
        UpdateTime(false, Atsc.CurrentSongBpmTime);
    }

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        if (container == null) return;

        // Recompute the chain forward from the last dirty point, or continue
        // forward from the last computed node if the playhead has moved past it.
        if (dirty || currentTime > computedUpToTime)
        {
            // Continue from the last computed node during ordinary forward playback.
            // Restarting at the sentinel made each newly crossed event replay and resize
            // every earlier snapshot, producing the allocation spikes seen in profiling.
            var start = dirty ? dirtyFromTime : computedUpToTime;
            RecomputeTo(start, currentTime);
        }

        // Most playback frames remain inside the same event interval. Reuse the resolved
        // nodes instead of performing bucket lookup and successor traversal for every
        // cloned movement effect on every render frame.
        var canReuseState = isPlaying
            && !dirty
            && appliedState != null
            && currentTime >= appliedTime
            && currentTime >= appliedState.StartTime
            && currentTime < appliedState.EndTime;
        if (!canReuseState)
        {
            container.SetStateAt(currentTime);
            appliedState = container.CurrentState;
            if (appliedState == endSentinel)
            {
                appliedTime = currentTime;
                return;
            }

            appliedNextState = container.GetNextStateFrom(appliedState);
            if (appliedNextState == endSentinel)
                appliedNextState = null;
        }

        appliedTime = currentTime;
        var seconds = appliedState == startSentinel
            ? 0f
            : Atsc.GetSecondsFromBeat(currentTime - appliedState.StartTime);
        ApplyVisual(currentTime, seconds, appliedState, appliedNextState);
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;

        // Link the new node into the bucket structure without expensive full-chain passes.
        var prev = container.GetOverlappingStateFrom(state);
        var next = container.GetNextStateFrom(state);

        if (prev != null)
            prev.EndTime = state.StartTime;

        state.EndTime = next != null ? next.StartTime : float.MaxValue;

        container.AddState(state);

        dirtyFromTime = prev != null ? prev.StartTime : startSentinel.StartTime;
        dirty = true;
        appliedState = null;
        appliedNextState = null;
    }

    public override void RemoveData(BaseEvent reference, BaseEvent original)
    {
        var state = container.GetStateFrom(reference, original);
        if (state == null) return;

        var prev = container.GetPreviousStateFrom(state);
        var next = container.GetNextStateFrom(state);

        if (prev != null)
            prev.EndTime = next != null ? next.StartTime : float.MaxValue;

        container.RemoveState(state);

        dirtyFromTime = prev != null ? prev.StartTime : startSentinel.StartTime;
        dirty = true;
        appliedState = null;
        appliedNextState = null;
    }

    private void RecomputeTo(float start, float target)
    {
        container.SetStateAt(start);
        var prev = container.CurrentState;
        // If the state at the starting beat has not had its snapshot built yet,
        // fall back to the start sentinel so we recompute the whole chain.
        if (prev == null || (prev != startSentinel && !prev.SnapshotValid))
            prev = startSentinel;

        var it = container.Collection.EnumerateAfter(prev);
        while (it.MoveNext())
        {
            var current = it.Current;
            if (current == endSentinel)
            {
                // No more events to compute; mark the chain as complete so we
                // don't keep rescanning the same empty tail every frame.
                computedUpToTime = float.MaxValue;
                break;
            }

            ComputeSnapshot(prev, current);
            current.SnapshotValid = true;
            prev = current;
            computedUpToTime = current.StartTime;

            // We need at least the node after the current playhead to lerp against.
            if (current.StartTime > target) break;
        }

        dirty = false;
    }
}
