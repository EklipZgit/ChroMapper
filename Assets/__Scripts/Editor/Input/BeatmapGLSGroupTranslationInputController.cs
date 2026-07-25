using Beatmap.Base;

public class
    BeatmapGLSGroupTranslationInputController : BeatmapGLSGroupInputController<BaseLightTranslationEventBoxGroup>, CMInput.IGLSTranslationObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve only this controller's raycast-owned preview event for outer-track scroll input.
    private bool TryGetHoveredEvent(out BaseLightTranslationBase evt)
    {
        evt = IsHovering ? HoveredObject?.PreviewEventData as BaseLightTranslationBase : null;
        return evt != null && ReferenceEquals(evt.EventBoxGroupData, HoveredObject.EventBoxGroupData);
    }

    private ScrollPrecisionController Precision => precision ??= FindFirstObjectByType<ScrollPrecisionController>();

    public void OnValueHover(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustTranslation(context, TryGetHoveredEvent(out var evt) ? evt : null, Precision);
    }

    // Use the explicit Ctrl+Alt action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context) =>
        GLSEventHoverMutation.AdjustTranslationEasing(context, TryGetHoveredEvent(out var evt) ? evt : null);

    // Outer previews expose only hover-specific mutations; fixed value actions remain inner-editor controls.
    public void OnValuen100(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValuen50(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue0(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue50(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
    public void OnValue100(UnityEngine.InputSystem.InputAction.CallbackContext context) { }
}
