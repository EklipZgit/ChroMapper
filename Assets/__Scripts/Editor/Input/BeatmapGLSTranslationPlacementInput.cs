using System;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSTranslationPlacementInput : BeatmapInputController<GLSGroupContainer>, CMInput.IGLSTranslationPlacementActions
{
    public event Action<float> OnValueChanged;
    public event Action<float> OnValueDeltaChanged;

    public void OnSetValuen100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChanged?.Invoke(-1f);
    }

    public void OnSetValuen50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChanged?.Invoke(-.5f);
    }

    public void OnSetValue0(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChanged?.Invoke(0f);
    }

    public void OnSetValue50(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChanged?.Invoke(.5f);
    }

    public void OnSetValue100(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueChanged?.Invoke(1f);
    }

    public void OnSetValuePrecise(InputAction.CallbackContext context)
    {
        if (context.performed) OnValueDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }
}
