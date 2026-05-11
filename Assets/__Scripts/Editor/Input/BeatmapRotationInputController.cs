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
    [SerializeField] private BeatmapRuntimeContext beatmapRuntimeContext;
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private RotationCallbackController rotationCallbackController;
    [SerializeField] private EventAppearanceSO eventAppearance;

    public void OnRotateClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed || !EditContext.EditingMode.HasFlag(editMode) || !RaycastFirstObject(out var con))
            return;
        switch (con)
        {
            case EventContainer:
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
            case EventContainer:
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
        rotationCallbackController.Rotation = Mathf.RoundToInt(
            Mathf.Round((rotationCallbackController.Rotation + prec) * 1_000f) / 1_000f);
    }

    public void OnGridRotateCounterClockwise(InputAction.CallbackContext context)
    {
        if (CustomStandaloneInputModule.IsPointerOverGameObject<GraphicRaycaster>(0, true)) return;
        if (!context.performed
            || !EditContext.EditingMode.HasFlag(editMode)
            || BeatSaberSongContainer.Instance.Map.MajorVersion != 4)
            return;
        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        rotationCallbackController.Rotation = Mathf.RoundToInt(
            Mathf.Round((rotationCallbackController.Rotation - prec) * 1_000f) / 1_000f);
    }

    private void RotateObject(ObjectContainer c, bool clockwise)
    {
        var original = BeatmapFactory.Clone(c.ObjectData);

        var modifier = clockwise ? 1 : -1;
        switch (c.ObjectData)
        {
            case BaseEvent evt:
                {
                    if (evt.IsLaneRotationEvent())
                    {
                        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
                        var value = Mathf.Round((evt.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
                        evt.Rotation += value;
                        tracksManager.RefreshTracks();
                    }

                    break;
                }
            case BaseGrid grid:
                {
                    var prec = scrollPrecisionController.GetCurrentRotationPrecision();
                    grid.Rotation += Mathf.RoundToInt(modifier * prec);
                    tracksManager.RefreshTracks();
                    break;
                }
        }

        if (c.ObjectData.CompareTo(original) == 0) return;

        if (c is EventContainer e) eventAppearance.SetAppearance(e, beatmapRuntimeContext.TracksDefinition);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                c.ObjectData,
                c.ObjectData,
                original,
                mergeType: ActionMergeType.ModifyRotationValue));
    }
}
