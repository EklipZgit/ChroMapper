using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairSinMoveEffect : BasicMovementEffect<LightPairSinMoveStateData>
{
    public int LeftEventType = -1;
    public int RightEventType = -1;
    public int SwitchEventType = -1;
    public LightPairSinMove Visual;

    // A timeline rebuild must reuse the phase chosen for an event time; otherwise an unrelated edit makes lasers jump.
    private readonly Dictionary<float, float> randomPhaseByTime = new();
    private readonly Dictionary<BaseEvent, float> switchRandomPhaseByEvent = new();

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<LightPairSinMove>();
    }

    public override void Initialize()
    {
        // Reinitialization replaces the event timeline, so retained event keys and
        // time-based random phases must not leak into the next map state.
        randomPhaseByTime.Clear();
        switchRandomPhaseByEvent.Clear();
        base.Initialize();
    }

    protected override LightPairSinMoveStateData CreateState(BaseEvent data) => new(data);

    protected override void ComputeSnapshot(LightPairSinMoveStateData previous, LightPairSinMoveStateData current)
    {
        if (previous == null)
        {
            // The sentinel describes both lasers before any event and preserves the environment's serialized override.
            var startValueOffset = Visual != null ? Visual.StartValueOffset : 0f;
            current.LeftPhase = startValueOffset;
            current.RightPhase = startValueOffset;
            current.LeftSpeed = 0f;
            current.RightSpeed = 0f;
            current.LeftEnabled = false;
            current.RightEnabled = false;
            current.OverrideRandomValues = Visual != null && Visual.OverrideRandomValues;
            current.SwitchEventCount = 0;
            current.RandomPhaseTime = float.NaN;
            return;
        }

        var deltaSeconds = Atsc.GetSecondsFromBeat(current.StartTime - previous.StartTime);
        current.LeftPhase = EvaluatePhase(previous.LeftPhase, previous.LeftSpeed, previous.LeftEnabled, deltaSeconds);
        current.RightPhase = EvaluatePhase(previous.RightPhase, previous.RightSpeed, previous.RightEnabled, deltaSeconds);
        current.LeftSpeed = previous.LeftSpeed;
        current.RightSpeed = previous.RightSpeed;
        current.LeftEnabled = previous.LeftEnabled;
        current.RightEnabled = previous.RightEnabled;
        current.OverrideRandomValues = previous.OverrideRandomValues;
        current.SwitchEventCount = previous.SwitchEventCount;
        current.RandomPhase = previous.RandomPhase;
        current.RandomPhaseTime = previous.RandomPhaseTime;

        var evt = current.Base;
        if (evt.Type == SwitchEventType)
        {
            // Beat Saber derives the switch from same-type index parity and deliberately rerolls that callback frame.
            current.OverrideRandomValues = (current.SwitchEventCount % 2) == 1;
            current.SwitchEventCount++;
            current.RandomPhase = GetSwitchRandomPhase(evt, current.OverrideRandomValues);
            current.RandomPhaseTime = current.StartTime;
            var startValueOffset = Visual != null ? Visual.StartValueOffset : 0f;
            current.LeftPhase = current.RandomPhase + startValueOffset;
            current.RightPhase = current.RandomPhase + startValueOffset;
            current.LeftSpeed = Mathf.Abs(current.LeftSpeed);
            // Preserve the game's switch behavior: the right speed is copied from the now-positive left speed.
            current.RightSpeed = Mathf.Abs(current.LeftSpeed);
            return;
        }

        // Same-time callbacks share the frame's phase, including a phase rerolled by a preceding switch callback.
        if (current.RandomPhaseTime != current.StartTime)
        {
            current.RandomPhase = GetRandomPhase(current.StartTime, current.OverrideRandomValues);
            current.RandomPhaseTime = current.StartTime;
        }

        if (evt.Type == LeftEventType)
            ApplyEvent(evt.Value, current.RandomPhase, ref current.LeftPhase, ref current.LeftSpeed, ref current.LeftEnabled);
        else if (evt.Type == RightEventType)
            ApplyEvent(evt.Value, -current.RandomPhase, ref current.RightPhase, ref current.RightSpeed, ref current.RightEnabled);
    }

    protected override void ApplyVisual(float beat, float seconds, LightPairSinMoveStateData current, LightPairSinMoveStateData next)
    {
        if (Visual == null)
            return;

        // Song seconds, rather than frame deltas, reproduce the same sine phase at every arbitrary editor playhead.
        var leftPhase = EvaluatePhase(current.LeftPhase, current.LeftSpeed, current.LeftEnabled, seconds);
        var rightPhase = EvaluatePhase(current.RightPhase, current.RightSpeed, current.RightEnabled, seconds);
        Visual.Apply(leftPhase, rightPhase);
    }

    private float GetRandomPhase(float eventTime, bool overrideRandomValues)
    {
        if (overrideRandomValues)
            return 0f;

        // Events at one timeline time represent one callback frame and therefore share one random phase.
        if (!randomPhaseByTime.TryGetValue(eventTime, out var phase))
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            randomPhaseByTime.Add(eventTime, phase);
        }

        return phase;
    }

    private float GetSwitchRandomPhase(BaseEvent evt, bool overrideRandomValues)
    {
        if (overrideRandomValues)
            return 0f;

        // A switch invalidates the shared frame phase in the game, but its replacement must remain stable on recompute.
        if (!switchRandomPhaseByEvent.TryGetValue(evt, out var phase))
        {
            phase = Random.Range(0f, Mathf.PI * 2f);
            switchRandomPhaseByEvent.Add(evt, phase);
        }

        return phase;
    }

    private void ApplyEvent(int value, float phaseOffset, ref float phase, ref float speed, ref bool enabled)
    {
        var startValueOffset = Visual != null ? Visual.StartValueOffset : 0f;
        if (value == 0)
        {
            // A zero event resets position but retains speed because a later switch reads the stored game speed.
            enabled = false;
            phase = startValueOffset;
        }
        else if (value > 0)
        {
            enabled = true;
            phase = phaseOffset + startValueOffset;
            speed = value * 1f;
        }
    }

    private static float EvaluatePhase(float phase, float speed, bool enabled, float seconds) =>
        phase + (enabled ? speed * seconds : 0f);
}

public class LightPairSinMoveStateData : BasicMovementStateData
{
    public float LeftPhase;
    public float RightPhase;
    public float LeftSpeed;
    public float RightSpeed;
    public bool LeftEnabled;
    public bool RightEnabled;
    public bool OverrideRandomValues;
    public int SwitchEventCount;
    public float RandomPhase;
    public float RandomPhaseTime;

    public LightPairSinMoveStateData(BaseEvent data) : base(data)
    {
    }
}
