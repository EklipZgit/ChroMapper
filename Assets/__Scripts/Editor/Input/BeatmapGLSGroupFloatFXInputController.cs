using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupFloatFXInputController : BeatmapGLSGroupInputController<BaseVfxEventEventBoxGroup>, CMInput.IGLSFloatFXObjectsActions
{
    private ScrollPrecisionController precision;

    // Resolve only this controller's raycast-owned preview event for outer-track scroll input.
    private bool TryGetHoveredEvent(out BaseFxEventFloat evt)
    {
        // Unity hover containers need explicit null checks before resolving their preview event.
        evt = IsHovering && HoveredObject != null
            ? HoveredObject.PreviewEventData as BaseFxEventFloat
            : null;
        return evt != null && ReferenceEquals(evt.EventBoxGroupData, HoveredObject.EventBoxGroupData);
    }

    private ScrollPrecisionController Precision => ResolvePrecision(ref precision);

    public void OnValueHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustFloatFx(context, TryGetHoveredEvent(out var evt) ? evt : null, Precision);
    }

    // Use the explicit Ctrl+Alt action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(UnityEngine.InputSystem.InputAction.CallbackContext context) =>
        GLSEventHoverMutation.AdjustFloatFxEasing(context, TryGetHoveredEvent(out var evt) ? evt : null);

    // Outer previews expose only hover-specific mutations; fixed value actions remain inner-editor controls.
    public void OnValuen100(InputAction.CallbackContext context) { }
    public void OnValuen50(InputAction.CallbackContext context) { }
    public void OnValue0(InputAction.CallbackContext context) { }
    public void OnValue50(InputAction.CallbackContext context) { }
    public void OnValue100(InputAction.CallbackContext context) { }
}
