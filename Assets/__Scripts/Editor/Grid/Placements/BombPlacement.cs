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
    [SerializeField] private ColorPicker colorPicker;

    [SerializeField] private ToggleColourDropdown dropdown;

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

    protected override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> container) =>
        new BeatmapObjectPlacementAction(spawned, container, "Placed a Bomb.");

    protected override BaseNote GenerateOriginalData() => new() { Type = (int)NoteType.Bomb };

    public override void Initialize(PlacementProvider provider)
    {
        base.Initialize(provider);
        PlacementVisualContainer.MaterialPropertyBlock.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.UpdateMaterials();
        PlacementVisualContainer.NoteData = QueuedData;
    }

    protected override void UpdatePlacement(Intersections.IntersectionHit hit, Vector3 localPoint)
    {
        var placementZ = SongBpmTime * EditorScaleController.EditorScale;
        var roundedPoint = new Vector3(Mathf.FloorToInt(localPoint.x), Mathf.FloorToInt(localPoint.y), placementZ);

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
                    Mathf.Clamp(roundedPoint.x, minX, maxX - 1),
                    Mathf.Clamp(roundedPoint.y, minY, maxY - 1),
                    roundedPoint.z)
                + (Vector3)GridOffset;
        }
    }

    protected override void UpdateData(PlacementState state)
    {
        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var pos = (Vector2)PlacementVisualContainer.transform.localPosition - GridOffset;
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

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }
}
