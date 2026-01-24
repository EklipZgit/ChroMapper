using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;

public class TrackLaneRingsRotation : MonoBehaviour
{
    public TrackLaneRingsManager Manager;
    public float StartupRotationAngle;
    public float StartupRotationStep;
    public int StartupRotationPropagationSpeed;
    public float StartupRotationFlexySpeed;

    public float RotationStep = 90;
    public bool CounterSpin;

    private List<RingRotationEffect> activeEffects;
    private List<RingRotationEffect> effectsPool;

    private void Awake()
    {
        activeEffects = new List<RingRotationEffect>(20);
        effectsPool = new List<RingRotationEffect>(20);
        for (var i = 0; i < effectsPool.Capacity; i++) effectsPool.Add(new RingRotationEffect());
    }

    public void DoReset() // Reset is a editor monobehaviour method
    {
        for (var i = activeEffects.Count - 1; i >= 0; i--)
        {
            RecycleRingRotationEffect(activeEffects[i]);
            activeEffects.RemoveAt(i);
        }

        foreach (var trackLaneRing in Manager.Rings) trackLaneRing.DoReset();
    }

    private void Start() =>
        AddRingRotationEvent(
            StartupRotationAngle,
            StartupRotationStep,
            StartupRotationPropagationSpeed,
            StartupRotationFlexySpeed,
            false,
            new BaseEvent());

    private void FixedUpdate()
    {
        var rings = Manager.Rings;
        var len = rings.Count;
        for (var i = activeEffects.Count - 1; i >= 0; i--)
        {
            var effect = activeEffects[i];
            var progress = (int)effect.ProgressPos;
            while (progress < effect.ProgressPos + effect.RotationPropagationSpeed && progress < len)
            {
                var destZ = effect.RotationAngle + (progress * effect.RotationStep);
                rings[progress].SetRotation(destZ, effect.RotationFlexySpeed);

                progress++;
            }

            effect.ProgressPos += effect.RotationPropagationSpeed;
            if (effect.ProgressPos >= len)
            {
                RecycleRingRotationEffect(activeEffects[i]);
                activeEffects.RemoveAt(i);
            }
        }
    }

    public void AddRingRotationEvent(
        float angle,
        float step,
        float propagationSpeed,
        float flexySpeed,
        float rotation,
        bool clockwise,
        bool counterSpinEvent)
    {
        var effect = SpawnRingRotationEffect();
        var multiplier = clockwise ? 1 : -1;
        effect.ProgressPos = 0;
        effect.RotationStep = step;
        effect.RotationPropagationSpeed = propagationSpeed;
        effect.RotationFlexySpeed = flexySpeed;

        if (CounterSpin && counterSpinEvent) multiplier *= -1;

        effect.RotationAngle = angle + (rotation * multiplier);
        activeEffects.Add(effect);
    }

    public void AddRingRotationEvent(
        float angle,
        float step,
        float propagationSpeed,
        float flexySpeed,
        bool clockwise,
        BaseEvent data)
    {
        var rotationStepLocal = RotationStep;
        var counterSpinEvent = false;

        if (data.CustomData != null)
        {
            // Chroma still applies multipliers to individual values so they should be set first
            if (data.CustomStep != null) step = data.CustomStep.Value;
            if (data.CustomProp != null) propagationSpeed = data.CustomProp.Value;
            if (data.CustomSpeed != null) flexySpeed = data.CustomSpeed.Value;
            if (data.CustomRingRotation != null) rotationStepLocal = data.CustomRingRotation.Value;

            if (data.CustomStepMult != null) step *= data.CustomStepMult.Value;
            if (data.CustomPropMult != null) propagationSpeed *= data.CustomPropMult.Value;
            if (data.CustomSpeedMult != null) flexySpeed *= data.CustomSpeedMult.Value;

            counterSpinEvent = data.CustomData.HasKey("_counterSpin") && data.CustomData["_counterSpin"].AsBool;
        }

        if (data.CustomData != null && data.CustomData.HasKey("_reset") && data.CustomData["_reset"] == true)
        {
            AddRingRotationEvent(angle, 0, 50, 50, 90, counterSpinEvent, false);
            return;
        }

        AddRingRotationEvent(
            angle,
            step,
            propagationSpeed,
            flexySpeed,
            rotationStepLocal,
            clockwise,
            counterSpinEvent);
    }

    private void RecycleRingRotationEffect(RingRotationEffect effect) => effectsPool.Add(effect);

    private RingRotationEffect SpawnRingRotationEffect()
    {
        RingRotationEffect result;
        if (effectsPool.Count > 0)
        {
            result = effectsPool[0];
            effectsPool.RemoveAt(0);
        }
        else
        {
            result = new RingRotationEffect();
        }

        return result;
    }

    private class RingRotationEffect
    {
        public float ProgressPos;

        public float RotationAngle;
        public float RotationFlexySpeed;
        public float RotationPropagationSpeed;
        public float RotationStep;
    }
}
