using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventFloatFXInputController : BeatmapGLSEventInputController<BaseFxEventFloat>,
                                                     CMInput.IGLSFloatFXObjectsActions
{
    public event Action<float> OnValueChanged;
    private float currentValue;

    private void OnValueChange(float value)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                GLSEventFloatFXCommand.SetValue(HoveredObject.EventData as BaseFxEventFloat, value);
            }
        }
        else
        {
            NotifyValueChanged(value);
        }
    }
    
    public void OnValuen100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(-1f);
    }

    public void OnValuen50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(-.5f);
    }

    public void OnValue0(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(0f);
    }

    public void OnValue50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(.5f);
    }

    public void OnValue100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChange(1f);
    }

    public void OnValueHover(InputAction.CallbackContext context)
    {
        // Unity hover containers need explicit null checks before resolving their inner event.
        var evt = IsHovering && HoveredObject != null
            ? HoveredObject.EventData as BaseFxEventFloat
            : null;
        GLSEventHoverMutation.AdjustFloatFx(context, evt, ScrollPrecisionController);
    }

    // Use the explicit Ctrl+Alt action because the Alt-only value action is suppressed by more-specific chords.
    public void OnTweakEasingHover(InputAction.CallbackContext context)
    {
        // Unity hover containers need explicit null checks before resolving their inner event.
        var evt = IsHovering && HoveredObject != null
            ? HoveredObject.EventData as BaseFxEventFloat
            : null;
        GLSEventHoverMutation.AdjustFloatFxEasing(context, evt);
    }

    public void NotifyValueChanged(float value)
    {
        // Retain placement-restored state until an inactive GLS view subscribes during Start.
        currentValue = value;
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }

    // Replay the last provider notification for a GLS view that initialized after map loading.
    public void RefreshViews() => OnValueChanged?.Invoke(currentValue);
}
