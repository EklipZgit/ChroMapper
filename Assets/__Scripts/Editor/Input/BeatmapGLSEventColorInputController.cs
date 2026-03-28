using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine.InputSystem;

public class BeatmapGLSEventColorInputController : BeatmapGLSEventInputController<BaseLightColorBase>,
                                                   CMInput.IGLSColorObjectsActions
{
    public event Action<int> OnColorChanged;
    public event Action<float> OnBrightnessChanged;
    public event Action<int> OnStrobeFrequencyChanged;
    public event Action<float> OnStrobeBrightnessChanged;
    public event Action<int> OnSoftStrobeChanged;

    public void OnColor0Light(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.Red);
    }

    public void OnColor1Light(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.Blue);
    }

    public void OnColorWLight(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.White);
    }

    public void NotifyColorChanged(LightColor color)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnColorChanged?.Invoke((int)color);
    }

    public void OnStatic0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.None);
            NotifyBrightnessChanged(0f);
        }
    }

    public void OnStatic50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.None);
            NotifyBrightnessChanged(.5f);
        }
    }

    public void OnStatic100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.None);
            NotifyBrightnessChanged(1f);
        }
    }

    public void OnFade0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.Linear);
            NotifyBrightnessChanged(0f);
        }
    }

    public void OnFade50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.Linear);
            NotifyBrightnessChanged(.5f);
        }
    }

    public void OnFade100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            EasingInputController.NotifyEasingChanged(EaseType.Linear);
            NotifyBrightnessChanged(1f);
        }
    }

    public void OnSetBrightness0(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(0f);
    }

    public void OnSetBrightness10(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.1f);
    }

    public void OnSetBrightness20(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.2f);
    }

    public void OnSetBrightness30(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.3f);
    }

    public void OnSetBrightness40(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.4f);
    }

    public void OnSetBrightness50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.5f);
    }

    public void OnSetBrightness60(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.6f);
    }

    public void OnSetBrightness70(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.7f);
    }

    public void OnSetBrightness80(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.8f);
    }

    public void OnSetBrightness90(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.9f);
    }

    public void OnSetBrightness100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1f);
    }

    public void OnSetBrightness120(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1.2f);
    }

    public void OnSetBrightness150(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1.5f);
    }

    public void OnSetBrightnessPrecise(InputAction.CallbackContext context)
    {
        // if (context.performed) OnBrightnessDeltaChanged?.Invoke(Mathf.Sign(context.ReadValue<Vector2>().y));
    }

    public void NotifyBrightnessChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnBrightnessChanged?.Invoke(value);
    }

    public void OnStrobeOn(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyStrobeFrequencyChanged(1);
    }

    public void OnStrobeOff(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyStrobeFrequencyChanged(0);
    }

    public void NotifyStrobeFrequencyChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeFrequencyChanged?.Invoke(value);
    }

    public void OnChangeStrobeFrequencyPrecise(InputAction.CallbackContext context)
    {
        // if (context.performed) OnStrobeFrequencyDeltaChanged?.Invoke(Mathf.Sign(context.ReadValue<Vector2>().y));
    }

    private int strobeBrightnessCycle = 0;
    private float[] strobeBrightness = { 0f, 0.5f, 1f };

    public void OnChangeStrobeBrightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            strobeBrightnessCycle++;
            strobeBrightnessCycle %= strobeBrightness.Length;
            NotifyStrobeBrightnessChanged(strobeBrightness[strobeBrightnessCycle]);
        }
    }

    public void NotifyStrobeBrightnessChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeBrightnessChanged?.Invoke(value);
    }

    public void OnChangeStrobeBrightnessPrecise(InputAction.CallbackContext context)
    {
        // if (context.performed) OnStrobeBrightnessDeltaChanged?.Invoke(Mathf.Sign(context.ReadValue<Vector2>().y));
    }

    public void OnSoftStrobe(InputAction.CallbackContext context)
    {
        if (context.performed) NotifySoftStrobeChanged(0);
    }

    public void NotifySoftStrobeChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnSoftStrobeChanged?.Invoke(value);
    }

    public void OnChangeEventColor(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }
}
