using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using UnityEngine;
using Random = UnityEngine.Random;

public class LightRotationEffect : BasicEventStateManager<LightRotationStateData>
{
    public Vector3 RotationVector;
    public float SpeedMultiplier;

    private Transform tr;
    private Quaternion startRotation;
    private float speed;

    private readonly BasicEventStateChunksContainer<LightRotationStateData> container = new();

    private void Start()
    {
        tr = transform;
        startRotation = tr.rotation;
        enabled = false;
    }

    private void Update() => tr.Rotate(RotationVector, Time.deltaTime * speed, Space.Self);

    public override void Initialize() => InitializeStates(container);

    public override void UpdateTime(float currentTime)
    {
        if (!container.IsCurrentOrFindState(currentTime, Atsc.IsPlaying)) UpdateRotation();
    }

    private void UpdateRotation()
    {
        var state = container.CurrentState;
        var evt = state.Base;
        float value = evt.Value;

        var direction = Random.value < 0.5f ? 1f : -1f;
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

            if (evt.CustomDirection.HasValue) direction = evt.CustomDirection.Value == 0 ? 1f : -1f;
        }

        switch (value)
        {
            case 0:
                enabled = false;
                if (lockRotation) return;
                tr.localRotation = startRotation;
                break;
            case > 0:
                tr.localRotation = startRotation;
                tr.Rotate(RotationVector, Random.Range(0f, 180f), Space.Self);
                enabled = !evt.CustomLockRotation.HasValue || lockRotation;
                speed = value * SpeedMultiplier * 20f * direction;
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
        HandleInsertState(container, state);
    }

    public override void RemoveData(BaseEvent data, BaseEvent original)
    {
        var state = HandleRemoveState(container, data, original);
        if (container.CurrentState != state) return;
        container.SetStateAt(data.SongBpmTime);
    }

    public override void UpdateDirty() => UpdateRotation();
}

public class LightRotationStateData : BasicEventStateData
{
    public LightRotationStateData(BaseEvent data) : base(data)
    {
    }
}
