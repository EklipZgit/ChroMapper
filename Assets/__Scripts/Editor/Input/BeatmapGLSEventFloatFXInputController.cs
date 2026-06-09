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
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseFxEventFloat;
            var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
            var prec = ScrollPrecisionController.GetCurrentFloatFXPrecision() / 100f;
            var value = Mathf.Round((evt.Value + (delta * prec)) * 1_000f) / 1_000f;
            GLSEventFloatFXCommand.SetValue(evt, value);
        }
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }
}
