using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class TrackLaneRingsRotationEffect : BasicMovementEffect<TrackLaneRingsRotationStateData>
{
    public TrackLaneRingsRotation Visual;

    public float Rotation;
    public float Step;
    public RotationStepType StepType;
    public float PropagationSpeed;
    public float FlexySpeed;

    // Rendering runs every frame, so its scratch state is retained instead of allocating
    // arrays while evaluating the two fixed frames surrounding the playhead.
    // Packing related values cuts each snapshot and retained evaluator from three arrays to one.
    private RingRotationState[] evaluationRingStates;
    private float[] evaluationPreviousRotations;
    private RingRotationWave[] evaluationWaves;
    private TrackLaneRingsRotationStateData evaluationState;
    private int evaluationFrame;
    private int evaluationWaveCount;
    private bool evaluationValid;
    private bool isPlaying;
    private float appliedFramePosition = float.NaN;

    private void Awake()
    {
        if (Visual == null)
            Visual = GetComponent<TrackLaneRingsRotation>();

        if (Visual != null)
        {
            Visual.enabled = false;
            if (Visual.Manager != null)
                Visual.Manager.UseCached = true;
        }
    }

    protected override TrackLaneRingsRotationStateData CreateState(BaseEvent data) => new(data);

    public override void UpdateTime(bool isPlaying, float currentTime)
    {
        this.isPlaying = isPlaying;

        // Both play and pause must use one deterministic state evaluator. Switching
        // to the old live queue on resume cannot reconstruct overlapping propagation.
        if (Visual != null)
        {
            Visual.enabled = false;
            if (Visual.Manager != null)
                Visual.Manager.UseCached = true;
        }

        base.UpdateTime(isPlaying, currentTime);
    }

    protected override void ComputeSnapshot(TrackLaneRingsRotationStateData previous, TrackLaneRingsRotationStateData current)
    {
        // An edit can recompute an existing state object in place, so invalidate any
        // retained live evaluator that was derived from its old snapshot contents.
        if (evaluationState == current)
        {
            evaluationValid = false;
            appliedFramePosition = float.NaN;
        }

        var ringCount = Visual != null && Visual.Manager != null ? Visual.Manager.Rings.Count : 0;
        if (ringCount == 0)
            return;

        EnsureSnapshotArrays(current, ringCount, previous != null ? previous.ActiveWaveCount + 1 : 1);

        if (previous == null)
        {
            for (var i = 0; i < ringCount; i++)
            {
                var rotation = Visual.Manager.Rings[i].GetRotation();
                current.RingStates[i] = new RingRotationState
                {
                    Rotation = rotation,
                    Destination = rotation,
                    Speed = 0f
                };
            }

            current.ActiveWaveCount = 0;
            current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
            current.SnapshotFrame = Mathf.FloorToInt(current.SnapshotSeconds / Time.fixedDeltaTime);
            current.FirstRingDest = Visual.StartupRotationAngle;
            current.Step = Visual.StartupRotationStep;
            current.Rotation = Visual.StartupRotationAngle;
            current.Propagation = Visual.StartupRotationPropagationSpeed;
            current.Speed = Visual.StartupRotationFlexySpeed;
            current.Clockwise = false;
            current.CounterSpin = false;
            current.HasRandom = false;

            // Beat Saber and Chroma both create the serialized startup buildup as an
            // independent wave before authored callbacks begin.
            AddWave(current);
            return;
        }

        // Only unfinished wave cursors are copied; expanding their future ring work here
        // made event insertion proportional to every assignment that had not happened yet.
        for (var i = 0; i < ringCount; i++)
            current.RingStates[i] = previous.RingStates[i];

        current.ActiveWaveCount = previous.ActiveWaveCount;
        for (var i = 0; i < current.ActiveWaveCount; i++)
            current.ActiveWaves[i] = previous.ActiveWaves[i];

        current.SnapshotSeconds = Atsc.GetSecondsFromBeat(current.StartTime);
        current.SnapshotFrame = Mathf.FloorToInt(current.SnapshotSeconds / Time.fixedDeltaTime);
        AdvanceState(
            current.RingStates,
            current.ActiveWaves,
            ref current.ActiveWaveCount,
            previous.SnapshotFrame,
            current.SnapshotFrame,
            ringCount);

        current.Rotation = current.Base.CustomRingRotation ?? Rotation;

        // Snapshot recomputes must reuse the originally selected random values or edits
        // elsewhere in the chain would make unchanged authored events drift.
        if (!current.HasRandom)
        {
            current.RandomStep = GetRandomStep();
            current.Clockwise = Random.value >= 0.5f;
            current.CounterSpin = current.Base.CustomData != null
                && current.Base.CustomData.HasKey("_counterSpin")
                && current.Base.CustomData["_counterSpin"].AsBool;
            current.HasRandom = true;
        }

        // Start from the retained random choice on every recompute so a step multiplier
        // cannot compound when an earlier edited event invalidates this snapshot.
        current.Step = current.RandomStep;
        if (current.Base.CustomStep.HasValue)
            current.Step = current.Base.CustomStep.Value;
        if (current.Base.CustomStepMult.HasValue)
            current.Step *= current.Base.CustomStepMult.Value;

        if (current.Base.CustomDirection.HasValue)
            current.Clockwise = current.Base.CustomDirection.Value == 0;

        var prop = current.Base.CustomProp ?? PropagationSpeed;
        if (current.Base.CustomPropMult.HasValue)
            prop *= current.Base.CustomPropMult.Value;
        // Chroma propagates with the authored float exactly. Non-positive values never
        // advance its active wave, so they intentionally produce no assignments here.
        current.Propagation = prop;

        current.Speed = current.Base.CustomSpeed ?? FlexySpeed;
        if (current.Base.CustomSpeedMult.HasValue)
            current.Speed *= current.Base.CustomSpeedMult.Value;

        // Chroma's reset branch ignores custom rotation parameters and emits the
        // environment's base rotation with its hard-coded immediate wave settings.
        var reset = current.Base.CustomData != null
            && current.Base.CustomData.HasKey("_reset")
            && current.Base.CustomData["_reset"].AsBool;
        if (reset)
        {
            current.Rotation = Rotation;
            current.Step = 0f;
            current.Propagation = 50f;
            current.Speed = 50f;
        }

        var signed = current.Rotation * (current.Clockwise ? 1f : -1f);
        if (Visual.CounterSpin && current.CounterSpin)
            signed *= -1f;

        // Chroma asks the actual first ring for its currently assigned destination.
        // This normally accumulates targets, but also preserves the OEM edge case where
        // multiple callbacks arrive before an earlier wave's first FixedTick.
        current.FirstRingDest = current.RingStates[0].Destination + signed;

        // Keep the float cursor itself: truncating it afresh each tick preserves Chroma's
        // repeated fractional assignments without pre-expanding the rest of the wave.
        AddWave(current);
    }

    protected override void ApplyVisual(float beat, float seconds, TrackLaneRingsRotationStateData current, TrackLaneRingsRotationStateData next)
    {
        if (Visual == null || Visual.Manager == null)
            return;

        var rings = Visual.Manager.Rings;
        var ringCount = rings.Count;
        EnsureEvaluationArrays(ringCount, current.ActiveWaveCount);

        // Absolute song frames keep interpolation phase stable across event boundaries;
        // relative event fractions visibly jump whenever an event is off the fixed grid.
        // BasicMovementEffect already converted the interval to song seconds; retain the
        // snapshot's absolute seconds to avoid a second BPM lookup per cloned ring system.
        var framePosition = (current.SnapshotSeconds + seconds) / Time.fixedDeltaTime;
        if (isPlaying
            && framePosition == appliedFramePosition
            && evaluationValid
            && evaluationState == current)
        {
            return;
        }

        var frame = Mathf.FloorToInt(framePosition);

        // Only continuous playback may reuse mutable evaluator cursors. Paused stepping,
        // rewind, and seeks always rebuild from the immutable snapshot so the result cannot
        // depend on which direction or sequence of editor navigation reached this time.
        var canAdvanceIncrementally = isPlaying
            && evaluationValid
            && evaluationState == current
            && frame >= evaluationFrame
            && frame <= evaluationFrame + 1;
        if (!canAdvanceIncrementally)
        {
            evaluationValid = false;
            for (var i = 0; i < ringCount; i++)
                evaluationRingStates[i] = current.RingStates[i];

            evaluationWaveCount = current.ActiveWaveCount;
            for (var i = 0; i < evaluationWaveCount; i++)
                evaluationWaves[i] = current.ActiveWaves[i];

            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                current.SnapshotFrame,
                frame,
                ringCount);
        }
        else if (frame > evaluationFrame)
        {
            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                evaluationFrame + 1,
                frame,
                ringCount);
        }

        if (!evaluationValid || evaluationState != current || frame != evaluationFrame)
        {
            for (var i = 0; i < ringCount; i++)
                evaluationPreviousRotations[i] = evaluationRingStates[i].Rotation;

            AdvanceState(
                evaluationRingStates,
                evaluationWaves,
                ref evaluationWaveCount,
                frame,
                frame + 1,
                ringCount);
            evaluationState = current;
            evaluationFrame = frame;
            evaluationValid = true;
        }

        appliedFramePosition = framePosition;
        var interpolation = framePosition - frame;
        for (var i = 0; i < ringCount; i++)
        {
            var rotation = Mathf.Lerp(evaluationPreviousRotations[i], evaluationRingStates[i].Rotation, interpolation);
            // localEulerAngles immediately converts through Quaternion.Euler before calling
            // localRotation; perform that conversion directly to remove one native wrapper
            // from each of the thousands of ring transform writes shown in the profiler.
            rings[i].CachedTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        }
    }

    private static void EnsureSnapshotArrays(TrackLaneRingsRotationStateData state, int ringCount, int waveCapacity)
    {
        if (state.RingStates == null || state.RingStates.Length != ringCount)
            state.RingStates = new RingRotationState[ringCount];
        if (state.ActiveWaves == null || state.ActiveWaves.Length < waveCapacity)
            state.ActiveWaves = new RingRotationWave[waveCapacity];
    }

    private void EnsureEvaluationArrays(int ringCount, int waveCapacity)
    {
        if (evaluationRingStates == null || evaluationRingStates.Length != ringCount)
        {
            evaluationValid = false;
            evaluationRingStates = new RingRotationState[ringCount];
            evaluationPreviousRotations = new float[ringCount];
        }

        // This grows only with overlapping waves and is reused by every rendered frame.
        if (evaluationWaves == null || evaluationWaves.Length < waveCapacity)
            evaluationWaves = new RingRotationWave[waveCapacity];
    }

    // Assignments happen before their fixed tick's lerp. Wave creation order is retained,
    // then traversed backwards because Chroma processes active effects newest-to-oldest.
    private static void AdvanceState(
        RingRotationState[] ringStates,
        RingRotationWave[] waves,
        ref int waveCount,
        int fromFrame,
        int toFrame,
        int ringCount)
    {
        if (toFrame <= fromFrame)
            return;

        var frame = fromFrame;
        while (waveCount > 0)
        {
            var assignmentFrame = int.MaxValue;
            for (var i = 0; i < waveCount; i++)
                assignmentFrame = Mathf.Min(assignmentFrame, waves[i].NextFrame);

            if (assignmentFrame > toFrame)
                break;

            // Usually active waves assign every tick; this still jumps any empty gap.
            LerpAll(ringStates, assignmentFrame - frame - 1, ringCount);
            for (var i = waveCount - 1; i >= 0; i--)
            {
                var wave = waves[i];
                if (wave.NextFrame != assignmentFrame)
                    continue;

                // Chroma truncates the old float ProgressPos before advancing it, so a
                // fractional wave can deliberately assign the same ring on many ticks.
                var ring = (long)wave.Progress;
                wave.Progress += wave.Propagation;
                while (ring < wave.Progress && ring < ringCount)
                {
                    var ringIndex = (int)ring;
                    ringStates[ringIndex].Destination = wave.FirstRingDestination + (ring * wave.Step);
                    ringStates[ringIndex].Speed = wave.Speed;
                    ring++;
                }

                wave.NextFrame++;
                waves[i] = wave;
            }

            RemoveCompletedWaves(waves, ref waveCount, ringCount);
            LerpAll(ringStates, 1, ringCount);
            frame = assignmentFrame;
        }

        LerpAll(ringStates, toFrame - frame, ringCount);
    }

    private static void RemoveCompletedWaves(RingRotationWave[] waves, ref int waveCount, int ringCount)
    {
        var destination = 0;
        for (var source = 0; source < waveCount; source++)
        {
            if (!(waves[source].Progress < ringCount))
                continue;

            waves[destination++] = waves[source];
        }

        waveCount = destination;
    }

    private static void LerpAll(RingRotationState[] ringStates, int frames, int ringCount)
    {
        if (frames <= 0)
            return;

        // Live playback advances one fixed tick at a time. Avoiding Mathf.Pow in that
        // dominant path matches TrackLaneRing.FixedUpdateRing and removes one expensive
        // transcendental call per animated ring from the profiler's rotation hot path.
        if (frames == 1)
        {
            var fixedDeltaTime = Time.fixedDeltaTime;
            for (var i = 0; i < ringCount; i++)
            {
                var ringState = ringStates[i];
                ringState.Rotation = Mathf.Lerp(
                    ringState.Rotation,
                    ringState.Destination,
                    fixedDeltaTime * ringState.Speed);
                ringStates[i] = ringState;
            }
            return;
        }

        for (var i = 0; i < ringCount; i++)
        {
            var ringState = ringStates[i];
            ringState.Rotation = LerpDiscrete(
                ringState.Rotation,
                ringState.Destination,
                ringState.Speed,
                frames);
            ringStates[i] = ringState;
        }
    }

    private static void AddWave(TrackLaneRingsRotationStateData state)
    {
        if (state.Propagation <= 0f)
            return;

        state.ActiveWaves[state.ActiveWaveCount++] = new RingRotationWave
        {
            NextFrame = state.SnapshotFrame + 1,
            Progress = 0f,
            FirstRingDestination = state.FirstRingDest,
            Step = state.Step,
            Propagation = state.Propagation,
            Speed = state.Speed
        };
    }

    // Closed-form fixed-step movement avoids replaying every tick between distant
    // assignments while retaining Mathf.Lerp's clamped fixedDeltaTime * speed factor.
    private static float LerpDiscrete(float start, float dest, float speed, int frames)
    {
        if (frames <= 0)
            return start;

        var factor = 1f - Mathf.Clamp01(Time.fixedDeltaTime * speed);
        return dest - ((dest - start) * Mathf.Pow(factor, frames));
    }

    private float GetRandomStep() => StepType switch
    {
        RotationStepType.Range0ToMax => Random.Range(0f, Step),
        RotationStepType.Range => Random.Range(0f - Step, Step),
        RotationStepType.MaxOr0 => Random.value < 0.5f ? Step : 0f,
        _ => 0f
    };
}

public struct RingRotationState
{
    public float Rotation;
    public float Destination;
    public float Speed;
}

public struct RingRotationWave
{
    public int NextFrame;
    public float Progress;
    public float FirstRingDestination;
    public float Step;
    public float Propagation;
    public float Speed;
}

public class TrackLaneRingsRotationStateData : BasicMovementStateData
{
    public RingRotationState[] RingStates;
    public RingRotationWave[] ActiveWaves;
    public int ActiveWaveCount;
    public int SnapshotFrame;
    public float SnapshotSeconds;
    public float FirstRingDest;
    public float RandomStep;
    public float Step;
    public float Rotation;
    public float Propagation;
    public float Speed;
    public bool Clockwise;
    public bool CounterSpin;
    public bool HasRandom;

    public TrackLaneRingsRotationStateData(BaseEvent data) : base(data)
    {
    }
}

public enum RotationStepType : byte
{
    Range0ToMax,
    Range,
    MaxOr0
}
