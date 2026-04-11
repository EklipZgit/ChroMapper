using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairSinMove : MonoBehaviour
{
    public LightRotationEffect LeftEffect;
    public LightRotationEffect RightEffect;
    public GenericCallbackEventEffect SwitchEffect;

    public TransformContainer[] Transforms = new TransformContainer[2];

    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    private int randomGenerationFrameNum = -1;
    private float randomStartOffset;

    private void Awake()
    {
        Transforms[0].Side = 1f;
        Transforms[1].Side = -1f;
        foreach (var container in Transforms)
        {
            container.Speed = 0f;
            container.StartPosition = container.Transform.localPosition;
            container.StartMovementValue = StartValueOffset;

            var vector = Vector3.LerpUnclamped(
                StartPositionOffset,
                EndPositionOffset,
                (Mathf.Sin(container.StartMovementValue) * 0.5f) + 0.5f);
            vector.x *= container.Side;
            container.Transform.localPosition = container.StartPosition + vector;
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
            container.MovementValue += dt * container.Speed;
            var vec = Vector3.LerpUnclamped(
                StartPositionOffset,
                EndPositionOffset,
                (Mathf.Sin(container.MovementValue) * 0.5f) + 0.5f);
            vec.x *= container.Side;
            container.Transform.localPosition = container.StartPosition + vec;
        }
    }

    private void HandleLeftStateChanged(LightRotationStateData state) => UpdateMoveEvent(state, Transforms[0]);
    private void HandleRightStateChanged(LightRotationStateData state) => UpdateMoveEvent(state, Transforms[1]);

    private void HandleSwitchStateChanged((int index, BasicEventStateData state) data)
    {
        randomGenerationFrameNum = -1;
        UpdateRandom();
        foreach (var c in Transforms)
        {
            c.MovementValue = randomStartOffset + c.StartMovementValue;
            c.Speed = Mathf.Abs(c.Speed);
        }
    }

    private void UpdateRandom()
    {
        var frameCount = Time.frameCount;
        if (randomGenerationFrameNum == frameCount) return;
        randomGenerationFrameNum = frameCount;
        randomStartOffset = OverrideRandomValues ? 0f : Random.Range(0f, MathF.PI * 2f);
    }

    private void UpdateMoveEvent(LightRotationStateData state, TransformContainer container)
    {
        UpdateRandom();
        UpdateMovement(state, container, randomStartOffset * container.Side);
    }

    private void UpdateMovement(
        LightRotationStateData state,
        TransformContainer container,
        float movementOffset)
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

            // if (evt.CustomDirection.HasValue)
            // {
            //     direction = evt.CustomDirection.Value == 0 ? 1f : -1f;
            //     if (container.Mirror) direction = -direction;
            // }
        }

        switch (value)
        {
            case 0:
                container.Enabled = false;
                if (lockRotation) return;
                var vector = Vector3.LerpUnclamped(
                    StartPositionOffset,
                    EndPositionOffset,
                    (Mathf.Sin(container.StartMovementValue) * 0.5f) + 0.5f);
                vector.x *= container.Side;
                container.Transform.localPosition = container.StartPosition + vector;
                break;
            case > 0:
                container.Enabled = true;
                if (!lockRotation)
                {
                    container.MovementValue = movementOffset + container.StartMovementValue;
                    var vec = Vector3.LerpUnclamped(
                        StartPositionOffset,
                        EndPositionOffset,
                        (Mathf.Sin(container.MovementValue) * 0.5f) + 0.5f);
                    vec.x *= container.Side;
                    container.Transform.localPosition = container.StartPosition + vec;
                }

                container.Speed = value;
                break;
        }
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public bool Enabled;

        [NonSerialized] public float Speed;
        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public float StartMovementValue;
        [NonSerialized] public float MovementValue;
        [NonSerialized] public float Side;
    }
}
