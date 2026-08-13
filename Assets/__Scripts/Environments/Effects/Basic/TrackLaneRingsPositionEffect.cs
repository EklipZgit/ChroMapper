using Beatmap.Base;
using UnityEngine;

public class TrackLaneRingsPositionEffect : BasicMovementEffect<TrackLaneRingsPositionStateData>
{
    public TrackLaneRingsPositionSpawner Visual;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<TrackLaneRingsPositionSpawner>();

        if (Visual != null && Visual.RingManager != null)
            Visual.RingManager.UseCached = true;
    }

    protected override TrackLaneRingsPositionStateData CreateState(BaseEvent data) => new(data);

    public override void Initialize()
    {
        // Runtime-built components receive their Visual after Awake, so claim the manager
        // here as well and prevent its live loop from fighting the deterministic evaluator.
        if (Visual != null && Visual.RingManager != null)
            Visual.RingManager.UseCached = true;

        base.Initialize();
    }

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        // Use the same fixed-step-equivalent evaluator while playing and paused.
        // The previous live handoff could only restore event-start state, so rewind
        // and resume changed the lerp discontinuously.
        if (Visual != null && Visual.RingManager != null)
            Visual.RingManager.UseCached = true;

        base.UpdateTime(isPlaying, currentTime);
    }

    protected override void ComputeSnapshot(TrackLaneRingsPositionStateData previous, TrackLaneRingsPositionStateData current)
    {
        var ringCount = Visual != null && Visual.RingManager != null ? Visual.RingManager.Rings.Count : 0;
        if (ringCount == 0)
            return;

        if (current.RingPositions == null
            || current.PreviousRingPositions == null
            || current.RingPositions.Length != ringCount
            || current.PreviousRingPositions.Length != ringCount)
        {
            current.RingPositions = new float[ringCount];
            current.PreviousRingPositions = new float[ringCount];
        }

        if (previous == null)
        {
            // Start sentinel: capture the initial ring Z positions.
            for (var i = 0; i < ringCount; i++)
            {
                current.RingPositions[i] = Visual.RingManager.Rings[i].PositionZ;
                current.PreviousRingPositions[i] = current.RingPositions[i];
            }

            current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
            // Frame -1 is the captured pre-song state; frame zero is the first fixed pair
            // shared with rotation once the audio controller begins rendering.
            current.SnapshotFrame = -1;
            current.SameTypeIndex = -1;
            current.Step = 0f;
            current.Speed = 0f;
            current.PreviousStep = 0f;
            current.PreviousSpeed = 0f;
            current.AssignmentFrame = int.MinValue;
            return;
        }

        current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        // The event snapshot is the last phased fixed state visible on its callback frame;
        // its position assignment begins on the same following tick as ring rotation.
        current.AssignmentFrame = TrackLaneRingsRotationEffect.GetFirstAssignmentFrame(
            current.SnapshotSeconds,
            Time.fixedDeltaTime);
        current.SnapshotFrame = current.AssignmentFrame - 1;
        current.SameTypeIndex = previous.SameTypeIndex + 1;
        var frames = current.SnapshotFrame - previous.SnapshotFrame;
        for (var i = 0; i < ringCount; i++)
        {
            // Replay the prior event's delayed LateUpdate assignment while carrying the snapshot chain forward.
            EvaluateDiscretePair(
                previous,
                i,
                frames,
                out current.PreviousRingPositions[i],
                out current.RingPositions[i]);
        }

        current.PreviousStep = previous.Step;
        current.PreviousSpeed = previous.Speed;
        // Heck gives modern speed precedence and falls back to V2 preciseSpeed.
        current.Speed = current.Base.CustomSpeed ?? current.Base.CustomPreciseSpeed ?? Visual.MoveSpeed;
        // Chroma's ring-step patch supports precise step and speed only; legacy multipliers do not apply here.
        current.Step = current.Base.CustomStep
            ?? (current.SameTypeIndex % 2 == 0 ? Visual.MaxPositionStep : Visual.MinPositionStep);
    }

    protected override void ApplyVisual(float beat, float seconds, TrackLaneRingsPositionStateData current, TrackLaneRingsPositionStateData next)
    {
        if (Visual == null || Visual.RingManager == null)
            return;

        // Position and rotation are fields of the same OEM TrackLaneRing and therefore
        // must use the same phased fixed pair and unclamped TimeHelper render factor.
        TrackLaneRingsRotationEffect.GetPreviewRenderState(
            current.SnapshotSeconds + seconds,
            Time.fixedDeltaTime,
            out _,
            out var fixedFrame,
            out var interpolation);
        var frames = fixedFrame - current.SnapshotFrame;
        var rings = Visual.RingManager.Rings;
        for (var i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            // Replay both sides of the modeled LateUpdate assignment on the render hot path.
            EvaluateDiscretePair(
                current,
                i,
                frames,
                out var previousPosition,
                out var currentPosition);
            var position = previousPosition + ((currentPosition - previousPosition) * interpolation);
            ring.CachedTransform.localPosition = new Vector3(
                ring.PositionOffset.x,
                ring.PositionOffset.y,
                position);
        }
    }

    // Replay Unity's float recurrence exactly and retain both render endpoints in one pass.
    private void EvaluateDiscretePair(
        TrackLaneRingsPositionStateData state,
        int ringIndex,
        int frames,
        out float previous,
        out float current)
    {
        var value = state.RingPositions[ringIndex];
        previous = state.PreviousRingPositions[ringIndex];
        var positionOffset = Visual.RingManager.Rings[ringIndex].PositionOffset.z;
        for (var i = 0; i < frames; i++)
        {
            previous = value;
            var tickFrame = state.SnapshotFrame + i + 1;
            var assigned = tickFrame >= state.AssignmentFrame;
            var step = assigned ? state.Step : state.PreviousStep;
            var speed = assigned ? state.Speed : state.PreviousSpeed;
            var destination = positionOffset + (ringIndex * step);
            var next = Mathf.Lerp(value, destination, Time.fixedDeltaTime * speed);
            // A stable old destination cannot skip a later callback assignment in this interval.
            if (next == value && assigned)
                break;
            value = next;
        }
        current = value;
    }
}

public class TrackLaneRingsPositionStateData : BasicMovementStateData
{
    public float[] RingPositions;
    public float[] PreviousRingPositions;
    public float Step;
    public float Speed;
    public float PreviousStep;
    public float PreviousSpeed;
    public float SnapshotSeconds;
    public int SnapshotFrame;
    public int AssignmentFrame;

    public TrackLaneRingsPositionStateData(BaseEvent data) : base(data)
    {
    }
}
