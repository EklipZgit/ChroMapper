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

    public void OnEnterGroup(InputAction.CallbackContext context)
    {
        if (context.performed && CanInteract && editContext.EditingMode.HasFlag(EditingMode.GLS) && IsHovering)
        {
            eventGridProvider.GroupContext = HoveredObject.EventBoxGroupData;
            if (atsc.CurrentSongBpmTime < HoveredObject.EventBoxGroupData.SongBpmTime)
                atsc.MoveToSongBpmTime(HoveredObject.EventBoxGroupData.SongBpmTime);
            editContext.EditingMode = EditingMode.EventBox;
        }
    }

    public void OnExitGroup(InputAction.CallbackContext context)
    {
        if (context.performed && editContext.EditingMode.HasFlag(EditingMode.EventBox))
        {
            editContext.EditingMode = EditingMode.GLS;
        }
    }
}
