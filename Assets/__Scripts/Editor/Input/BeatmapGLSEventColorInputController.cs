using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventColorInputController : BeatmapGLSEventInputController<BaseLightColorBase>,
                                                   CMInput.IGLSColorObjectsActions
{
    public event Action<int> OnColorChanged;
    public event Action<float> OnBrightnessChanged;
    public event Action<int> OnFadeChanged;
    public event Action<int> OnStrobeFrequencyChanged;
    public event Action<float> OnStrobeBrightnessChanged;
    public event Action<int> OnSoftStrobeChanged;
    private float currentBrightness;
    private int currentFade;
    private int currentStrobeFrequency;
    private float currentStrobeBrightness;
    private int currentSoftStrobe;

    // Keep the keybind label aligned with the primary light color it selects.
    public void OnPrimaryLightColor(InputAction.CallbackContext context)
    {
        if (context.performed) OnColorPerformed(LightColor.Red);
    }

    // Keep the keybind label aligned with the secondary light color it selects.
    public void OnSecondaryLightColor(InputAction.CallbackContext context)
    {
        if (context.performed) OnColorPerformed(LightColor.Blue);
    }

    // Keep the keybind label aligned with the white light color it selects.
    public void OnWhiteLightColor(InputAction.CallbackContext context)
    {
        if (context.performed) OnColorPerformed(LightColor.White);
    }

    // Avoid the gameplay-mode Bomb binding while routing Basic Events and both GLS views through the color tile handler.
    public void OnChromaLightColor(InputAction.CallbackContext context)
    {
        if (context.performed && !EditContext.EditingMode.HasFlag(EditingMode.Gameplay))
        {
            ColorTypeController.RequestChromaLightColor();
        }
    }

    // GLS Color Objects remains enabled in every workspace, so restrict the shared C binding to GLS workspaces.
    // Event Box is the inner Global Lights color-node view; excluding it prevented its strobe hotkey from working.
    // The explicit modes keep Beatmap hotkeys from opening the strobe picker outside Global Lights.
    public void OnStrobeChromaColor(InputAction.CallbackContext context)
    {
        if (context.performed
            && (EditContext.EditingMode == EditingMode.GLS || EditContext.EditingMode == EditingMode.EventBox))
        {
            StrobeColorPickerController.ToggleEnabled();
        }
    }

    private void OnColorPerformed(LightColor lightColor)
    {
        if (KeybindsController.IsHoverKeyHeld && IsHovering)
        {
            if (IsHovering)
                GLSEventColorCommand.SetColor(HoveredObject.EventData as BaseLightColorBase, (int)lightColor);
        }
        else
        {
            NotifyColorChanged(lightColor);
        }
    }

    public void NotifyColorChanged(LightColor color)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnColorChanged?.Invoke((int)color);
    }

    private void OnBrightnessPerformed(int fadeChange, float brightness, EaseType easeType)
    {
        if (KeybindsController.IsHoverKeyHeld && IsHovering)
        {
            if (IsHovering)
            {
                GLSEventColorCommand.SetBrightnessAndEasing(
                    HoveredObject.EventData as BaseLightColorBase,
                    brightness,
                    easeType);
            }
        }
        else
        {
            NotifyFadeChanged(fadeChange);
            NotifyBrightnessChanged(brightness);
        }
    }
    
    public void OnStatic0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(-1, 0f, EaseType.None);
    }

    public void OnStatic50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(-1, 0.5f, EaseType.None);
    }

    public void OnStatic100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(-1, 1f, EaseType.None);
    }

    public void OnFade0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(0, 0f, EaseType.Linear);
    }

    public void OnFade50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(0, 0.5f, EaseType.Linear);
    }

    public void OnFade100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessPerformed(0, 1f, EaseType.Linear);
    }

    private void OnSetBrightnessOnlyPerformed(float brightness)
    {
        if (KeybindsController.IsHoverKeyHeld && IsHovering)
        {
            if (IsHovering)
            {
                GLSEventColorCommand.SetBrightness(
                    HoveredObject.EventData as BaseLightColorBase,
                    brightness);
            }
        }
        else
        {
            NotifyBrightnessChanged(brightness);
        }
    }

    public void OnBrightness0(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(0f);
    }

    public void OnBrightness10(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.1f);
    }

    public void OnBrightness20(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.2f);
    }

    public void OnBrightness30(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.3f);
    }

    public void OnBrightness40(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.4f);
    }

    public void OnBrightness50(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.5f);
    }

    public void OnBrightness60(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.6f);
    }

    public void OnBrightness70(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.7f);
    }

    public void OnBrightness80(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.8f);
    }

    public void OnBrightness90(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(.9f);
    }

    public void OnBrightness100(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(1f);
    }

    public void OnBrightness120(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(1.2f);
    }

    public void OnBrightness150(InputAction.CallbackContext context)
    {
        if (context.performed) OnSetBrightnessOnlyPerformed(1.5f);
    }

    // GLSEasingTypeRibbonInputTest: a color-ribbon hit edits the transition's ahead node; alt+scroll owns
    // its customData.easingType toggle exactly like the Basic Event ribbon chord.
    private bool TryGetRibbonTransition(BaseLightColorBase source, out BaseLightColorBase transition) =>
        GLSEventCommon.TryGetColorTransitionTarget(HoveredObject, source, out transition);

    // Keep hover value mutations under the Tweak prefix in keybind settings.
    public void OnTweakBrightnessHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        if (TryGetRibbonTransition(evt, out var transition))
        {
            GLSEventHoverMutation.CycleColorLerpType(context, transition);
        }
        else
        {
            GLSEventHoverMutation.AdjustColorBrightness(context, evt, ScrollPrecisionController);
        }

        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void NotifyBrightnessChanged(float value)
    {
        currentBrightness = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnBrightnessChanged?.Invoke(value);
    }

    public void NotifyFadeChanged(int value)
    {
        currentFade = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnFadeChanged?.Invoke(value);
    }

    public void OnStrobeOn(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobePerformed(1);
    }

    public void OnStrobeOff(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobePerformed(0);
    }

    private void OnStrobePerformed(int toggledOn)
    {
        if (KeybindsController.IsHoverKeyHeld && IsHovering)
        {
            if (IsHovering)
            {
                GLSEventColorCommand.SetStrobeFade(HoveredObject.EventData as BaseLightColorBase, toggledOn);
            }
        }
        else
        {
            NotifyStrobeFrequencyChanged(toggledOn);
        }
    }

    public void OnTweakStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        // GLSEasingTypeRibbonInputTest: Ctrl+Alt stays a no-op on ribbons like the Basic Event ribbon;
        // node-only chords must not leak onto the transition's source node.
        if (GLSEventCommon.IsColorTransitionRibbonHit(HoveredObject))
        {
            return;
        }

        GLSEventHoverMutation.AdjustColorFrequency(context, evt, ScrollPrecisionController);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void NotifyStrobeFrequencyChanged(int value)
    {
        currentStrobeFrequency = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeFrequencyChanged?.Invoke(value);
    }

    private int strobeBrightnessCycle;
    private float[] strobeBrightness = { 0f, 0.5f, 1f };

    public void OnStrobeBrightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            strobeBrightnessCycle++;
            strobeBrightnessCycle %= strobeBrightness.Length;
            NotifyStrobeBrightnessChanged(strobeBrightness[strobeBrightnessCycle]);
        }
    }

    public void OnTweakStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        // GLSEasingTypeRibbonInputTest: the three-modifier chord is node-only; a ribbon hit must not leak
        // strobe brightness edits onto the transition's source node.
        if (GLSEventCommon.IsColorTransitionRibbonHit(HoveredObject))
        {
            return;
        }

        GLSEventHoverMutation.AdjustColorStrobeBrightness(context, evt, ScrollPrecisionController);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnToggleStrobeFadeHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        // GLSEasingTypeRibbonInputTest: Shift+scroll on a ribbon cycles the ahead node's strobeEasing,
        // matching the node chord.
        var target = TryGetRibbonTransition(evt, out var transition) ? transition : evt;
        if (GLSEventHoverMutation.CycleColorStrobeFade(context, target))
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        // GLSEasingTypeRibbonInputTest: Ctrl+Shift+scroll on a ribbon cycles the ahead node's colorEasing,
        // matching the node chord.
        var target = TryGetRibbonTransition(evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustColorEasing(context, target);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnTweakStrobeColorEasingHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        // GLSEasingTypeRibbonInputTest: Alt+Shift+scroll on a ribbon cycles the ahead node's
        // strobeColorEasing, matching the node chord.
        var target = TryGetRibbonTransition(evt, out var transition) ? transition : evt;
        GLSEventHoverMutation.AdjustStrobeColorEasing(context, target);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void NotifyStrobeBrightnessChanged(float value)
    {
        currentStrobeBrightness = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeBrightnessChanged?.Invoke(value);
    }

    public void OnSoftStrobe(InputAction.CallbackContext context)
    {
        if (context.performed) NotifySoftStrobeChanged(0);
    }

    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        TryGetHoveredEvent(context, out var evt);
        GLSEventHoverMutation.MirrorColor(context, evt);
        if (evt != null)
        {
            RefreshHoveredVisualAfterMutation();
        }
    }

    public void OnApplyToSelected(InputAction.CallbackContext context) { }

    public void NotifySoftStrobeChanged(int value)
    {
        currentSoftStrobe = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnSoftStrobeChanged?.Invoke(value);
    }

    // Replay the last provider notification for a GLS view that initialized after map loading.
    public void RefreshViews()
    {
        OnBrightnessChanged?.Invoke(currentBrightness);
        OnFadeChanged?.Invoke(currentFade);
        OnStrobeFrequencyChanged?.Invoke(currentStrobeFrequency);
        OnStrobeBrightnessChanged?.Invoke(currentStrobeBrightness);
        OnSoftStrobeChanged?.Invoke(currentSoftStrobe);
    }
}
