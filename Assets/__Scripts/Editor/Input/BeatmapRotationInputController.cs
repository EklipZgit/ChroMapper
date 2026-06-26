using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class BeatmapRotationInputController : BeatmapInputController<ObjectContainer>,
                                              CMInput.IRotationObjectsActions
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private LaneRotationProvider laneRotationProvider;
    [SerializeField] private GridLane gridLane;

    public event Action<float> OnRotationInput;

    public void OnRotateClockwise(InputAction.CallbackContext context) => HandleRotateDirectional(context, true);

    public void OnRotateCounterClockwise(InputAction.CallbackContext context)
    {
        HandleRotateDirectional(context, false);
    }

    public void HandleRotateDirectional(InputAction.CallbackContext context, bool clockwise)
    {
        Debug.Log("wtf2");
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed)
            return;
        Debug.Log("w");
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        var modifier = clockwise ? 1 : -1;

        if (KeybindsController.IsHoverKeyHeld && KeybindsController.IsControlKeyHeld)
        {
            if (EditContext.EditingMode.HasFlag(editMode)
                && !gridLane.Hide)
                RotationCommand.PlaceEventInPlace(atsc.CurrentJsonTime, clockwise, prec);

            laneRotationProvider.SetEditRotation(
                Mathf.RoundToInt(
                    Mathf.Round((laneRotationProvider.EditRotation + (modifier * prec)) * 1_000f) / 1_000f));
        }
        else if (KeybindsController.IsHoverKeyHeld
            && EditContext.EditingMode.HasFlag(editMode)
            && RaycastFirstObject(out var con))
        {
            switch (con)
            {
                case RotationEventContainer:
                    RotationCommand.RotateObject(con.ObjectData, clockwise, prec);
                    break;
                case NoteContainer:
                case ObstacleContainer:
                case ArcContainer:
                case ChainContainer:
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                    RotationCommand.RotateObject(con.ObjectData, clockwise, prec);
                    break;
            }
        }
    }

    public void OnRotateCopy(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con))
            return;

        if (KeybindsController.IsHoverKeyHeld && KeybindsController.IsControlKeyHeld)
        {
            int rotation;
            switch (con)
            {
                case RotationEventContainer evt:
                    rotation = (int)evt.EventData.Rotation;
                    break;
                case NoteContainer:
                case ObstacleContainer:
                case ArcContainer:
                case ChainContainer:
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                    rotation = (con.ObjectData as BaseGrid)?.Rotation ?? 0;
                    break;
                default:
                    return;
            }

            laneRotationProvider.SetEditRotation(rotation);
        }
        else if (KeybindsController.IsHoverKeyHeld)
        {
            switch (con)
            {
                case RotationEventContainer evt:
                    RotationCommand.SetRotation(
                        con.ObjectData,
                        Mathf.DeltaAngle(evt.EventData.Rotation, laneRotationProvider.EditRotation));
                    break;
                case NoteContainer:
                case ObstacleContainer:
                case ArcContainer:
                case ChainContainer:
                    if (BeatSaberSongContainer.Instance.Map.MajorVersion != 4) return;
                    RotationCommand.SetRotation(con.ObjectData, laneRotationProvider.EditRotation);
                    break;
            }
        }
    }

    public void OnModify(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsHoverKeyHeld
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        RotationCommand.ModifyHover(e.EventData, modifier, prec);
    }

    public void OnInvert(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !KeybindsController.IsHoverKeyHeld
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || !RaycastFirstObject(out var con)
            || con is not RotationEventContainer e)
            return;

        RotationCommand.Invert(e.EventData);
    }

    public void OnRotation15Degrees(InputAction.CallbackContext context) => HandleRotationInput(context, 15);
    public void OnRotation30Degrees(InputAction.CallbackContext context) => HandleRotationInput(context, 15);
    public void OnRotation45Degrees(InputAction.CallbackContext context) => HandleRotationInput(context, 15);
    public void OnRotation60Degrees(InputAction.CallbackContext context) => HandleRotationInput(context, 15);

    public void HandleRotationInput(InputAction.CallbackContext context, float rotation)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)
            || !context.performed
            || !EditContext.EditingMode.HasFlag(editMode))
            return;

        if (KeybindsController.IsHoverKeyHeld)
        {
            if (RaycastFirstObject(out var con)
                && con is RotationEventContainer e)
                RotationCommand.SetRotation(e.EventData, rotation);
        }
        else
            OnRotationInput?.Invoke(rotation);
    }
}
