using System;
using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventTranslationInputController : BeatmapGLSEventInputController<BaseLightTranslationBase>,
                                                         CMInput.IGLSTranslationObjectsActions
{
    public event Action<float> OnValueChanged;

    public void OnSetValuen100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(-1f);
    }

    public void OnSetValuen50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(-.5f);
    }

    public void OnSetValue0(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(0f);
    }

    public void OnSetValue50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(.5f);
    }

    public void OnSetValue100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyValueChanged(1f);
    }

    public void NotifyValueChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnValueChanged?.Invoke(value);
    }

    public void OnSetValuePrecise(InputAction.CallbackContext context)
    {
        // if (context.performed) OnValueDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }
}
