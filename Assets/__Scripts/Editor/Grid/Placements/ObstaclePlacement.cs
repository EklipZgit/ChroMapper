using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObstaclePlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";
    [SerializeField] private ObstacleAppearanceSO obstacleAppearanceSo;
    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    private int originIndex;
    private Vector3 originPos;

    private float startJsonTime;
    private float startSongBpmTime;

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

    public static bool IsPlacing { get; private set; }

    private float SmallestRankableWallDuration => Atsc.GetBeatFromSeconds(0.016f);

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Place a Wall.");

    protected override BaseObstacle GenerateOriginalData() => new();

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.ObstacleData = QueuedData;
    }

    // Wall transform anchor on bottom middle
    protected override void UpdatePlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var roundedPoint = localPoint;
        var size = 1.0f;
        if (PrecisionPlacementController.IsEnabled)
        {
            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            size = 1.0f / precision;
            roundedPoint.x = (Mathf.Floor(roundedPoint.x * precision) / precision) + (size / 2f);
            roundedPoint.y = Mathf.Floor(roundedPoint.y * precision) / precision;
        }
        else
        {
            roundedPoint.x =
                Mathf.Clamp(Mathf.Floor(roundedPoint.x), Bounds.min.x, Bounds.max.x - 1f) + (size / 2f);
            roundedPoint.y =
                IsPlacing
                    ? Mathf.Clamp(Mathf.Floor(roundedPoint.y + .5f), Bounds.min.y, Bounds.max.y + 1f) - .5f
                    : Mathf.Clamp(Mathf.Floor(roundedPoint.y + .5f), Bounds.min.y, Bounds.max.y - 1f) - .5f;
        }

        if (!IsPlacing)
        {
            PlacementVisualContainer.transform.localPosition = roundedPoint;
            PlacementVisualContainer.SetScale(new Vector3(size, size, Mathf.Epsilon));
        }
        else
        {
            var originShove = originPos;
            var sizeX = size;
            var sizeY = size;

            // there's probably elegant way to do this,
            // i just cant think now
            if (roundedPoint.x < originPos.x)
            {
                var difference = Mathf.Abs(roundedPoint.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (roundedPoint.y < originPos.y)
            {
                var difference = Mathf.Abs(roundedPoint.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            var newLocalScale = roundedPoint + new Vector3(sizeX, sizeY, 0f) - originShove;
            PlacementVisualContainer.transform.localPosition =
                originShove + new Vector3((newLocalScale.x - size) / 2f, 0f, 0f);
            PlacementVisualContainer.SetScale(newLocalScale);
        }
    }

    protected override void UpdateData(PlacementState state)
    {
        if (!IsPlacing)
        {
            startJsonTime = RoundedJsonTime;
            PlacementVisualContainer.ObstacleData.Duration = SmallestRankableWallDuration;

            return;
        }

        QueuedData.Duration = RoundedJsonTime - startJsonTime;
        if (Mathf.Abs(RoundedJsonTime - startJsonTime) < SmallestRankableWallDuration)
            QueuedData.Duration = SmallestRankableWallDuration;
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);

        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var pos = (Vector2)PlacementVisualContainer.transform.localPosition;
        var scale = (Vector2)PlacementVisualContainer.ObstacleScale;

        // let's not talk about this
        QueuedData.Type = pos.y <= 2 ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;

        var vanillaPos = new Vector2(Mathf.FloorToInt(pos.x - (scale.x / 2f)), Mathf.FloorToInt(pos.y));
        var coordinates = pos - new Vector2(scale.x / 2f, .5f);
        QueuedData.CustomCoordinate = vanillaPos != coordinates ? coordinates + Vector2.up : null;
        QueuedData.PosX = (int)vanillaPos.x + 2;
        QueuedData.PosY = (int)vanillaPos.y + 1;

        var vanillaSize = new Vector2(Mathf.CeilToInt(scale.x), Mathf.CeilToInt(scale.y));
        QueuedData.CustomSize = vanillaSize != scale ? scale : null;
        QueuedData.Width = (int)vanillaSize.x;
        QueuedData.Height = (int)vanillaSize.y;
    }

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
            IsPlacing = false;
        }
        else
        {
            originPos = PlacementVisualContainer.transform.localPosition;
            startJsonTime = RoundedJsonTime;
            startSongBpmTime = SongBpmTime;
            IsPlacing = true;
        }
    }

    protected override void TransferQueuedToDraggedObject(ref BaseObstacle dragged, BaseObstacle queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }

    public override void Cancel()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
            QueuedData = GenerateOriginalData();
            PlacementVisualContainer.ObstacleData = QueuedData;
            // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
            PlacementVisualContainer.SetScale(
                new Vector3(
                    1,
                    PlacementVisualContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f,
                    Mathf.Epsilon));
        }
    }
}
