using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class ObstaclePlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private GridViewController gridViewController;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;
    private bool hasExpanded;
    private bool hasOffset;
    private bool hasPreviousSnappedState;

    private int originIndex;
    private Vector3 originPos;
    private Vector2 previousSnappedState;
    private Vector3 scale;

    private float startJsonTime;
    private float startSongBpmTime;

    private bool v2Mode;

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

    private float SmallestRankableWallDuration => Atsc.GetBeatFromSeconds(0.016f);

    public void Awake() => LoadInitialMap.OnLevelLoaded += HandleLevelLoaded;

    public override void OnDestroy()
    {
        base.OnDestroy();
        LoadInitialMap.OnLevelLoaded -= HandleLevelLoaded;
    }

    protected override void ResetHysteresis()
    {
        base.ResetHysteresis();
        hasPreviousSnappedState = false;
        previousSnappedState = Vector2.zero;
    }

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicts) =>
        new BeatmapObjectPlacementAction(spawned, conflicts, "Place a Wall.");

    protected override BaseObstacle GenerateOriginalData() => new();
    private void HandleLevelLoaded() => v2Mode = BeatSaberSongContainer.Instance.Map.MajorVersion == 2;

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.ObstacleData = QueuedData;
        obstacleAppearance.SetObstacleAppearance(PlacementVisualContainer, true);
    }

    public override void UpdateState(Intersections.IntersectionHit hit, PlacementInputState inputState)
    {
        if (IsPlacing && !AllowPlacement) Cancel();
        base.UpdateState(hit, inputState);
    }

    // Wall transform anchor on bottom middle
    protected override void HandleHitToPlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var size = 1f;
        if (PrecisionPlacementController.IsEnabled)
        {
            ResetHysteresis();
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            size = 1f / precision;
            LanePosition = BeatmapPositionHelper.LocalPositionToLanePosition(
                localPoint,
                precision,
                BeatmapConstant.ObstacleYOffset);
        }
        else
        {
            var rawX = (localPoint.x / BeatmapConstant.LaneSize) - (gridViewController.IsOdd ? 0.5f : 0f);
            var rawY = (localPoint.y - BeatmapConstant.YOffset - BeatmapConstant.ObstacleYOffset)
                / BeatmapConstant.LaneSize;
            var raw = new Vector2(rawX, rawY);
            if (!hasPreviousSnappedState)
            {
                previousSnappedState = new Vector2(Mathf.Floor(raw.x), Mathf.Floor(raw.y));
                hasPreviousSnappedState = true;
            }
            else
                previousSnappedState = BeatmapPositionHelper.SnapWithHysteresis(raw, previousSnappedState);

            LanePosition = new Vector3(previousSnappedState.x, previousSnappedState.y, 0f);
        }

        LanePosition.x += (size / 2f) + (gridViewController.IsOdd ? 0.5f : 0f);
        var zPlacement = BeatmapPositionHelper.SongTimeToLanePositionZ(SongBpmTime);
        LanePosition.z = zPlacement;

        if (!IsPlacing)
            scale = new Vector3(size, size, Mathf.Epsilon);
        else
        {
            var originShove = originPos;
            var sizeX = size;
            var sizeY = size;

            // there's probably elegant way to do this,
            // i just cant think now
            if (LanePosition.x < originPos.x)
            {
                var difference = Mathf.Abs(LanePosition.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (LanePosition.y < originPos.y)
            {
                var difference = Mathf.Abs(LanePosition.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            scale = LanePosition + new Vector3(sizeX, sizeY, 0f) - originShove;
            LanePosition = originShove + new Vector3((scale.x - size) / 2f, 0f, 0f);
        }

        if (v2Mode && !PrecisionPlacementController.IsEnabled)
        {
            if (LanePosition.y < 1.5)
            {
                LanePosition.y = 0;
                scale.y = 5f;
            }
            else
            {
                LanePosition.y = 2;
                scale.y = 3f;
            }
        }

        PlacementVisualContainer.transform.localPosition =
            BeatmapPositionHelper.LanePositionToLocalPosition(LanePosition, BeatmapConstant.ObstacleYOffset);
        if (scale != PlacementVisualContainer.ObstacleScale / BeatmapConstant.LaneSize)
            PlacementVisualContainer.SetScale(scale * BeatmapConstant.LaneSize);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        if (!IsPlacing)
        {
            startJsonTime = RoundedJsonTime;
            PlacementVisualContainer.ObstacleData.Duration = SmallestRankableWallDuration;
        }
        else
        {
            QueuedData.Duration = RoundedJsonTime - startJsonTime;
            if (Mathf.Abs(RoundedJsonTime - startJsonTime) < SmallestRankableWallDuration)
                QueuedData.Duration = SmallestRankableWallDuration;
        }
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);

        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var pos = LanePosition;
        pos.y -= 0.5f;

        // let's not talk about this
        QueuedData.Type = pos.y < 2
            ? (int)ObstacleType.Full
            : (int)ObstacleType.Crouch;

        var vanillaPos = new Vector2(Mathf.FloorToInt(pos.x - (scale.x / 2f)), Mathf.FloorToInt(pos.y));
        var coordinates = (Vector2)pos - new Vector2(scale.x / 2f, .5f);
        QueuedData.CustomCoordinate = vanillaPos != coordinates ? coordinates + Vector2.up : null;
        QueuedData.PosX = (int)vanillaPos.x + 2;
        QueuedData.PosY = (int)vanillaPos.y + 1;

        var vanillaSize = new Vector2(Mathf.CeilToInt(scale.x), Mathf.CeilToInt(scale.y));
        QueuedData.CustomSize = vanillaSize != (Vector2)scale ? (Vector2)scale : null;
        QueuedData.Width = (int)vanillaSize.x;
        QueuedData.Height = (int)vanillaSize.y;
    }

    protected override void HandleRotationChanged(float rotation) => QueuedData.Rotation = (int)rotation;

    public override void HandleApply()
    {
        if (IsPlacing)
        {
            QueuedData.JsonTime = startJsonTime;

            var endSongBpmTime =
                startSongBpmTime + (PlacementVisualContainer.ObstacleScale.z / EditorScaleController.EditorScale);

            if (endSongBpmTime - startSongBpmTime < SmallestRankableWallDuration)
            {
                endSongBpmTime = startSongBpmTime + SmallestRankableWallDuration;
                var endJsonTime = (float)BeatSaberSongContainer.Instance.Map.SongBpmTimeToJsonTime(endSongBpmTime);
                QueuedData.Duration = endJsonTime - startJsonTime;
            }

            ObjectContainerCollection.SpawnObject(QueuedData, out var conflicting);
            BeatmapActionContainer.AddAction(GenerateAction(QueuedData, conflicting));
            QueuedData = BeatmapFactory.Clone(QueuedData);
            PlacementVisualContainer.ObstacleData = QueuedData;
            // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
            State = PlacementState.Idle;
        }
        else
        {
            originPos = LanePosition;
            startJsonTime = RoundedJsonTime;
            startSongBpmTime = SongBpmTime;
            State = PlacementState.Placing;
        }
    }

    protected override void TransferQueuedToDraggedObject(ref BaseObstacle dragged, BaseObstacle queued)
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

    public override void Cancel()
    {
        base.Cancel();
        if (!IsPlacing) return;
        State = PlacementState.Idle;
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
        PlacementVisualContainer.SetScale(
            new Vector3(
                1,
                PlacementVisualContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f,
                Mathf.Epsilon));
    }
}
