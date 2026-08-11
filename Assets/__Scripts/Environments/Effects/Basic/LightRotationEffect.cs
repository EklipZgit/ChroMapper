using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightRotationEffect : BasicMovementEffect<LightRotationStateData>
{
    public LightRotation Visual;
    public float SpeedMultiplier = 1f;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightRotation>();
    }

    public override void Initialize()
    {
        // A missing visual no longer crashes snapshot construction, but keep a targeted
        // diagnostic because such a manager cannot render its otherwise valid timeline.
        if (Visual == null)
            Debug.LogError($"LightRotationEffect on '{name}' initialized without a LightRotation visual.", this);

        base.Initialize();
    }

    protected override LightRotationStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightRotationStateData previous, LightRotationStateData current)
    {
        var evt = current.Base;
        var resolvedValue = (float)evt.Value;

        if (previous == null)
        {
            // Start sentinel: t=0 has no rotation.
            current.Angle = 0f;
            current.Enabled = false;
            current.Speed = 0f;
            current.StartOffset = 0f;
            current.Direction = 0f;
            current.HasRandom = false;
            current.Lock = false;
            return;
        }

        var deltaSeconds = Atsc.GetSecondsFromBeat(current.StartTime - previous.StartTime);
        var preEventAngle = previous.Angle + (previous.Enabled ? previous.Speed * deltaSeconds : 0f);

        var lockRotation = evt.CustomLockRotation == true;

        if (resolvedValue > 0)
        {
            if (evt.CustomPreciseSpeed.HasValue)
                resolvedValue = evt.CustomPreciseSpeed.Value;
            else if (evt.CustomSpeed.HasValue)
                resolvedValue = evt.CustomSpeed.Value;
        }

        if (resolvedValue > 0 && !current.HasRandom)
        {
            // Resolve the random offset and direction once for this node so edits never
            // cause the preview to jump to a new random value.
            current.StartOffset = Random.Range(0f, 180f);
            current.Direction = Random.value < 0.5f ? 1f : -1f;
            current.HasRandom = true;
        }

        if (evt.CustomDirection.HasValue)
        {
            // Chroma defines direction relative to the laser side: event 12 mirrors
            // the same custom direction value used by the opposite rotating laser.
            var isLeftEvent = evt.Type == 12;
            current.Direction = evt.CustomDirection.Value == 0
                ? isLeftEvent ? -1f : 1f
                : isLeftEvent ? 1f : -1f;
        }

        current.Lock = lockRotation;

        if (resolvedValue == 0)
        {
            current.Enabled = false;
            current.Speed = 0f;
            current.Angle = lockRotation ? preEventAngle : 0f;
        }
        else
        {
            current.Enabled = true;
            current.Angle = lockRotation ? preEventAngle : current.StartOffset;
            // Keep serialized speed on the state manager: snapshots can be computed before
            // a dynamically built visual is available, while Chroma custom data still bypasses it.
            var speedMultiplier = evt.CustomData != null ? 1f : SpeedMultiplier;
            current.Speed = resolvedValue * speedMultiplier * 20f * current.Direction;
        }
    }

    protected override void ApplyVisual(float beat, float seconds, LightRotationStateData current, LightRotationStateData next)
    {
        if (Visual == null)
            return;

        var angle = current.Angle + (current.Enabled ? current.Speed * seconds : 0f);
        Visual.Apply(angle);
    }
}

public class LightRotationStateData : BasicMovementStateData
{
    public float StartOffset;   // random 0..180 applied on an enabled event
    public float Direction;     // -1 or 1
    public float Speed;
    public float Angle;         // the actual angle at this node's start
    public bool Enabled;
    public bool Lock;           // Chroma: do not reset the transform on this event
    public bool HasRandom;      // have StartOffset/Direction already been resolved for this node

    public LightRotationStateData(BaseEvent data) : base(data)
    {
    }
}
