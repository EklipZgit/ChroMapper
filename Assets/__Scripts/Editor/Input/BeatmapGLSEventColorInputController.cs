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
    public event Action<int> OnStrobeFrequencyChanged;
    public event Action<float> OnStrobeBrightnessChanged;
    public event Action<int> OnSoftStrobeChanged;

    public void OnColor0Light(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.Red);
    }

    public void OnColor0LightHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventColorCommand.SetColor(HoveredObject.EventData as BaseLightColorBase, (int)LightColor.Red);
    }

    public void OnColor1Light(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.Blue);
    }

    public void OnColor1LightHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventColorCommand.SetColor(HoveredObject.EventData as BaseLightColorBase, (int)LightColor.Blue);
    }

    public void OnColorWLight(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyColorChanged(LightColor.White);
    }

    public void OnColorWLightHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventColorCommand.SetColor(HoveredObject.EventData as BaseLightColorBase, (int)LightColor.White);
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

    public void OnStatic0BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                0f,
                EaseType.None);
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

    public void OnStatic50BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                .5f,
                EaseType.None);
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

    public void OnStatic100BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                1f,
                EaseType.None);
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

    public void OnFade0BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                0f,
                EaseType.Linear);
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

    public void OnFade50BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                .5f,
                EaseType.Linear);
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

    public void OnFade100BrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventColorCommand.SetBrightnessAndEasing(
                HoveredObject.EventData as BaseLightColorBase,
                1f,
                EaseType.Linear);
        }
    }

    public void OnBrightness0(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(0f);
    }

    public void OnBrightness10(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.1f);
    }

    public void OnBrightness20(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.2f);
    }

    public void OnBrightness30(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.3f);
    }

    public void OnBrightness40(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.4f);
    }

    public void OnBrightness50(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.5f);
    }

    public void OnBrightness60(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.6f);
    }

    public void OnBrightness70(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.7f);
    }

    public void OnBrightness80(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.8f);
    }

    public void OnBrightness90(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(.9f);
    }

    public void OnBrightness100(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1f);
    }

    public void OnBrightness120(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1.2f);
    }

    public void OnBrightness150(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyBrightnessChanged(1.5f);
    }

    public void OnBrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightColorBase;
            var delta = Mathf.Sign(context.ReadValue<float>());
            GLSEventColorCommand.SetBrightness(evt, Mathf.Max(0f, evt.Brightness + (delta * .1f)));
        }
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

    public void OnStrobeOnHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventColorCommand.SetStrobeFade(HoveredObject.EventData as BaseLightColorBase, 1);
    }

    public void OnStrobeOff(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyStrobeFrequencyChanged(0);
    }

    public void OnStrobeOffHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
            GLSEventColorCommand.SetStrobeFade(HoveredObject.EventData as BaseLightColorBase, 0);
    }

    public void OnStrobeFrequencyHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightColorBase;
            var delta = Mathf.Sign(context.ReadValue<float>());
            GLSEventColorCommand.SetFrequency(evt, (int)Mathf.Max(0f, evt.Frequency + delta));
        }
    }

    public void NotifyStrobeFrequencyChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeFrequencyChanged?.Invoke(value);
    }

    private int strobeBrightnessCycle = 0;
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

    public void OnStrobeBrightnessHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightColorBase;
            var delta = Mathf.Sign(context.ReadValue<float>());
            GLSEventColorCommand.SetStrobeBrightness(evt, Mathf.Max(0f, evt.StrobeBrightness + (delta * .1f)));
        }
    }

    public void NotifyStrobeBrightnessChanged(float value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnStrobeBrightnessChanged?.Invoke(value);
    }

    public void OnSoftStrobe(InputAction.CallbackContext context)
    {
        if (context.performed) NotifySoftStrobeChanged(0);
    }

    public void OnMirrorHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var evt = HoveredObject.EventData as BaseLightColorBase;
            GLSEventColorCommand.SetColor(evt, (evt.Color + 1) % 2);
        }
    }

    public void NotifySoftStrobeChanged(int value)
    {
        EasingInputController.NotifyExtensionChanged(0);
        OnSoftStrobeChanged?.Invoke(value);
    }
}
