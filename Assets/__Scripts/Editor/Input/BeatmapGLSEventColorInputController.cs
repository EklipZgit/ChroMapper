using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapGLSEventColorInputController : BeatmapGLSGroupInputController<BaseLightColorEventBoxGroup>,
                                                   CMInput.IGLSColorObjectsActions
{
    public event Action<int> OnColorChanged;
    public event Action<bool> OnExtensionPerformed;
    public event Action<float> OnBrightnessChanged;
    public event Action<float> OnBrightnessDeltaChanged;
    public event Action<int> OnEasingChanged;
    public event Action<int> OnStrobeChanged;
    public event Action<int> OnSoftStrobeChanged;
    public event Action<float> OnStrobeFrequencyChanged;
    public event Action<float> OnStrobeFrequencyDeltaChanged;
    public event Action<float> OnStrobeBrightnessChanged;
    public event Action<float> OnStrobeBrightnessDeltaChanged;

    public void OnColor0Light(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnColorChanged?.Invoke((int)LightColor.Red);
        }
    }

    public void OnColor1Light(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnColorChanged?.Invoke((int)LightColor.Blue);
        }
    }

    public void OnColorWLight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnColorChanged?.Invoke((int)LightColor.White);
        }
    }

    public void OnExtensionLight(InputAction.CallbackContext context)
    {
        if (context.performed) OnExtensionPerformed?.Invoke(true);
    }

    public void OnStatic0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.None);
            OnBrightnessChanged?.Invoke(0f);
        }
    }

    public void OnStatic50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.None);
            OnBrightnessChanged?.Invoke(.5f);
        }
    }

    public void OnStatic100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.None);
            OnBrightnessChanged?.Invoke(1f);
        }
    }

    public void OnFade0Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.Linear);
            OnBrightnessChanged?.Invoke(0f);
        }
    }

    public void OnFade50Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.Linear);
            OnBrightnessChanged?.Invoke(.5f);
        }
    }

    public void OnFade100Brightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            OnExtensionPerformed?.Invoke(false);
            OnEasingChanged?.Invoke((int)EaseType.Linear);
            OnBrightnessChanged?.Invoke(1f);
        }
    }

    public void OnSetBrightness0(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(0f);
    }

    public void OnSetBrightness10(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.1f);
    }

    public void OnSetBrightness20(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.2f);
    }

    public void OnSetBrightness30(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.3f);
    }

    public void OnSetBrightness40(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.4f);
    }

    public void OnSetBrightness50(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.5f);
    }

    public void OnSetBrightness60(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.6f);
    }

    public void OnSetBrightness70(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.7f);
    }

    public void OnSetBrightness80(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.8f);
    }

    public void OnSetBrightness90(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(.9f);
    }

    public void OnSetBrightness100(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(1f);
    }

    public void OnSetBrightness120(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(1.2f);
    }

    public void OnSetBrightness150(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessChanged?.Invoke(1.5f);
    }

    public void OnStrobeOn(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobeChanged?.Invoke(1);
    }

    public void OnStrobeOff(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobeChanged?.Invoke(0);
    }

    public void OnChangeStrobeFrequencyPrecise(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobeFrequencyDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }

    private int strobeBrightnessCycle = 0;
    private float[] strobeBrightness = { 0f, 0.5f, 1f };

    public void OnChangeStrobeBrightness(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            strobeBrightnessCycle++;
            strobeBrightnessCycle %= strobeBrightness.Length;
            OnStrobeBrightnessChanged?.Invoke(strobeBrightness[strobeBrightnessCycle]);
        }
    }

    public void OnChangeStrobeBrightnessPrecise(InputAction.CallbackContext context)
    {
        if (context.performed) OnStrobeBrightnessDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }

    public void OnSoftStrobe(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnChangeEventColor(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnSetBrightnessPrecise(InputAction.CallbackContext context)
    {
        if (context.performed) OnBrightnessDeltaChanged?.Invoke(context.ReadValue<Vector2>().y);
    }
}
