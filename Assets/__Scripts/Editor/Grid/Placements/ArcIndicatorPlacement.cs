using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class ArcIndicatorPlacement : BasePlacement<BaseArc, ArcIndicatorContainer, ArcGridContainer>
{
    [SerializeField] private DeleteToolController deleteToolController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private BeatmapSharedNoteInputController beatmapSharedNoteInputController;
    
    public override void Start()
    {
        base.Start();
        beatmapSharedNoteInputController.OnCutDirectionChanged += HandleOnCutDirectionChanged;
    }

    public void OnDestroy()
    {
        beatmapSharedNoteInputController.OnCutDirectionChanged -= HandleOnCutDirectionChanged;
    }
    
    private void HandleOnCutDirectionChanged(int value)
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

    public override ObjectContainer StartDrag(GameObject draggedObject)
    {
        var con = base.StartDrag(draggedObject);
        if (IsDragging)
            DraggedObjectContainer.ParentArc.Dragged = true;

        return con;
    }

    protected override List<BeatmapAction> PerformPreFinishDragActions()
    {
        DraggedObjectContainer.ParentArc.Dragged = false;
        
        return new List<BeatmapAction>();
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Edited an arc.");

    protected override BaseArc GenerateOriginalData() => new();

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
                if (dragged.Rotation != queued.Rotation)
                {
                    dragged.Rotation = queued.Rotation;
                    TracksManager.RefreshTracks();
                }

                break;
            case IndicatorType.Tail:
                dragged.TailJsonTime = queued.JsonTime;
                dragged.TailPosX = queued.PosX;
                dragged.TailPosY = queued.PosY;
                dragged.TailCutDirection = queued.TailCutDirection;
                dragged.CustomTailCoordinate = queued.CustomTailCoordinate;
                if (dragged.TailRotation != queued.Rotation)
                {
                    dragged.TailRotation = queued.Rotation;
                    TracksManager.RefreshTracks();
                }

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
}
