using Beatmap.Containers;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapNJSEventInputController : BeatmapInputController<NJSEventContainer>, CMInput.INJSEventObjectsActions
{
    [SerializeField] private ScrollPrecisionController scrollPrecisionController;

    public void OnTweakNJSValue(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        RaycastFirstObject(out var containerToEdit);
        if (containerToEdit == null) return;
        if (containerToEdit.NJSData.UsePrevious == 1) return;

        var original = BeatmapFactory.Clone(containerToEdit.ObjectData);

        var modifier = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue)
            * scrollPrecisionController.GetCurrentTimePrecision();

        containerToEdit.NJSData.RelativeNJS += modifier;
        if (containerToEdit.NJSData.RelativeNJS
            <= -BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed)
        {
            containerToEdit.NJSData.RelativeNJS =
                scrollPrecisionController.GetCurrentTimePrecision()
                - BeatSaberSongContainer.Instance.MapDifficultyInfo.NoteJumpSpeed;
        }

        if (containerToEdit.NJSData.CompareTo(original) == 0) return;

        containerToEdit.UpdateNJSText();

        BeatmapActionContainer.AddAction(
            new BeatmapObjectModifiedAction(
                containerToEdit.ObjectData,
                containerToEdit.ObjectData,
                original,
                "Modified NJS Event Value",
                mergeType: ActionMergeType.ModifyNJSEventValue));
    }
}
