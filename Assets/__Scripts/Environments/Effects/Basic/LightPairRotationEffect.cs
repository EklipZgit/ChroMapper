using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightPairRotationEffect : BasicEventStateManager<LightRotationStateData>
{
    public Transform TransformL;
    public Transform TransformR;

    public int LeftEvent;
    public int RightEvent;
    public int SwitchEvent;

    public Vector3 RotationVector;

    public bool OverrideRandomValues;
    public bool UseZPositionForAngleOffset;
    public float ZPositionAngleOffsetScale;

    public float StartRotation;

    private int randomGenerationFrameNum = -1;
    private float randomStartRotation;
    private float randomDirection;

    private readonly Dictionary<int, LightPairRotationContainer> typeToContainer = new();
    private readonly List<LightPairRotationContainer> actives = new();
    private LightPairRotationContainer switchContainer;

    private void Awake()
    {
        foreach (var (type, tr, mirror) in new[]
            {
                (LeftEvent, TransformL, false), (RightEvent, TransformR, true), (SwitchEvent, null, false)
            }
            .Distinct())
        {
            var container = new LightPairRotationContainer
            {
                Speed = 0f, StartAngle = mirror ? -StartRotation : StartRotation, Mirror = mirror
            };

            if (tr != null)
            {
                container.HasTransform = true;
                container.Start = tr.rotation;
                container.Transform = tr;
                container.Transform.localRotation =
                    container.Start * Quaternion.Euler(RotationVector * container.StartAngle);
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
            container.Angle += dt * container.Speed;
            container.Transform.localRotation = container.Start * Quaternion.Euler(RotationVector * container.Angle);
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

    private void UpdateSwitchEvent(LightPairRotationContainer container)
    {
        var state = container.Container.CurrentState;
        OverrideRandomValues = container.Container.GetStateIndex(state) % 2 == 1;
        UpdateRandom();
        foreach (var active in actives)
        {
            active.Angle = active.Mirror
                ? 0f - randomStartRotation + active.StartAngle
                : randomStartRotation + active.StartAngle;
            active.Speed = active.Mirror ? 0f - Mathf.Abs(active.Speed) : Mathf.Abs(active.Speed);
            active.Transform.localRotation = active.Start * Quaternion.Euler(RotationVector * active.Angle);
        }
    }

    private void UpdateRotationEvent(LightPairRotationContainer container)
    {
        UpdateRandom();
        UpdateRotation(
            container,
            container.Mirror ? -randomStartRotation : randomStartRotation,
            container.Mirror ? -randomDirection : randomDirection);
    }

    private void UpdateRotation(
        LightPairRotationContainer container,
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
}

public class LightPairRotationContainer
{
    public bool Enabled;
    public bool Mirror;
    public bool HasTransform;

    public Transform Transform;
    public float Speed;
    public Quaternion Start;
    public float StartAngle;
    public float Angle;

    public readonly BasicEventStateChunksContainer<LightRotationStateData> Container = new();
}
