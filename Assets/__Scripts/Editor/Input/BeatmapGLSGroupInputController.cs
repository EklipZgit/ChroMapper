using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public abstract class BeatmapGLSGroupInputController<TData> : BeatmapInputController<GLSGroupContainer>,
                                                              CMInput.IGLSGroupSelectActions
    where TData : BaseEventBoxGroup
{
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GLSEventGridProvider eventGridProvider;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;

    protected override bool ValidObject(GLSGroupContainer container) => container.EventBoxGroupData is TData;

    private bool CanInteract =>
        !Input.GetMouseButton((int)MouseButton.Right)
        && KeybindsController.IsMouseInWindow
        && !SongTimelineController.IsHovering
        && !SceneTransitionManager.IsLoading
        && !DeleteToolController.IsActive
        && !NodeEditorController.IsActive
        && !UIMode.PreviewMode
        && !MassSelect;

    // TODO: prevent interaction after box selection is complete, race condition or somethin
    public void OnEnterGroup(InputAction.CallbackContext context)
    {
        if (context.performed && CanInteract && EditContext.EditingMode.HasFlag(EditingMode.GLS) && IsHovering)
        {
            eventGridProvider.GroupContext = HoveredObject.EventBoxGroupData;
            if (atsc.CurrentSongBpmTime < HoveredObject.EventBoxGroupData.SongBpmTime)
                atsc.MoveToSongBpmTime(HoveredObject.EventBoxGroupData.SongBpmTime);
            EditContext.EditingMode = EditingMode.EventBox;
        }
    }

    public void OnExitGroup(InputAction.CallbackContext context)
    {
        if (context.performed && EditContext.EditingMode.HasFlag(EditingMode.EventBox))
            EditContext.EditingMode = EditingMode.GLS;
    }

    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering) GLSGroupCommand.Mirror(HoveredObject.EventBoxGroupData);
    }
}
