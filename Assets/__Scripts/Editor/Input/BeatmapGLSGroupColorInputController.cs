using System;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSGroupColorInputController : BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>,
                                                   CMInput.IGLSColorObjectsActions
{
    private ScrollPrecisionController scrollPrecisionController;

    // Resolve the current hovered preview event for this controller's GLS node type, keeping the
    // physically hit container so ribbon chords can test against the exact object under the cursor.
    private bool TryGetHoveredEvent(
        InputAction.CallbackContext context,
        out BaseLightColorBase evt,
        out GLSGroupContainer container) =>
        TryGetHoveredPreviewEvent(context, out evt, out container);

    private ScrollPrecisionController ScrollPrecisionController =>
        ResolvePrecision(ref scrollPrecisionController);

    // GLSEasingTypeRibbonInputTest: a color-ribbon hit edits the transition's ahead node; alt+scroll owns
    // its customData.easingType toggle exactly like the Basic Event ribbon chord.
    // A masked outer strip is a no-op ribbon hover, never a request to change the group's source node.
    private bool TryGetRibbonTransition(
        GLSGroupContainer container,
        BaseLightColorBase source,
        out BaseLightColorBase transition) =>
        GLSEventCommon.IsColorRibbonHover(container, source, out transition);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakBrightnessHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        if (TryGetRibbonTransition(container, evt, out var transition))
        {
            GLSEventHoverMutation.CycleColorLerpType(context, transition);
        }
        else
        {
            GLSEventHoverMutation.AdjustColorBrightness(context, evt, ScrollPrecisionController);
        }
    }

    public void OnTweakStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        // GLSEasingTypeRibbonInputTest: Ctrl+Alt stays a no-op on ribbons like the Basic Event ribbon;
        // node-only chords must not leak onto the transition's source node.
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
        {
            return;
        }

        GLSEventHoverMutation.AdjustColorFrequency(context, evt, ScrollPrecisionController);
    }

    public void OnTweakStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        // GLSEasingTypeRibbonInputTest: the three-modifier chord is node-only; a ribbon hit must not leak
        // strobe brightness edits onto the transition's source node.
        if (GLSEventCommon.IsColorTransitionRibbonHit(container))
        {
            return;
        }

        GLSEventHoverMutation.AdjustColorStrobeBrightness(context, evt, ScrollPrecisionController);
    }

    public void OnToggleStrobeFadeHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        // GLSEasingTypeRibbonInputTest: Shift+scroll on a ribbon cycles the ahead node's strobeEasing,
        // matching the node chord.
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.CycleColorStrobeFade(context, target);
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        // GLSEasingTypeRibbonInputTest: Ctrl+Shift+scroll on a ribbon cycles the ahead node's colorEasing,
        // matching the node chord.
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustColorEasing(context, target);
    }

    public void OnTweakStrobeColorEasingHover(InputAction.CallbackContext context)
    {
        var evt = TryGetHoveredEvent(context, out var resolved, out var container) ? resolved : null;
        // GLSEasingTypeRibbonInputTest: Alt+Shift+scroll on a ribbon cycles the ahead node's
        // strobeColorEasing, matching the node chord.
        var target = TryGetRibbonTransition(container, evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustStrobeColorEasing(context, target);
    }

    // Outer previews support only hover-specific mutations; non-hover actions remain owned by the inner editor.
    public void OnPrimaryLightColor(InputAction.CallbackContext context) { }
    public void OnSecondaryLightColor(InputAction.CallbackContext context) { }
    public void OnWhiteLightColor(InputAction.CallbackContext context) { }
    public void OnChromaLightColor(InputAction.CallbackContext context) { }
    public void OnStrobeChromaColor(InputAction.CallbackContext context) { }
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
        GLSEventHoverMutation.MirrorColor(context, TryGetHoveredEvent(context, out var evt, out _) ? evt : null);
    }
}
