using Beatmap.Base;

public class BeatmapGLSGroupRotationInputController : BeatmapGLSGroupInputController<BaseLightRotationEventBoxGroup>, CMInput.IGLSRotationObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve the current hovered preview event for this controller's GLS node type.
    private bool TryGetHoveredEvent(UnityEngine.InputSystem.InputAction.CallbackContext context, out BaseLightRotationBase evt) =>
        TryGetHoveredPreviewEvent(context, out evt);

    private ScrollPrecisionController Precision => ResolvePrecision(ref precision);

    public void OnAngleHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustRotation(context, TryGetHoveredEvent(context, out var evt) ? evt : null, Precision);
    }

    public void OnTweakLoopHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustRotationLoop(context, TryGetHoveredEvent(context, out var evt) ? evt : null);
    }

    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSEventHoverMutation.AdjustRotationEasing(context, resolved);
    }

    public void OnCycleAxisHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        // The authored axis action targets this controller's outer preview event.
        var resolved = TryGetHoveredEvent(context, out var evt) ? evt : null;
        GLSCommonCommand.CycleEventAxis(context, resolved);
    }

    public void OnCycleDirectionHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.CycleRotationDirection(context, TryGetHoveredEvent(context, out var evt) ? evt : null);
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
