using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcIndicatorPlacement : BasePlacement<BaseArc, ArcIndicatorContainer, ArcGridContainer>,
                                     CMInput.INotePlacementActions
{
    private const int upKey = 0;
    private const int leftKey = 1;
    private const int downKey = 2;
    private const int rightKey = 3;

    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private LaserSpeedController laserSpeedController;

    // Below is copied from NotePlacement. Would be nice to have some kind of shared placement.
    private readonly float diagonalStickMaxTime = 0.3f;
    private readonly List<bool> heldKeys = new() { false, false, false, false };
    private bool diagonal;
    private bool flagDirectionsUpdate;
    private static HashSet<BaseObject> SelectedObjects => SelectionController.SelectedObjects;

    private void LateUpdate()
    {
        if (flagDirectionsUpdate)
        {
            HandleDirectionValues();
            flagDirectionsUpdate = false;
        }
    }


    //TODO perhaps make a helper function to deal with the context.performed and context.canceled checks
    public void OnDownNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, downKey);

    public void OnLeftNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, leftKey);

    public void OnUpNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, upKey);

    public void OnRightNote(InputAction.CallbackContext context) => HandleKeyUpdate(context, rightKey);

    public void OnDotNote(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        deleteToolController.UpdateDeletion(false);
        UpdateCut((int)NoteCutDirection.Any);
    }

    public void OnUpLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.UpLeft);
    }

    public void OnUpRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.UpRight);
    }

    public void OnDownRightNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.DownRight);
    }

    public void OnDownLeftNote(InputAction.CallbackContext context)
    {
        if (context.performed && !laserSpeedController.Activated) UpdateCut((int)NoteCutDirection.DownLeft);
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, "Edited an arc.");

    protected override BaseArc GenerateOriginalData() => new();

    protected override void UpdateData(PlacementInputState inputState)
    {
        var pos = (Vector2)PlacementVisualContainer.transform.localPosition - GridOffset;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        var roundedHit = new Vector2(pos.x - 2f, pos.y);
        if (PrecisionPlacementController.IsEnabled)
        {
            if (inputState == PlacementInputState.Hover) return;
            switch (DraggedObjectContainer.IndicatorType)
            {
                case IndicatorType.Head:
                    QueuedData.CustomCoordinate = roundedHit;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = roundedHit;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        else
        {
            if (inputState == PlacementInputState.Hover) return;
            switch (DraggedObjectContainer.IndicatorType)
            {
                case IndicatorType.Head:
                    QueuedData.CustomCoordinate = !(Mathf.Approximately(vanillaX, pos.x)
                        && Mathf.Approximately(vanillaY, pos.y))
                        ? roundedHit
                        : null;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = !(Mathf.Approximately(vanillaX, pos.x)
                        && Mathf.Approximately(vanillaY, pos.y))
                        ? roundedHit
                        : null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override void TransferQueuedToDraggedObject(ref BaseArc dragged, BaseArc queued)
    {
        switch (DraggedObjectContainer.IndicatorType)
        {
            case IndicatorType.Head:
                dragged.JsonTime = queued.JsonTime;
                dragged.PosX = queued.PosX;
                dragged.PosY = queued.PosY;
                dragged.CutDirection = queued.CutDirection;
                dragged.CustomCoordinate = queued.CustomCoordinate;
                break;
            case IndicatorType.Tail:
                dragged.TailJsonTime = queued.JsonTime;
                dragged.TailPosX = queued.PosX;
                dragged.TailPosY = queued.PosY;
                dragged.TailCutDirection = queued.TailCutDirection;
                dragged.CustomTailCoordinate = queued.CustomTailCoordinate;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        DraggedObjectContainer.ParentArc.NotifySplineChanged(dragged);
    }

    public void OnPlaceObject(InputAction.CallbackContext context)
    {
        // This placement controller is only used for dragging the arc indicator
    }

    public override float GetContainerPosZ(ObjectContainer con)
    {
        if (con is ArcIndicatorContainer indicator)
        {
            if (indicator.IndicatorType == IndicatorType.Head)
            {
                return (indicator.ParentArc.ArcData.SongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }

            if (indicator.IndicatorType == IndicatorType.Tail)
            {
                return (indicator.ParentArc.ArcData.TailSongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }
        }

        return base.GetContainerPosZ(con);
    }

    protected override float GetDraggedObjectJsonTime() =>
        DraggedObjectContainer.IndicatorType == IndicatorType.Tail
            ? DraggedObjectData.TailJsonTime
            : DraggedObjectData.JsonTime;

    public void UpdateCut(int value)
    {
        if (DraggedObjectContainer == null || DraggedObjectContainer.ParentArc == null) return;
        switch (DraggedObjectContainer.IndicatorType)
        {
            case IndicatorType.Head:
                QueuedData.CutDirection = value;
                DraggedObjectContainer.ParentArc.ArcData.CutDirection = value;
                break;
            case IndicatorType.Tail:
                QueuedData.TailCutDirection = value;
                DraggedObjectContainer.ParentArc.ArcData.TailCutDirection = value;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) flagDirectionsUpdate = true;
        heldKeys[id] = context.performed;
    }

    private void HandleDirectionValues()
    {
        deleteToolController.UpdateDeletion(false);

        var upNote = heldKeys[upKey];
        var downNote = heldKeys[downKey];
        var leftNote = heldKeys[leftKey];
        var rightNote = heldKeys[rightKey];
        var previousDiagonalState = diagonal;

        var handleUpDownNotes = upNote ^ downNote; // XOR: True if the values are different, false if the same
        var handleLeftRightNotes = leftNote ^ rightNote;

        diagonal = handleUpDownNotes && handleLeftRightNotes;

        if (previousDiagonalState && !diagonal)
        {
            StartCoroutine(CheckForDiagonalUpdate());
            return;
        }

        if (handleUpDownNotes && !handleLeftRightNotes) // We handle simple up/down notes
        {
            if (upNote)
                UpdateCut((int)NoteCutDirection.Up);
            else
                UpdateCut((int)NoteCutDirection.Down);
        }
        else if (!handleUpDownNotes && handleLeftRightNotes) // We handle simple left/right notes
        {
            if (leftNote)
                UpdateCut((int)NoteCutDirection.Left);
            else
                UpdateCut((int)NoteCutDirection.Right);
        }
        else if (diagonal) //We need to do a diagonal
        {
            if (leftNote)
            {
                if (upNote)
                    UpdateCut((int)NoteCutDirection.UpLeft);
                else
                    UpdateCut((int)NoteCutDirection.DownLeft);
            }
            else
            {
                if (upNote)
                    UpdateCut((int)NoteCutDirection.UpRight);
                else
                    UpdateCut((int)NoteCutDirection.DownRight);
            }
        }
    }

    private IEnumerator CheckForDiagonalUpdate()
    {
        var previousHeldKeys = new List<bool>(heldKeys);
        yield return new WaitForSeconds(diagonalStickMaxTime);
        // Weird way of saying "Are the keys being held right now the same as before"
        if (!previousHeldKeys.Except(heldKeys).Any()) flagDirectionsUpdate = true;
    }
}
