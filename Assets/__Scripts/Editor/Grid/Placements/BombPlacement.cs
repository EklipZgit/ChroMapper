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

    protected override void UpdatePlacement(
        Vector3 rawHit,
        Vector3 roundedHit,
        PlacementState state)
    {
        // Check if Chroma Color notes button is active and apply _color
        QueuedData.CustomColor = CanPlaceChromaObjects && dropdown.Visible
            ? colorPicker.CurrentColor
            : null;

        var posX = (int)roundedHit.x;
        var posY = (int)roundedHit.y;

        var vanillaX = Mathf.Clamp(posX + 2, 0, 3);
        var vanillaY = Mathf.Clamp(posY, 0, 2);

        var vanillaBounds = (vanillaX == posX + 2) && vanillaY == posY;

        QueuedData.PosX = vanillaX;
        QueuedData.PosY = vanillaY;

        if (PrecisionPlacementController.IsEnabled)
        {
            rawHit.z = SongBpmTime * EditorScaleController.EditorScale;

            var precision = Settings.Instance.PrecisionPlacementGridPrecision;
            roundedHit = (Vector2)Vector2Int.RoundToInt((Vector2)rawHit * precision) / precision;
            PlacementVisualContainer.transform.localPosition = roundedHit;

            QueuedData.CustomCoordinate = (Vector2)roundedHit;
        }
        else
        {
            QueuedData.CustomCoordinate = !vanillaBounds
                ? (Vector2)roundedHit - VanillaOffset + PrecisionOffset
                : null;
        }

        PlacementVisualContainer.MaterialPropertyBlock.SetFloat(alwaysTranslucent, 1);
        PlacementVisualContainer.UpdateMaterials();

        PlacementVisualContainer.NoteData = QueuedData;
        PlacementVisualContainer.UpdateGridPosition();
    }

    protected override void TransferQueuedToDraggedObject(ref BaseNote dragged, BaseNote queued)
    {
        dragged.JsonTime = queued.JsonTime;
        dragged.PosX = queued.PosX;
        dragged.PosY = queued.PosY;
        dragged.CustomCoordinate = queued.CustomCoordinate;
    }
}
