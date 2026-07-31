using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class BombPlacement : BasePlacement<BaseNote, NoteContainer, NoteGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";

    private static readonly int alwaysTranslucent = Shader.PropertyToID("_AlwaysTranslucent");
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private ToggleColourDropdown dropdown;
    private bool hasPreviousSnappedState;
    private Vector2 previousSnappedState;

    // Chroma Color Check
    public static bool CanPlaceChromaObjects
    {
        get
        {
            if (Settings.NonPersistentSettings.ContainsKey(ChromaColorKey))
                return (bool)Settings.NonPersistentSettings[ChromaColorKey];
            return false;
        }
    }

    protected override void ResetHysteresis()
    {
        base.ResetHysteresis();
        hasPreviousSnappedState = false;
        previousSnappedState = Vector2.zero;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Placed a Bomb.");

    protected override BaseNote GenerateOriginalData() => new() { Type = (int)NoteType.Bomb };

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.ModelController.MpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.ArrowMpbController.Mpb.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.UpdateMaterials();
        PlacementVisualContainer.NoteData = QueuedData;
    }

    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);

        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePositionRound(
                localPoint,
                precision,
                BeatmapConstant.PlayerYOffset / 2f);
            LanePosition.z = zPlacement;
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.PlayerYOffset / 2f);
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - (BeatmapConstant.PlayerYOffset / 2f))
                / BeatmapConstant.LaneSize;
            var raw = new Vector2(rawX, rawY);
            if (!hasPreviousSnappedState)
            {
                previousSnappedState = new Vector2(Mathf.Floor(raw.x), Mathf.Floor(raw.y));
                hasPreviousSnappedState = true;
            }
            else
                previousSnappedState = BeatmapPositionHelper.SnapWithHysteresis(raw, previousSnappedState);

            LanePosition = new Vector3(
                previousSnappedState.x + (gridViewController.IsOdd ? 0.5f : 0f),
                previousSnappedState.y,
                zPlacement);
            PlacementVisualContainer.transform.localPosition =
                BeatmapPositionHelper.LanePositionToLocalPosition(
                    LanePosition,
                    Bounds,
                    BeatmapConstant.PlayerYOffset / 2f);
        }
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.x += 2f;

        var vanillaX = Mathf.FloorToInt(Mathf.Clamp(pos.x, 0f, 3f));
        var vanillaY = Mathf.FloorToInt(Mathf.Clamp(pos.y, 0f, 2f));

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        if (PrecisionPlacementController.IsEnabled)
            QueuedData.CustomCoordinate = new Vector2(pos.x - 2f, pos.y);
        else
        {
            QueuedData.CustomCoordinate =
                !(Mathf.Approximately(vanillaX, pos.x)
                    && Mathf.Approximately(vanillaY, pos.y))
                    ? new Vector2(pos.x - 2f, pos.y)
                    : null;
        }
    }

    protected override void HandleRotationChanged(float rotation) => QueuedData.Rotation = (int)rotation;

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
        if (dragged.Rotation != queued.Rotation)
        {
            dragged.Rotation = queued.Rotation;
            TracksManager.RefreshTracks();
        }
    }

    public override void FinishDrag()
    {
        base.FinishDrag();
        QueuedData.Rotation = (int)LaneRotationProvider.EditRotation;
    }
}
