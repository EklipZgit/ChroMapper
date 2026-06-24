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
    [SerializeField] private LaneRotationProvider laneRotationProvider;

    public void OnRotateClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed || !EditContext.EditingMode.HasFlag(editMode) || !RaycastFirstObject(out var con))
            return;
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        switch (con)
        {
            case RotationEventContainer:
                RotationCommand.RotateObject(con.ObjectData, true, prec);
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotationCommand.RotateObject(con.ObjectData, true, prec);
                break;
        }
    }

    public void OnRotateCounterClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed || !EditContext.EditingMode.HasFlag(editMode) || !RaycastFirstObject(out var con))
            return;
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        switch (con)
        {
            case RotationEventContainer:
                RotationCommand.RotateObject(con.ObjectData, false, prec);
                break;
            case NoteContainer:
            case ObstacleContainer:
            case ArcContainer:
            case ChainContainer:
                if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                RotationCommand.RotateObject(con.ObjectData, false, prec);
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

    public void OnInvert(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        RotationCommand.Invert(e.EventData);
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
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        RotationCommand.ModifyHover(e.EventData, modifier, prec);
    }
}
