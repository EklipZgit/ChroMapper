using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

// A pair must consume all three event types in one ordered timeline. Independent managers
// cannot reconstruct switch events or the other side when the playhead is scrubbed backwards.
public class LightPairRotationEffect : BasicMovementEffect<LightPairRotationStateData>
{
    public int LeftEventType;
    public int RightEventType;
    public int SwitchEventType = -1;
    public LightPairRotation Visual;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightPairRotation>();
    }

    protected override LightPairRotationStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightPairRotationStateData previous, LightPairRotationStateData current)
    {
        if (previous == null)
        {
            // The sentinel records both serialized rest angles so evaluating before the first
            // event never depends on whichever transform happened to be rendered previously.
            current.LeftStartAngle = Visual != null ? Visual.StartRotation : 0f;
            current.RightStartAngle = -current.LeftStartAngle;
            current.LeftAngle = current.LeftStartAngle;
            current.RightAngle = current.RightStartAngle;
            current.LeftSpeed = 0f;
            current.RightSpeed = 0f;
            current.LeftEnabled = false;
            current.RightEnabled = false;
            current.OverrideRandomValues = Visual != null && Visual.OverrideRandomValues;
            current.SwitchEventIndex = 0;
            current.RandomStartRotation = 0f;
            current.RandomDirection = 1f;
            current.HasRandom = false;
            return;
        }

        var deltaSeconds = Atsc.GetSecondsFromBeat(current.StartTime - previous.StartTime);
        current.LeftAngle = GetAngle(previous.LeftAngle, previous.LeftSpeed, previous.LeftEnabled, deltaSeconds);
        current.RightAngle = GetAngle(previous.RightAngle, previous.RightSpeed, previous.RightEnabled, deltaSeconds);
        current.LeftSpeed = previous.LeftSpeed;
        current.RightSpeed = previous.RightSpeed;
        current.LeftStartAngle = previous.LeftStartAngle;
        current.RightStartAngle = previous.RightStartAngle;
        current.LeftEnabled = previous.LeftEnabled;
        current.RightEnabled = previous.RightEnabled;
        current.OverrideRandomValues = previous.OverrideRandomValues;
        current.SwitchEventIndex = previous.SwitchEventIndex;

        if (current.Base.Type == SwitchEventType)
        {
            // Match sameTypeIndex parity: the first callback has index zero, then the
            // retained count advances for the next switch event.
            current.OverrideRandomValues = current.SwitchEventIndex % 2 == 1;
            current.SwitchEventIndex++;
        }

        ResolveRandom(previous, current);

        // OEM dispatch gives left and right precedence when event identifiers overlap with
        // the optional switch identifier, although the switch parity above still advances.
        if (current.Base.Type == LeftEventType)
        {
            ApplyRotationEvent(current, true);
        }
        else if (current.Base.Type == RightEventType)
        {
            ApplyRotationEvent(current, false);
        }
        else if (current.Base.Type == SwitchEventType)
        {
            ApplySwitchEvent(current);
        }
    }

    protected override void ApplyVisual(
        float beat,
        float seconds,
        LightPairRotationStateData current,
        LightPairRotationStateData next)
    {
        if (Visual == null)
            return;

        // Angles are derived directly from the snapshot and arbitrary song time so pause,
        // rewind, and large seeks never rely on MonoBehaviour.Update or Time.deltaTime.
        var leftAngle = GetAngle(current.LeftAngle, current.LeftSpeed, current.LeftEnabled, seconds);
        var rightAngle = GetAngle(current.RightAngle, current.RightSpeed, current.RightEnabled, seconds);
        Visual.Apply(leftAngle, rightAngle);
    }

    private void ResolveRandom(LightPairRotationStateData previous, LightPairRotationStateData current)
    {
        if (previous.HasRandom && previous.StartTime == current.StartTime)
        {
            // Beat Saber generates one random pair per callback frame. Events at the same song
            // time are evaluated together here and must share it across both laser sides.
            current.RandomStartRotation = previous.RandomStartRotation;
            current.RandomDirection = previous.RandomDirection;
            current.HasRandom = true;
            return;
        }

        if (current.HasRandom)
            return;

        if (current.OverrideRandomValues)
        {
            // Retain the callback-frame-derived OEM override once; regenerating it during a
            // dirty-chain recompute would make an unchanged event jump after every edit.
            current.RandomDirection = 1f;
            current.RandomStartRotation = Time.frameCount % 360;
            if (Visual != null && Visual.UseZPositionForAngleOffset)
                current.RandomStartRotation += Visual.transform.position.z * Visual.ZPositionAngleOffsetScale;
        }
        else
        {
            current.RandomDirection = Random.value < 0.5f ? 1f : -1f;
            current.RandomStartRotation = Random.Range(0f, 360f);
        }

        current.HasRandom = true;
    }

    private void ApplyRotationEvent(LightPairRotationStateData state, bool left)
    {
        var value = (float)state.Base.Value;
        var lockRotation = state.Base.CustomLockRotation == true;

        if (value > 0)
        {
            if (state.Base.CustomPreciseSpeed.HasValue)
                value = state.Base.CustomPreciseSpeed.Value;
            else if (state.Base.CustomSpeed.HasValue)
                value = state.Base.CustomSpeed.Value;
        }

        var direction = left ? state.RandomDirection : -state.RandomDirection;
        if (state.Base.CustomDirection.HasValue)
        {
            direction = state.Base.CustomDirection.Value == 0 ? 1f : -1f;
            if (!left)
                direction = -direction;
        }

        if (value == 0)
        {
            // Chroma's lock extension disables motion but deliberately retains the reached
            // angle; without lock, Beat Saber resets only the relevant side to its rest angle.
            if (left)
            {
                state.LeftEnabled = false;
                if (!lockRotation)
                    state.LeftAngle = state.LeftStartAngle;
            }
            else
            {
                state.RightEnabled = false;
                if (!lockRotation)
                    state.RightAngle = state.RightStartAngle;
            }

            return;
        }

        if (value < 0)
            return;

        // Positive events restart only their addressed side at the shared random offset,
        // then use Beat Saber's fixed twenty-degrees-per-song-second value scale.
        if (left)
        {
            state.LeftEnabled = true;
            if (!lockRotation)
                state.LeftAngle = state.RandomStartRotation + state.LeftStartAngle;
            state.LeftSpeed = value * 20f * direction;
        }
        else
        {
            state.RightEnabled = true;
            if (!lockRotation)
                state.RightAngle = -state.RandomStartRotation + state.RightStartAngle;
            state.RightSpeed = value * 20f * direction;
        }
    }

    private static void ApplySwitchEvent(LightPairRotationStateData state)
    {
        // OEM switches reposition both sides even when disabled and normalize their existing
        // speed signs; preserving enabled flags keeps stopped lasers stopped after the switch.
        state.LeftAngle = state.RandomStartRotation + state.LeftStartAngle;
        state.RightAngle = -state.RandomStartRotation + state.RightStartAngle;
        state.LeftSpeed = Mathf.Abs(state.LeftSpeed);
        state.RightSpeed = -Mathf.Abs(state.RightSpeed);
    }

    private static float GetAngle(float angle, float speed, bool enabled, float seconds) =>
        angle + (enabled ? speed * seconds : 0f);
}

// Every node owns a complete pair snapshot because either side or the switch can change the
// future of both transforms, and partial snapshots cannot be safely recomputed after edits.
public class LightPairRotationStateData : BasicMovementStateData
{
    public float LeftAngle;
    public float RightAngle;
    public float LeftSpeed;
    public float RightSpeed;
    public float RandomStartRotation;
    public float RandomDirection;
    public float LeftStartAngle;
    public float RightStartAngle;
    public bool LeftEnabled;
    public bool RightEnabled;
    public bool OverrideRandomValues;
    public bool HasRandom;
    public int SwitchEventIndex;

    public LightPairRotationStateData(BaseEvent data) : base(data)
    {
    }
}
