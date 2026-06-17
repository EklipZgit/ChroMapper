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
    [SerializeField] private RotationCallbackController rotationCallbackController;
    [SerializeField] private EventAppearanceSO eventAppearance;

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
            case BaseRotationEvent evt:
                {
                    var prec = scrollPrecisionController.GetCurrentRotationPrecision();
                    evt.Rotation = Mathf.Round((evt.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
                    tracksManager.RefreshTracks();

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

        if (c is RotationEventContainer e) eventAppearance.SetAppearance(e);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                c.ObjectData,
                c.ObjectData,
                original,
                mergeType: ActionMergeType.ModifyRotationValue));
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
        var original = BeatmapFactory.Clone(e.ObjectData);

        e.EventData.Rotation *= -1;
        tracksManager.RefreshTracks();

        eventAppearance.SetAppearance(e);
        BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(e.ObjectData, e.ObjectData, original));
    }

    private void ModifyHover(RotationEventContainer e, int modifier)
    {
        var original = BeatmapFactory.Clone(e.ObjectData);

        var prec = scrollPrecisionController.GetCurrentRotationPrecision();
        e.EventData.Rotation = Mathf.Round((e.EventData.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
        tracksManager.RefreshTracks();

        if (e.EventData.CompareTo(original) == 0) return;

        eventAppearance.SetAppearance(e);
        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                e.ObjectData,
                e.ObjectData,
                original,
                mergeType: ActionMergeType.EventMainTweak));
    }
}
