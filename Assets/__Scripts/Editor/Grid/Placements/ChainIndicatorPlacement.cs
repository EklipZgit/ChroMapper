using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

// This is almost the same as ArcIndicatorPlacement
public class ChainIndicatorPlacement : BasePlacement<BaseChain, ChainIndicatorContainer, ChainGridContainer>,
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
        new BeatmapObjectPlacementAction(spawned, conflicting, "Edited a chain.");

    protected override BaseChain GenerateOriginalData() => new();

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;
        var offset = new Vector3(hit.GameObject.transform.localScale.x % 2 / 2f, 0f, 0f);
        var roundedPoint = new Vector3(
            Mathf.FloorToInt(localPoint.x + offset.x),
            Mathf.FloorToInt(localPoint.y),
            placementZ);

        if (PrecisionPlacementController.IsEnabled)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedPoint.x = Mathf.Round(localPoint.x * precision) / precision;
            roundedPoint.y = Mathf.Round(localPoint.y * precision) / precision;
            PlacementVisualContainer.transform.localPosition = roundedPoint;
        }
        else
        {
            var minX = Bounds.min.x;
            var maxX = Bounds.max.x;

            var minY = Bounds.min.y;
            var maxY = Bounds.max.y;

            PlacementVisualContainer.transform.localPosition = new Vector3(
                    Mathf.Clamp(roundedPoint.x - offset.x, minX, maxX - 1),
                    Mathf.Clamp(roundedPoint.y, minY, maxY - 1),
                    roundedPoint.z)
                + (Vector3)GridOffset;
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        var pos = (Vector2)PlacementVisualContainer.transform.localPosition - GridOffset;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        var coordinate = new Vector2(pos.x - 2f, pos.y);
        if (PrecisionPlacementController.IsEnabled)
        {
            if (inputState == PlacementInputState.Hover) return;
            switch (DraggedObjectContainer.IndicatorType)
            {
                case IndicatorType.Head:
                    QueuedData.CustomCoordinate = coordinate;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = coordinate;
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
                        ? coordinate
                        : null;
                    break;
                case IndicatorType.Tail:
                    QueuedData.CustomTailCoordinate = !(Mathf.Approximately(vanillaX, pos.x)
                        && Mathf.Approximately(vanillaY, pos.y))
                        ? coordinate
                        : null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    protected override void TransferQueuedToDraggedObject(ref BaseChain dragged, BaseChain queued)
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
                dragged.CustomTailCoordinate = queued.CustomTailCoordinate;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        DraggedObjectContainer.ParentChain.AdjustTimePlacement();
        DraggedObjectContainer.ParentChain.GenerateChain(dragged);
    }

    public override void Apply() { }

    // This placement controller is only used for dragging the chain indicator
    public void OnPlaceObject(InputAction.CallbackContext context) { }

    public override float GetContainerPosZ(ObjectContainer con)
    {
        if (con is ChainIndicatorContainer indicator)
        {
            if (indicator.IndicatorType == IndicatorType.Head)
            {
                return (indicator.ParentChain.ChainData.SongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }

            if (indicator.IndicatorType == IndicatorType.Tail)
            {
                return (indicator.ParentChain.ChainData.TailSongBpmTime - Atsc.CurrentSongBpmTime)
                    * EditorScaleController.EditorScale;
            }
        }

        return base.GetContainerPosZ(con);
    }

    protected override float GetDraggedObjectJsonTime()
    {
        if (DraggedObjectContainer.IndicatorType == IndicatorType.Tail) return DraggedObjectData.TailJsonTime;

        return DraggedObjectData.JsonTime;
    }

    public void UpdateCut(int value)
    {
        if (DraggedObjectContainer != null && DraggedObjectContainer.ParentChain != null)
        {
            if (DraggedObjectContainer.IndicatorType == IndicatorType.Head)
            {
                QueuedData.CutDirection = value;
                DraggedObjectContainer.ParentChain.ChainData.CutDirection = value;
            }
        }
    }

    private void HandleKeyUpdate(InputAction.CallbackContext context, int id)
    {
        if (context.performed ^ heldKeys[id]) HandleDirectionValues();
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
        if (!previousHeldKeys.Except(heldKeys).Any()) HandleDirectionValues();
    }
}
