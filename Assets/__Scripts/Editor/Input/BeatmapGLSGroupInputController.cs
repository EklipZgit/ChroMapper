using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public abstract class BeatmapGLSGroupInputController<TData> : BeatmapInputController<GLSGroupContainer>,
                                                              CMInput.IGLSGroupSelectActions
    where TData : BaseEventBoxGroup
{
    private GLSGroupContainer lastHoveredContainer;
    private bool wasHovering;
    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private GLSEventGridProvider eventGridProvider;
    [SerializeField] private BoxSelectionPlacement boxSelectionPlacement;

    // Box selection owns its active and finishing-click frames, preventing GLS hover highlight and entry takeover.
    protected override bool ValidObject(GLSGroupContainer container) =>
        !boxSelectionPlacement.IsPlacing
        && boxSelectionPlacement.LastCompletionFrame != Time.frameCount
        && container.EventBoxGroupData is TData;

    // Expose this controller's raycast-owned inner node for outer-track per-node scroll bindings.
    protected BaseGLSEvent HoveredEventData => HoveredObject?.PreviewEventData;

    protected override void HandleHoverChanged(GLSGroupContainer container)
    {
        // Keep shared precision from claiming wheel chords while a ghost node owns them.
        if (lastHoveredContainer != container) lastHoveredContainer?.SetGroupHighlighted(false);
        container?.SetGroupHighlighted(true);
        lastHoveredContainer = container;
    }

    protected override void LateUpdate()
    {
        // Claim shared scroll precision only while this type-owned outer controller has a valid hover target.
        if (wasHovering != IsHovering)
        {
            wasHovering = IsHovering;
            GLSEventInputHoverTracker.SetHovering(wasHovering);
        }

        base.LateUpdate();
    }

    protected virtual void OnDisable()
    {
        // Release shared precision when this outer controller is disabled during a mode or scene transition.
        if (!wasHovering) return;
        wasHovering = false;
        GLSEventInputHoverTracker.SetHovering(false);
    }

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
        // Log the callback ordering against box selection before changing click-priority behavior again.
        // if (context.performed)
        // {
        //     Debug.Log(
        //         $"[GLS Drag Entry] frame={Time.frameCount}, boxState={boxSelectionPlacement.State}, " +
        //         $"boxPlacing={boxSelectionPlacement.IsPlacing}, hovering={IsHovering}, " +
        //         $"groupId={HoveredObject?.EventBoxGroupData?.ID}.");
        // }

        // Ignore the finishing box-selection click even though box placement has already reset to Idle this frame.
        if (context.performed
            && CanInteract
            && boxSelectionPlacement.LastCompletionFrame != Time.frameCount
            && EditContext.EditingMode.HasFlag(EditingMode.GLS)
            && IsHovering)
        {
            var clickedEvent = HoveredObject.PreviewEventData;
            if (atsc.CurrentSongBpmTime < HoveredObject.EventBoxGroupData.SongBpmTime)
                atsc.MoveToSongBpmTime(HoveredObject.EventBoxGroupData.SongBpmTime);

            // order of operations matter bc Visual Beat Origin is reset on edit mode change, so set group context after changing edit mode
            EditContext.EditingMode = EditingMode.EventBox;
            atsc.VisualBeatOrigin = HoveredObject.EventBoxGroupData.SongBpmTime;
            eventGridProvider.GroupContext = HoveredObject.EventBoxGroupData;
            // Select the represented inner event after the mode transition clears the prior outer-track selection.
            if (clickedEvent != null) SelectionController.Select(clickedEvent);
        }
    }

    public void OnExitGroup(InputAction.CallbackContext context)
    {
        if (context.performed && EditContext.EditingMode.HasFlag(EditingMode.EventBox))
            EditContext.EditingMode = EditingMode.GLS;
    }

    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        // Middle-click is type-owned by the specific outer GLS controller; do not mirror an entire color group too.
    }
}
