using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupColorInputController : BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>,
                                                   CMInput.IGLSColorObjectsActions
{
    private ScrollPrecisionController scrollPrecisionController;

    // Resolve only this controller's current preview event; outer input must never use shared hover state.
    private bool TryGetHoveredEvent(out BaseLightColorBase evt)
    {
        // Unity hover containers need explicit null checks before resolving their preview event.
        evt = IsHovering && HoveredObject != null
            ? HoveredObject.PreviewEventData as BaseLightColorBase
            : null;
        return evt != null && ReferenceEquals(evt.EventBoxGroupData, HoveredObject.EventBoxGroupData);
    }

    private ScrollPrecisionController ScrollPrecisionController =>
        ResolvePrecision(ref scrollPrecisionController);

    public void OnBrightnessHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorBrightness(context, TryGetHoveredEvent(out var evt) ? evt : null, ScrollPrecisionController);
    }

    public void OnStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorFrequency(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    public void OnStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorStrobeBrightness(context, TryGetHoveredEvent(out var evt) ? evt : null, ScrollPrecisionController);
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.AdjustColorEasing(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }

    // Outer previews support only hover-specific mutations; non-hover actions remain owned by the inner editor.
    public void OnColor0Light(InputAction.CallbackContext context) { }
    public void OnColor1Light(InputAction.CallbackContext context) { }
    public void OnColorWLight(InputAction.CallbackContext context) { }
    public void OnStatic0Brightness(InputAction.CallbackContext context) { }
    public void OnStatic50Brightness(InputAction.CallbackContext context) { }
    public void OnStatic100Brightness(InputAction.CallbackContext context) { }
    public void OnFade0Brightness(InputAction.CallbackContext context) { }
    public void OnFade50Brightness(InputAction.CallbackContext context) { }
    public void OnFade100Brightness(InputAction.CallbackContext context) { }
    public void OnBrightness0(InputAction.CallbackContext context) { }
    public void OnBrightness10(InputAction.CallbackContext context) { }
    public void OnBrightness20(InputAction.CallbackContext context) { }
    public void OnBrightness30(InputAction.CallbackContext context) { }
    public void OnBrightness40(InputAction.CallbackContext context) { }
    public void OnBrightness50(InputAction.CallbackContext context) { }
    public void OnBrightness60(InputAction.CallbackContext context) { }
    public void OnBrightness70(InputAction.CallbackContext context) { }
    public void OnBrightness80(InputAction.CallbackContext context) { }
    public void OnBrightness90(InputAction.CallbackContext context) { }
    public void OnBrightness100(InputAction.CallbackContext context) { }
    public void OnBrightness120(InputAction.CallbackContext context) { }
    public void OnBrightness150(InputAction.CallbackContext context) { }
    public void OnStrobeOn(InputAction.CallbackContext context) { }
    public void OnStrobeOff(InputAction.CallbackContext context) { }
    public void OnStrobeBrightness(InputAction.CallbackContext context) { }
    public void OnSoftStrobe(InputAction.CallbackContext context) { }
    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        GLSEventHoverMutation.MirrorColor(context, TryGetHoveredEvent(out var evt) ? evt : null);
    }
}
