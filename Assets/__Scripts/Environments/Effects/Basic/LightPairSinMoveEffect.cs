using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairSinMoveEffect : BasicEventStateManager<LightRotationStateData>
{
    public TransformContainer[] Transforms = new TransformContainer[2];

    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    private int randomGenerationFrameNum = -1;
    private float randomStartOffset;

    private readonly Dictionary<int, TransformContainer> typeToContainer = new();
    private readonly TransformContainer switchContainer = new();

    private void Awake()
    {
        Transforms[0].Side = 1f;
        Transforms[1].Side = -1f;
        foreach (var container in Transforms)
        {
            container.HasTransform = true;

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

    public override void Initialize()
    {
        typeToContainer.Clear();
        foreach (var (type, container) in new[]
            {
                (Types[0], Transforms[0]), (Types[1], Transforms[1]), (Types.Count > 2 ? Types[2] : -1, switchContainer)
            })
        {
            InitializeStates(container.Container);
            if (type == -1) continue;
            typeToContainer[type] = container;
        }
    }

    public override void UpdateTime(float currentTime)
    {
        if (!switchContainer.Container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying))
            UpdateSwitchEvent(switchContainer);
        for (var i = 0; i < Transforms.Length; i++)
        {
            var active = Transforms[i];
            if (!active.Container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateMoveEvent(active);
        }
    }

    private void UpdateRandom()
    {
        var frameCount = Time.frameCount;
        if (randomGenerationFrameNum == frameCount) return;
        randomGenerationFrameNum = frameCount;
        randomStartOffset = OverrideRandomValues ? 0f : Random.Range(0f, MathF.PI * 2f);
    }

    private void UpdateSwitchEvent(TransformContainer container)
    {
        var state = container.Container.CurrentState;
        OverrideRandomValues = container.Container.GetStateIndex(state) % 2 == 1;
        randomGenerationFrameNum = -1;
        UpdateRandom();
        foreach (var c in Transforms)
        {
            c.MovementValue = randomStartOffset + c.StartMovementValue;
            c.Speed = Mathf.Abs(c.Speed);
        }
    }

    private void UpdateMoveEvent(TransformContainer container)
    {
        UpdateRandom();
        UpdateMovement(container, randomStartOffset * container.Side);
    }

    private void UpdateMovement(
        TransformContainer container,
        float movementOffset)
    {
        var state = container.Container.CurrentState;
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
                container.Enabled = !evt.CustomLockRotation.HasValue || lockRotation;
                container.MovementValue = movementOffset + container.StartMovementValue;
                var vec = Vector3.LerpUnclamped(
                    StartPositionOffset,
                    EndPositionOffset,
                    (Mathf.Sin(container.MovementValue) * 0.5f) + 0.5f);
                vec.x *= container.Side;
                container.Transform.localPosition = container.StartPosition + vec;
                container.Speed = value;
                break;
        }
    }

    protected override LightRotationStateData CreateState(BaseEvent data) => new(data);

    public override void BuildFromData(IEnumerable<BaseEvent> dataList)
    {
        foreach (var data in dataList) InsertData(data);
    }

    public override void InsertData(BaseEvent data)
    {
        var state = CreateState(data);
        state.StartTime = data.SongBpmTime;
        var container = typeToContainer[data.Type];
        HandleInsertState(container.Container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var container = typeToContainer[original.Type];
        var state = HandleRemoveState(container.Container, data, original);
        if (container.Container.CurrentState != state) return;
        container.Container.SetStateAt(data.SongBpmTime);
        if (container.HasTransform)
            UpdateMoveEvent(container);
        else
            UpdateSwitchEvent(container);
    }

    public override void UpdateDirty()
    {
        foreach (var container in typeToContainer.Values)
        {
            if (container.HasTransform)
                UpdateMoveEvent(container);
            else
                UpdateSwitchEvent(container);
        }
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public bool Enabled;
        [NonSerialized] public bool HasTransform;

        [NonSerialized] public float Speed;
        [NonSerialized] public Vector3 StartPosition;
        [NonSerialized] public float StartMovementValue;
        [NonSerialized] public float MovementValue;
        [NonSerialized] public float Side;

        public readonly BasicEventStateChunksContainer<LightRotationStateData> Container = new();
    }
}
