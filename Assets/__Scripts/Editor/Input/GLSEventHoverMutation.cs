using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSEventHoverMutation
{
    // Keep inner and outer GLS hover mutations identical while each controller owns target resolution.
    public static void AdjustColorBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.Brightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetBrightness(evt, Mathf.Max(0f, value));
    }

    public static void AdjustColorFrequency(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed || evt == null) return;
        GLSEventColorCommand.SetFrequency(evt, (int)Mathf.Max(0f, evt.Frequency + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue)));
    }

    public static void AdjustColorStrobeBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.StrobeBrightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetStrobeBrightness(evt, Mathf.Max(0f, value));
    }

    public static void MirrorColor(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (context.performed && evt != null) GLSEventColorCommand.SetColor(evt, (evt.Color + 1) % 2);
    }

    public static void AdjustRotation(InputAction.CallbackContext context, BaseLightRotationBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null || Keyboard.current == null || Keyboard.current.ctrlKey.isPressed || Keyboard.current.shiftKey.isPressed) return;
        var value = Mathf.Round((evt.Rotation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * precision.GetCurrentRotationPrecision())) * 1_000f) / 1_000f;
        GLSEventRotationCommand.SetValue(evt, Mathf.Repeat(value, 360f));
    }

    public static void AdjustRotationLoop(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null || Keyboard.current == null || Keyboard.current.shiftKey.isPressed) return;
        GLSEventRotationCommand.SetLoop(evt, (evt.Loop + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + 5) % 5);
    }

    public static void AdjustRotationEasing(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        var values = (EaseType[])Enum.GetValues(typeof(EaseType));
        var index = Array.IndexOf(values, (EaseType)evt.EaseType);
        GLSEventRotationCommand.SetEaseType(evt, (int)values[((index < 0 ? 0 : index) + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + values.Length) % values.Length]);
    }

    public static void CycleRotationDirection(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        var values = (LightRotationDirection[])Enum.GetValues(typeof(LightRotationDirection));
        var index = Array.IndexOf(values, (LightRotationDirection)evt.Direction);
        GLSEventRotationCommand.SetDirection(evt, values[((index < 0 ? 0 : index) + 1) % values.Length]);
    }

    public static void AdjustTranslation(InputAction.CallbackContext context, BaseLightTranslationBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Translation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentTranslationPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventTranslationCommand.SetValue(evt, value);
    }

    public static void AdjustFloatFx(InputAction.CallbackContext context, BaseFxEventFloat evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Value + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentFloatFXPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventFloatFXCommand.SetValue(evt, value);
    }
}
