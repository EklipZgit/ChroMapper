using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class ObstaclePlacement : BasePlacement<BaseObstacle, ObstacleContainer, ObstacleGridContainer>
{
    // Chroma Color Stuff
    public static readonly string ChromaColorKey = "PlaceChromaObjects";

    [FormerlySerializedAs("obstacleAppearanceSO")] [SerializeField]
    private ObstacleAppearanceSO obstacleAppearanceSo;

    [SerializeField] private ColorPicker colorPicker;
    [SerializeField] private ToggleColourDropdown dropdown;

    private int originIndex;
    private Vector2 originPosition;

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

    protected override void UpdatePlacement(
        Vector3 rawHit,
        Vector3 roundedHit,
        PlacementState state)
    {
        PlacementVisualContainer.ObstacleData = QueuedData;
        PlacementVisualContainer.ObstacleData.Duration = RoundedJsonTime - startJsonTime;
        obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
        var songBpmDuration = (float)BeatSaberSongContainer.Instance.Map.JsonTimeToSongBpmTime(RoundedJsonTime)
            - startSongBpmTime;

        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var wallTransform = PlacementVisualContainer.Animator.LocalTarget;

        if (IsPlacing)
        {
            if (PrecisionPlacementController.IsEnabled)
            {
                var precision = Settings.Instance.PrecisionPlacementGridPrecision;
                var precisionR = 1.0f / Settings.Instance.PrecisionPlacementGridPrecision;
                rawHit.x = (Mathf.Floor(rawHit.x * precision) * precisionR) + 0.5f;
                rawHit.y = (Mathf.Floor(rawHit.y * precision) * precisionR) + 1f;
                rawHit.z = songBpmDuration * EditorScaleController.EditorScale;

                var position = (Vector3)originPosition;
                var newLocalScale = rawHit - position + new Vector3(precisionR, precisionR, 0);
                if (newLocalScale.x <= 0)
                {
                    position.x = originPosition.x + newLocalScale.x - precisionR;
                    QueuedData.CustomCoordinate[0] = position.x;
                    newLocalScale.x = (2 * precisionR) - newLocalScale.x;
                }

                if (newLocalScale.y <= 0)
                {
                    position.y = originPosition.y + newLocalScale.y - precisionR;
                    QueuedData.CustomCoordinate[1] = position.y;
                    newLocalScale.y = (2 * precisionR) - newLocalScale.y;
                }

                var localPosition = new Vector3(
                    position.x + (newLocalScale.x * 0.5f),
                    position.y,
                    0);
                wallTransform.localPosition = localPosition;
                PlacementVisualContainer.transform.localPosition =
                    new Vector3(0, -0.5f, startSongBpmTime * EditorScaleController.EditorScale);

                PlacementVisualContainer.SetScale(newLocalScale);

                if (QueuedData.CustomSize == null) QueuedData.CustomSize = new JSONArray();

                QueuedData.CustomSize[0] = newLocalScale.x;
                QueuedData.CustomSize[1] = newLocalScale.y;
            }
            else
            {
                QueuedData.CustomCoordinate = null;
                QueuedData.CustomSize = null;

                // Ensure wall has positive width no matter right to left or left to right placement
                QueuedData.Width = Mathf.CeilToInt(rawHit.x + 2.5f) - originIndex;
                if (QueuedData.Width <= 0)
                {
                    QueuedData.PosX = originIndex + QueuedData.Width - 1;
                    QueuedData.Width = 2 - QueuedData.Width;
                }
                else
                    QueuedData.PosX = originIndex;

                wallTransform.localPosition = new Vector3(
                    QueuedData.PosX - 2f + (QueuedData.Width / 2f),
                    QueuedData.Type == (int)ObstacleType.Full ? 0 : 2,
                    0);
                PlacementVisualContainer.transform.localPosition =
                    new Vector3(0, -.5f, startSongBpmTime * EditorScaleController.EditorScale);

                PlacementVisualContainer.SetScale(
                    new Vector3(
                        QueuedData.Width,
                        QueuedData.Height,
                        songBpmDuration * EditorScaleController.EditorScale));
            }

            return;
        }

        startJsonTime = RoundedJsonTime;
        PlacementVisualContainer.ObstacleData.Duration = SmallestRankableWallDuration;

        if (PrecisionPlacementController.IsEnabled)
        {
            var precision = (float)Settings.Instance.PrecisionPlacementGridPrecision;
            rawHit.x = (Mathf.Floor(rawHit.x * precision) / precision) + 0.5f;
            rawHit.y = (Mathf.Floor(rawHit.y * precision) / precision) + 1f;
            rawHit.z = 0;
            var size = Vector3.one / precision;

            wallTransform.localPosition = rawHit + new Vector3(size.x * 0.5f, 0, 0);

            PlacementVisualContainer.SetScale(size);
            QueuedData.PosX = QueuedData.Type = 0;

            if (QueuedData.CustomData == null) QueuedData.CustomData = new JSONObject();
            QueuedData.CustomCoordinate = (Vector2)rawHit;
        }
        else
        {
            QueuedData.CustomCoordinate = null;
            QueuedData.CustomSize = null;

            var vanillaType = rawHit.y + 1 <= 2 ? (int)ObstacleType.Full : (int)ObstacleType.Crouch;

            QueuedData.PosX = Mathf.RoundToInt(roundedHit.x) + 2;
            QueuedData.PosY = vanillaType == (int)ObstacleType.Full ? 0 : 2;
            QueuedData.Height = vanillaType == (int)ObstacleType.Full ? 5 : 3;
            QueuedData.Type = vanillaType;

            wallTransform.localPosition = new Vector3(
                roundedHit.x + .5f,
                QueuedData.PosY,
                0);
            PlacementVisualContainer.transform.localPosition = new Vector3(0, -.5f, roundedHit.z);

            PlacementVisualContainer.SetScale(
                new Vector3(1, vanillaType == (int)ObstacleType.Full ? 5f : 3f, Mathf.Epsilon));
        }
    }

    public void OnMousePositionUpdate(InputAction.CallbackContext context)
    {
        if (!IsPlacing) return;
        var scale = PlacementVisualContainer.GetScale();
        PlacementVisualContainer.SetScale(
            new Vector3(
                scale.x,
                scale.y,
                (SongBpmTime - startSongBpmTime) * EditorScaleController.EditorScale));
    }

    public override void HandleApply()
    {
        if (IsPlacing)
        {
            IsPlacing = false;
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
            obstacleAppearanceSo.SetObstacleAppearance(PlacementVisualContainer);
            PlacementVisualContainer.SetScale(
                new Vector3(
                    1,
                    PlacementVisualContainer.ObstacleData.Type == (int)ObstacleType.Full ? 5f : 3f,
                    Mathf.Epsilon));
        }
        else
        {
            IsPlacing = true;
            originIndex = QueuedData.PosX;
            originPosition = QueuedData.CustomCoordinate?.ReadVector2()
                ?? new Vector2(originIndex, 5 - QueuedData.Height);
            startJsonTime = RoundedJsonTime;
            startSongBpmTime = SongBpmTime;
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
