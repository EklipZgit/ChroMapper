using System;
using System.Collections.Generic;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairRotationEffect : BasicEventStateManager<LightRotationStateData>
{
    public TransformContainer[] Transforms = new TransformContainer[2];

    public Vector3 RotationVector;

    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;

    public float StartRotation;

    private int randomGenerationFrameNum = -1;
    private float randomStartRotation;
    private float randomDirection;

    private bool hasInitialized;

    private readonly Dictionary<int, TransformContainer> typeToContainer = new();
    private readonly TransformContainer switchContainer = new();

    private void Awake()
    {
        Transforms[1].Mirror = true;
        foreach (var container in Transforms)
        {
            container.HasTransform = true;

            container.StartAngle = container.Mirror ? -StartRotation : StartRotation;
            container.Start = container.Transform.rotation;
            container.Transform.localRotation =
                container.Start * Quaternion.Euler(RotationVector * container.StartAngle);
        }
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
        for (var index = 0; index < Transforms.Length; index++)
        {
            var active = Transforms[index];
            if (!active.Container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateRotationEvent(active);
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

    private void UpdateSwitchEvent(TransformContainer container)
    {
        var state = container.Container.CurrentState;
        OverrideRandomValues = container.Container.GetStateIndex(state) % 2 == 1;
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

    private void UpdateRotationEvent(TransformContainer container)
    {
        UpdateRandom();
        UpdateRotation(
            container,
            container.Mirror ? -randomStartRotation : randomStartRotation,
            container.Mirror ? -randomDirection : randomDirection);
    }

    private void UpdateRotation(
        TransformContainer container,
        float startOffset,
        float direction)
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
            UpdateRotationEvent(container);
        else
            UpdateSwitchEvent(container);
    }

    public override void UpdateDirty()
    {
        foreach (var container in typeToContainer.Values)
        {
            if (container.HasTransform)
                UpdateRotationEvent(container);
            else
                UpdateSwitchEvent(container);
        }
    }

    [Serializable]
    public class TransformContainer
    {
        public Transform Transform;

        [NonSerialized] public bool Enabled;
        [NonSerialized] public bool Mirror;
        [NonSerialized] public bool HasTransform;

        [NonSerialized] public float Speed;
        [NonSerialized] public Quaternion Start;
        [NonSerialized] public float StartAngle;
        [NonSerialized] public float Angle;

        public readonly BasicEventStateChunksContainer<LightRotationStateData> Container = new();
    }
}
