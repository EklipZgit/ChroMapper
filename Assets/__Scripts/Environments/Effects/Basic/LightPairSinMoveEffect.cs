using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairSinMoveEffect : BasicEventStateManager<LightRotationStateData>
{
    public Transform TransformL;
    public Transform TransformR;

    public int LeftEvent;
    public int RightEvent;
    public int SwitchEvent;

    public bool OverrideRandomValues;
    public float StartValueOffset;
    public Vector3 StartPositionOffset;
    public Vector3 EndPositionOffset;

    private int randomGenerationFrameNum = -1;
    private float randomStartOffset;

    private readonly Dictionary<int, TransformContainer> typeToContainer = new();
    private readonly List<TransformContainer> actives = new();
    private TransformContainer switchContainer;

    private void Awake()
    {
        foreach (var (type, tr, mirror) in new[]
            {
                (LeftEvent, TransformL, false), (RightEvent, TransformR, true), (SwitchEvent, null, false)
            }
            .Distinct())
        {
            var container = new TransformContainer { Mirror = mirror, Speed = 0f, Side = mirror ? -1f : 1f };

            if (tr != null)
            {
                container.HasTransform = true;
                container.StartPosition = tr.localPosition;
                container.StartMovementValue = StartValueOffset;
                container.Transform = tr;

                var vector = Vector3.LerpUnclamped(
                    StartPositionOffset,
                    EndPositionOffset,
                    (Mathf.Sin(container.StartMovementValue) * 0.5f) + 0.5f);
                vector.x *= container.Side;
                container.Transform.localPosition = container.StartPosition + vector;

                actives.Add(container);
            }
            else
                switchContainer = container;

            typeToContainer[type] = container;
        }
    }

    private void Update()
    {
        var dt = Time.deltaTime;
        for (var i = 0; i < actives.Count; i++)
        {
            var container = actives[i];
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
        foreach (var container in typeToContainer.Values) InitializeStates(container.Container);
    }

    public override void UpdateTime(float currentTime)
    {
        if (!switchContainer.Container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying))
            UpdateSwitchEvent(switchContainer);
        for (var index = 0; index < actives.Count; index++)
        {
            var active = actives[index];
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
        foreach (var active in actives)
        {
            active.MovementValue = randomStartOffset + active.StartMovementValue;
            active.Speed = Mathf.Abs(active.Speed);
        }
    }

    private void UpdateMoveEvent(TransformContainer container)
    {
        UpdateRandom();
        UpdateMovement(container, container.Mirror ? -randomStartOffset : randomStartOffset);
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

    private class TransformContainer
    {
        public bool Enabled;
        public bool Mirror;
        public bool HasTransform;

        public float Speed;
        public Vector3 StartPosition;
        public Transform Transform;
        public float StartMovementValue;
        public float MovementValue;
        public float Side;

        public readonly BasicEventStateChunksContainer<LightRotationStateData> Container = new();
    }
}
