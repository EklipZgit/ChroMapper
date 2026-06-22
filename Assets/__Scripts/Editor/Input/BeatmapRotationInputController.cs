using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapRotationInputController : BeatmapInputController<ObjectContainer>,
                                              CMInput.IRotationObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private LaneRotationProvider laneRotationProvider;

    public void OnRotateClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed || !EditContext.EditingMode.HasFlag(editMode) || !RaycastFirstObject(out var con))
            return;
        switch (con)
        {
            case RotationEventContainer:
                RotateObject(con, true);
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotateObject(con, true);
                break;
        }
    }

    public void OnRotateCounterClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed || !EditContext.EditingMode.HasFlag(editMode) || !RaycastFirstObject(out var con))
            return;
        switch (con)
        {
            case RotationEventContainer:
                RotateObject(con, false);
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotateObject(con, false);
                break;
        }
    }

    public void OnGridRotateClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || BeatSaberSongContainer.Instance.Map.MajorVersion != 4)
            return;
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        laneRotationProvider.SetEditRotation(
            Mathf.RoundToInt(
                Mathf.Round((laneRotationProvider.EditRotation + prec) * 1_000f) / 1_000f));
    }

    public void OnGridRotateCounterClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || BeatSaberSongContainer.Instance.Map.MajorVersion != 4)
            return;
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        laneRotationProvider.SetEditRotation(
            Mathf.RoundToInt(
                Mathf.Round((laneRotationProvider.EditRotation - prec) * 1_000f) / 1_000f));
    }

    private void RotateObject(ObjectContainer c, bool clockwise)
    {
        var originalObject = c.ObjectData;
        var newObject = BeatmapFactory.Clone(originalObject);

        var modifier = clockwise ? 1 : -1;
        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    var prec = scrollPrecisionController.GetCurrentRotationPrecision();
                    evt.Rotation = Mathf.Round((evt.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
                    break;
                }
            case BaseGrid grid:
                {
                    var prec = scrollPrecisionController.GetCurrentRotationPrecision();
                    grid.Rotation += Mathf.RoundToInt(modifier * prec);
                    break;
                }
        }

        if (newObject.CompareTo(originalObject) == 0) return;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
        tracksManager.RefreshTracks();
    }

    public void OnInvert(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        Invert(e);
    }

    public void OnModifyHover(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        ModifyHover(e, modifier);
    }

    private void Invert(RotationEventContainer e)
    {
        var originalObject = e.EventData;
        var newObject = BeatmapFactory.Clone(originalObject);

        newObject.Rotation *= -1;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
        tracksManager.RefreshTracks();
    }

    private void ModifyHover(RotationEventContainer e, int modifier)
    {
        var originalObject = e.EventData;
        var newObject = BeatmapFactory.Clone(originalObject);

        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        newObject.Rotation = Mathf.Round((newObject.Rotation + (modifier * prec)) * 1_000f) / 1_000f;

        if (newObject.CompareTo(originalObject) == 0) return;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
        tracksManager.RefreshTracks();
    }
}
