using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class RotationEventPlacement :
    BasePlacement<BaseRotationEvent, RotationEventContainer, RotationEventGridContainer>,
    CMInput.IRotationEventPlacementActions
{
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;

    private bool earlyRotationPlaceNow;
    private bool negativeRotations;
    public float QueuedRotation = 30f;

    public void OnRotation15Degrees(InputAction.CallbackContext context)
    {
        if (context.performed) UpdateRotation(negativeRotations ? -15f : 15f);
    }

    public void OnRotation30Degrees(InputAction.CallbackContext context)
    {
        if (context.performed) UpdateRotation(negativeRotations ? -30f : 30f);
    }

    public void OnRotation45Degrees(InputAction.CallbackContext context)
    {
        if (context.performed) UpdateRotation(negativeRotations ? -45f : 45f);
    }

    public void OnRotation60Degrees(InputAction.CallbackContext context)
    {
        if (context.performed) UpdateRotation(negativeRotations ? -60f : 60f);
    }

    public void OnNegativeRotationModifier(InputAction.CallbackContext context) =>
        negativeRotations = context.performed;

    public void OnRotateInPlaceLeft(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(false, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceRight(InputAction.CallbackContext context)
    {
        if (context.performed) PlaceRotationNow(true, earlyRotationPlaceNow);
    }

    public void OnRotateInPlaceModifier(InputAction.CallbackContext context) =>
        earlyRotationPlaceNow = context.performed;

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
            (int)EventTypeValue.EarlyLaneRotation
            + Mathf.FloorToInt(PlacementVisualContainer.transform.localPosition.x),
            (int)EventTypeValue.EarlyLaneRotation,
            (int)EventTypeValue.LateLaneRotation);

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

    private void PlaceRotationNow(bool right, bool early)
    {
        var rotationType = early ? (int)EventTypeValue.EarlyLaneRotation : (int)EventTypeValue.LateLaneRotation;
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var evt = ObjectContainerCollection.MapObjects.Find(x =>
            x.JsonTime - epsilon < Atsc.CurrentJsonTime
            && x.JsonTime + epsilon > Atsc.CurrentJsonTime
            && x.Type == rotationType);

        //todo add support for custom rotation angles

        var startingValue = right ? 4 : 3;
        if (evt != null) startingValue = evt.Value;

        if (evt != null
            && ((startingValue == 4 && !right)
                || (startingValue == 3
                    && right))) //This is for when we're going from a rotation event to no rotation event
        {
            startingValue = evt.Value;
            ObjectContainerCollection.DeleteObject(evt, false);
            BeatmapActionContainer.AddAction(new BeatmapObjectDeletionAction(evt, "Deleted by PlaceRotationNow."));
        }
        else if ((startingValue < 7 && right) || (startingValue > 0 && !right))
        {
            if (evt != null) startingValue += right ? 1 : -1;
            var objectData = new BaseEvent
            {
                JsonTime = Atsc.CurrentJsonTime, Type = rotationType, Value = startingValue
            };

            ObjectContainerCollection.SpawnObject(objectData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(objectData, conflicting));
        }

        QueuedData = BeatmapFactory.Clone(QueuedData);
        TracksManager.RefreshTracks();
    }

    protected override void HandleDragged() => TracksManager.RefreshTracks();
}
