using Beatmap.Base;
using UnityEngine;

public class MovementEffect : BasicMovementEffect<MovementStateData>
{
    public Movement Visual;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<Movement>();
    }

    protected override MovementStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(MovementStateData previous, MovementStateData current)
    {
        var data = Visual.MovementData;
        var len = data.Length;
        var index = container.Collection.IndexOf(current);

        if (previous == null)
        {
            current.Offset = data[0];
            current.Target = data[0];
            current.TargetIndex = 0;
            return;
        }

        var deltaSeconds = Atsc.GetSecondsFromBeat(current.StartTime - previous.StartTime);
        var frames = Mathf.FloorToInt(deltaSeconds / Time.fixedDeltaTime);
        current.Offset = LerpDiscrete(previous.Offset, previous.Target, Visual.TransitionSpeed, frames);

        current.TargetIndex = index % len;
        current.Target = data[current.TargetIndex];
    }

    protected override void ApplyVisual(float beat, float seconds, MovementStateData current, MovementStateData next)
    {
        if (Visual == null)
            return;

        var framePosition = seconds / Time.fixedDeltaTime;
        var frames = Mathf.FloorToInt(framePosition);
        var currentOffset = LerpDiscrete(current.Offset, current.Target, Visual.TransitionSpeed, frames);
        var nextOffset = LerpDiscrete(current.Offset, current.Target, Visual.TransitionSpeed, frames + 1);
        Visual.Apply(Vector3.LerpUnclamped(currentOffset, nextOffset, framePosition - frames));
    }

    // MovementBeatmapEventEffect uses fixed-step LerpUnclamped rather than a
    // continuous exponential, then interpolates adjacent fixed states for rendering.
    private static Vector3 LerpDiscrete(Vector3 start, Vector3 destination, float speed, int frames)
    {
        if (frames <= 0)
            return start;

        var factor = Mathf.Pow(1f - (Time.fixedDeltaTime * speed), frames);
        return destination - ((destination - start) * factor);
    }
}

public class MovementStateData : BasicMovementStateData
{
    public Vector3 Offset;
    public Vector3 Target;
    public int TargetIndex;

    public MovementStateData(BaseEvent data) : base(data)
    {
    }
}
