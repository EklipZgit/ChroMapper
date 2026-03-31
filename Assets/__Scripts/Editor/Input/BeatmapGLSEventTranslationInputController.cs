using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventTranslationInputController : BeatmapGLSEventInputController<BaseLightTranslationBase>,
                                                         CMInput.IGLSTranslationObjectsActions
{
    public event Action<float> OnValueChanged;

    public void OnValuen100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(-1f);
    }

    public void OnValuen100Hover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, -1f);
    }

    public void OnValuen50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(-.5f);
    }

    public void OnValuen50Hover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, -.5f);
    }

    public void OnValue0(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(0f);
    }

    public void OnValue0Hover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, 0f);
    }

    public void OnValue50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(.5f);
    }

    public void OnValue50Hover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, .5f);
    }

    public void OnValue100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(1f);
    }

    public void OnValue100Hover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventTranslationCommand.SetValue(HoveredObject.EventData as BaseLightTranslationBase, 1f);
    }

    public void OnValueHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightTranslationBase;
            var delta = Mathf.Sign(context.ReadValue<float>());
            GLSEventTranslationCommand.SetValue(evt, evt.Translation + (delta * 0.1f));
        }
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }
}
