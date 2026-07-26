using Beatmap.Base;

public class BeatmapGLSGroupRotationInputController : BeatmapGLSGroupInputController<BaseLightRotationEventBoxGroup>, CMInput.IGLSRotationObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve only this controller's raycast-owned preview event for outer-track scroll input.
    private bool TryGetHoveredEvent(out BaseLightRotationBase evt)
    {
        // Unity hover containers need explicit null checks before resolving their preview event.
        evt = IsHovering && HoveredObject != null
            ? HoveredObject.PreviewEventData as BaseLightRotationBase
            : null;
        return evt != null && ReferenceEquals(evt.EventBoxGroupData, HoveredObject.EventBoxGroupData);
    }

    private ScrollPrecisionController Precision => ResolvePrecision(ref precision);

    public void OnAngleHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustRotation(context, TryGetHoveredEvent(out var evt) ? evt : null, Precision);
    }

    public void OnTweakLoopHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustRotationLoop(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustRotationEasing(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    public void OnCycleAxisHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // The authored axis action targets this controller's outer preview event.
        GLSCommonCommand.CycleEventAxis(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    public void OnCycleDirectionHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.CycleRotationDirection(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    // Outer previews expose only hover-specific mutations; fixed value actions remain inner-editor controls.
    public void OnAngle0(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnAngle90(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnAngle180(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnAngle270(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnRotationDirectionLeft(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnRotationDirectionAutomatic(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnRotationDirectionRight(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnChangeLoopCount(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnResetLoopCount(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
}
