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

    // this wouldve taken easily from select box but no it decided it wants to be special
    protected override void UpdatePlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var songBpmDuration = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(RoundedJsonTime)
            - startSongBpmTime;

        var roundedPoint = new Vector3(Mathf.Floor(localPoint.x), Mathf.Floor(localPoint.y), localPoint.z);
        if (PrecisionPlacementController.IsEnabled)
        {
            var precision = 1.0f / Settings.Instance.PrecisionPlacementGridPrecision;
            // localPoint.x
        }
        else
        {
            roundedPoint.x = Mathf.Clamp(roundedPoint.x, Bounds.min.x, Bounds.max.x - 1);
            roundedPoint.y = Mathf.Clamp(roundedPoint.y, Bounds.min.y, Bounds.max.y - 1);
        }

        Debug.Log($"{localPoint} {roundedPoint} {PlacementVisualContainer.transform.localPosition}");

        if (!IsPlacing)
        {
            if (PrecisionPlacementController.IsEnabled)
            {
            }
            else
            {
                var vanillaType = localPoint.y < 1.5 ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;
                var posY = vanillaType == (int)ObstacleType.Full ? 0 : 2;
                var height = vanillaType == (int)ObstacleType.Full ? 5 : 3;

                PlacementVisualContainer.transform.localPosition = new Vector3(
                    roundedPoint.x + .5f,
                    posY - .5f,
                    roundedPoint.z);
                PlacementVisualContainer.SetScale(
                    new Vector3(1f, height, Mathf.Epsilon));
            }
        }
        else
        {
            var originShove = originPos;
            float sizeX = 1;
            float sizeY = 1;

            // there's probably elegant way to do this,
            // i just cant think now
            if (localPoint.x < originPos.x)
            {
                var difference = Mathf.Abs(localPoint.x - originPos.x);
                sizeX += difference;
                originShove.x -= difference;
            }

            if (localPoint.y < originPos.y)
            {
                var difference = Mathf.Abs(localPoint.y - originPos.y);
                sizeY += difference;
                originShove.y -= difference;
            }

            PlacementVisualContainer.transform.localPosition = originShove;
            var newLocalScale = localPoint + new Vector3(sizeX, sizeY, 0.5f) - originShove;
            // PlacementVisualContainer.transform.localPosition = new Vector3(
            //     roundedPoint.x + .5f,
            //     posY - .5f,
            //     roundedPoint.z);
            PlacementVisualContainer.SetScale(newLocalScale);
        }
    }

    protected override void UpdateData(PlacementState state)
    {
        if (!IsPlacing)
        {
            startJsonTime = RoundedJsonTime;
            PlacementVisualContainer.ObstacleData.Duration = SmallestRankableWallDuration;


            if (PrecisionPlacementController.IsEnabled)
            {
                // if (newLocalScale.x <= 0)
                // {
                //     position.x = originPosition.x + newLocalScale.x - precisionR;
                //     QueuedData.CustomCoordinate[0] = position.x;
                // }
                //
                // if (newLocalScale.y <= 0)
                // {
                //     position.y = originPosition.y + newLocalScale.y - precisionR;
                //     QueuedData.CustomCoordinate[1] = position.y;
                // }

                if (QueuedData.CustomSize == null) QueuedData.CustomSize = new JSONArray();
            }
            else
            {
                QueuedData.CustomCoordinate = null;
                QueuedData.CustomSize = null;

                // QueuedData.Width = Mathf.CeilToInt(localPoint.x + 2.5f) - originIndex;
                if (QueuedData.Width <= 0)
                {
                    QueuedData.PosX = originIndex + QueuedData.Width - 1;
                    QueuedData.Width = 2 - QueuedData.Width;
                }
                else
                    QueuedData.PosX = originIndex;
            }

            return;
        }

        PlacementVisualContainer.ObstacleData.Duration = RoundedJsonTime - startJsonTime;
        // obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);

        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        if (PrecisionPlacementController.IsEnabled)
        {
            QueuedData.PosX = QueuedData.Type = 0;
            if (QueuedData.CustomData == null) QueuedData.CustomData = new JSONObject();
            // QueuedData.CustomCoordinate = (Vector2)localPoint;
        }
        else
        {
            QueuedData.CustomCoordinate = null;
            QueuedData.CustomSize = null;

            // var vanillaType = localPoint.y + 1 <= 2 ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;

            // QueuedData.PosX = Mathf.RoundToInt(roundedPoint.x) + 2;
            // QueuedData.PosY = vanillaType == (int)ObstacleType.Full ? 0 : 2;
            // QueuedData.Height = vanillaType == (int)ObstacleType.Full ? 5 : 3;
            // QueuedData.Type = vanillaType;
        }
    }

    public override void HandleApply()
    {
        if (IsPlacing)
        {
            QueuedData.JsonTime = startJsonTime;

            var endSongBpmTime =
                startSongBpmTime + (PlacementVisualContainer.GetScale().z / EditorScaleController.EditorScale);

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
            obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
            PlacementVisualContainer.SetScale(
                new Vector3(
                    1,
                    PlacementVisualContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f,
                    Mathf.Epsilon));
        }
    }
}
