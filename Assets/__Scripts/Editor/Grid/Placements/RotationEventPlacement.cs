using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class
    RotationEventPlacement : BasePlacement<BaseRotationEvent, RotationEventContainer, RotationEventGridContainer>
{
    [SerializeField] private BeatmapRotationInputController rotationInputController;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    private bool earlyRotationPlaceNow;
    private bool negativeRotations;
    public float QueuedRotation = 30f;

    public override void Start()
    {
        base.Start();
        rotationInputController.OnRotationInput += UpdateRotation;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed an Event.");

    protected override BaseRotationEvent GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        QueuedData.Type = Math.Clamp(
            (int)EventTypeValue.EarlyRotationEventType
            + Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x),
            (int)EventTypeValue.EarlyRotationEventType,
            (int)EventTypeValue.LateRotationEventType);

        UpdateQueuedRotation(QueuedRotation);
        UpdateAppearance();
    }

    private void UpdateQueuedRotation(float rotation) => QueuedData.Rotation = rotation;

    public void UpdateRotation(float rotation)
    {
        QueuedRotation = rotation;
        UpdateQueuedRotation(QueuedRotation);
        UpdateAppearance();
    }

    private void UpdateAppearance()
    {
        if (PlacementVisualContainer == null)
        {
            CreateVisual();
            if (IsIdle) HideVisual();
        }

        PlacementVisualContainer!.EventData = QueuedData;
        eventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    public override void HandleApply()
    {
        var evt = QueuedData;

        base.HandleApply();

        TracksManager.RefreshTracks();

        QueuedData = new BaseRotationEvent(evt) { CustomData = null }; // need to convert back to regular event
        PlacementVisualContainer.EventData = QueuedData;
    }

    protected override void TransferQueuedToDraggedObject(ref BaseRotationEvent dragged, BaseRotationEvent queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.Type = queued.Type;
    }

    protected override void HandleDragged() => TracksManager.RefreshTracks();
}
