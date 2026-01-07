using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairRotation : MonoBehaviour
{
    public LightRotationEffect LeftEffect;
    public LightRotationEffect RightEffect;
    public GenericCallbackEventEffect SwitchEffect;

    public TransformContainer[] Transforms = new TransformContainer[2];

    public Vector3 RotationVector;

    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;

    public float StartRotation;

    private int randomGenerationFrameNum = -1;
    private float randomStartRotation;
    private float randomDirection;

    private void Awake()
    {
        Transforms[1].Mirror = true;
        foreach (var container in Transforms)
        {
            container.StartAngle = container.Mirror ? -StartRotation : StartRotation;
            container.Start = container.Transform.rotation;
            container.Transform.localRotation =
                container.Start * Quaternion.Euler(RotationVector * container.StartAngle);
        }
    }

    private void Start()
    {
        if (LeftEffect != null) LeftEffect.OnStateChanged += HandleLeftStateChanged;
        if (RightEffect != null) RightEffect.OnStateChanged += HandleRightStateChanged;
        if (SwitchEffect != null) SwitchEffect.OnStateChanged += HandleSwitchStateChanged;
    }

    private void OnDestroy()
    {
        if (LeftEffect != null) LeftEffect.OnStateChanged -= HandleLeftStateChanged;
        if (RightEffect != null) RightEffect.OnStateChanged -= HandleRightStateChanged;
        if (SwitchEffect != null) SwitchEffect.OnStateChanged -= HandleSwitchStateChanged;
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        for (var i = 0; i < Transforms.Length; i++)
        {
            var container = Transforms[i];
            if (!container.Enabled) continue;
            container.Angle += dt * container.Speed;
            container.Transform.localRotation = container.Start * Quaternion.Euler(RotationVector * container.Angle);
        }
    }

    private void HandleLeftStateChanged(LightRotationStateData state) => UpdateRotationEvent(state, Transforms[0]);
    private void HandleRightStateChanged(LightRotationStateData state) => UpdateRotationEvent(state, Transforms[1]);

    private void HandleSwitchStateChanged((int index, BasicEventStateData state) data)
    {
        OverrideRandomValues = data.index % 2 == 1;
        UpdateRandom();
        foreach (var active in Transforms)
        {
            active.Angle = active.Mirror
                ? 0f - randomStartRotation + active.StartAngle
                : randomStartRotation + active.StartAngle;
            active.Speed = active.Mirror ? 0f - Mathf.Abs(active.Speed) : Mathf.Abs(active.Speed);
            active.Transform.localRotation = active.Start * Quaternion.Euler(RotationVector * active.Angle);
        }
    }

    private void UpdateRandom()
    {
        var frameCount = Time.frameCount;
        if (randomGenerationFrameNum != frameCount)
        {
            randomGenerationFrameNum = frameCount;
            if (OverrideRandomValues)
            {
                randomDirection = 1f;
                randomStartRotation = frameCount % 360;
                if (UseZPositionForAngleOffset) randomStartRotation += transform.position.z * ZPositionAngleOffsetScale;
            }
            else
            {
                randomDirection = Random.value < 0.5f ? 1f : -1f;
                randomStartRotation = Random.Range(0f, 360f);
            }
        }
    }

    private void UpdateRotationEvent(LightRotationStateData state, TransformContainer container)
    {
        UpdateRandom();
        UpdateRotation(
            state,
            container,
            container.Mirror ? -randomStartRotation : randomStartRotation,
            container.Mirror ? -randomDirection : randomDirection);
    }

    private void UpdateRotation(
        LightRotationStateData state,
        TransformContainer container,
        float startOffset,
        float direction)
    {
        var evt = state.Base;
        float value = evt.Value;

        var lockRotation = false;
        if (evt.CustomData != null)
        {
            if (evt.CustomLockRotation.HasValue) lockRotation = evt.CustomLockRotation.Value;

            if (value > 0)
            {
                if (evt.CustomPreciseSpeed.HasValue)
                    value = evt.CustomPreciseSpeed.Value;
                else if (evt.CustomSpeed.HasValue) value = evt.CustomSpeed.Value;
            }

            if (evt.CustomDirection.HasValue)
            {
                direction = evt.CustomDirection.Value == 0 ? 1f : -1f;
                if (container.Mirror) direction = -direction;
            }
        }

        switch (value)
        {
            case 0:
                container.Enabled = false;
                if (lockRotation) return;
                container.Transform.localRotation =
                    container.Start * Quaternion.Euler(RotationVector * container.StartAngle);
                break;
            case > 0:
                container.Enabled = !evt.CustomLockRotation.HasValue || lockRotation;
                container.Angle = startOffset + container.StartAngle;
                container.Transform.localRotation =
                    container.Start * Quaternion.Euler(RotationVector * container.Angle);
                container.Speed = value * 20f * direction;
                break;
        }
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public bool Enabled;
        [NonSerialized] public bool Mirror;

        [NonSerialized] public float Speed;
        [NonSerialized] public Quaternion Start;
        [NonSerialized] public float StartAngle;
        [NonSerialized] public float Angle;
    }
}
