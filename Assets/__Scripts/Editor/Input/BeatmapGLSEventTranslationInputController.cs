using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventTranslationInputController : BeatmapGLSEventInputController<BaseLightTranslationBase>,
                                                         CMInput.IGLSTranslationObjectsActions
{
    public event Action<float> OnValueChanged;

    private void OnValueChange(float value)
    {
        if (KeybindsController.IsHoverKeyHeld)
        {
            if (IsHovering)
            {
                GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, value);
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
        var evt = IsHovering ? HoveredObject?.EventData as BaseLightTranslationBase : null;
        GLSEventHoverMutation.AdjustTranslation(context, evt, ScrollPrecisionController);
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }
}
