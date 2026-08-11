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

        if (current.RingPositions == null || current.RingPositions.Length != ringCount)
            current.RingPositions = new float[ringCount];

        if (previous == null)
        {
            // Start sentinel: capture the initial ring Z positions.
            for (var i = 0; i < ringCount; i++)
                current.RingPositions[i] = Visual.RingManager.Rings[i].PositionZ;

            current.Step = 0f;
            current.Speed = 0f;
            return;
        }

        var deltaSeconds = Atsc.GetSecondsFromBeat(current.StartTime - previous.StartTime);
        var frames = Mathf.FloorToInt(deltaSeconds / Time.fixedDeltaTime);
        for (var i = 0; i < ringCount; i++)
        {
            var dest = i * previous.Step;
            current.RingPositions[i] = LerpDiscrete(
                previous.RingPositions[i],
                dest,
                previous.Speed,
                frames);
        }

        current.Speed = current.Base.CustomSpeed ?? Visual.MoveSpeed;
        if (current.Base.CustomSpeedMult.HasValue)
            current.Speed *= current.Base.CustomSpeedMult.Value;

        var index = container.Collection.IndexOf(current);
        current.Step = current.Base.CustomStep
            ?? (index % 2 == 0 ? Visual.MaxPositionStep : Visual.MinPositionStep);
        if (current.Base.CustomStepMult.HasValue)
            current.Step *= current.Base.CustomStepMult.Value;
    }

    protected override void ApplyVisual(float beat, float seconds, TrackLaneRingsPositionStateData current, TrackLaneRingsPositionStateData next)
    {
        if (Visual == null || Visual.RingManager == null)
            return;

        // ApplyVisual is intentionally used in both modes. It is the only path
        // that can be evaluated identically after a rewind or a pause/resume.
        var fixedDeltaTime = Time.fixedDeltaTime;
        var framePosition = seconds / fixedDeltaTime;
        var frames = Mathf.FloorToInt(framePosition);
        var interpolation = framePosition - frames;
        var rings = Visual.RingManager.Rings;
        for (var i = 0; i < rings.Count; i++)
        {
            var ring = rings[i];
            var destination = i * current.Step;
            var currentPosition = LerpDiscrete(current.RingPositions[i], destination, current.Speed, frames);
            var nextPosition = LerpDiscrete(current.RingPositions[i], destination, current.Speed, frames + 1);
            var position = Mathf.Lerp(currentPosition, nextPosition, interpolation);
            ring.CachedTransform.localPosition = new Vector3(
                ring.PositionOffset.x,
                ring.PositionOffset.y,
                ring.PositionOffset.z + position);
        }
    }

    // Match TrackLaneRing.FixedUpdateRing, which uses Mathf.Lerp every fixed tick;
    // the exponential form previously used here diverged during pause preview.
    private static float LerpDiscrete(float start, float dest, float speed, int frames)
    {
        if (frames <= 0 || speed <= 0f)
            return start;

        var factor = 1f - Mathf.Clamp01(Time.fixedDeltaTime * speed);
        return dest - (dest - start) * Mathf.Pow(factor, frames);
    }
}

public class TrackLaneRingsPositionStateData : BasicMovementStateData
{
    public float[] RingPositions;
    public float Step;
    public float Speed;

    public TrackLaneRingsPositionStateData(BaseEvent data) : base(data)
    {
    }
}
