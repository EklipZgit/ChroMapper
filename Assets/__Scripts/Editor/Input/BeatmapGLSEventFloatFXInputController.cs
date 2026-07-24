using System;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventFloatFXInputController : BeatmapGLSEventInputController<BaseFxEventFloat>,
                                                     CMInput.IGLSFloatFXObjectsActions
{
    public event Action<float> OnValueChanged;

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
        var evt = IsHovering ? HoveredObject?.EventData as BaseFxEventFloat : null;
        GLSEventHoverMutation.AdjustFloatFx(context, evt, ScrollPrecisionController);
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }
}
