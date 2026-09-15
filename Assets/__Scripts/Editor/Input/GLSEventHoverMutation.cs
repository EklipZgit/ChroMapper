using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSEventHoverMutation
{
    // Keep inner and outer GLS hover mutations identical while each controller owns target resolution.
    public static void AdjustColorBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        // GLSColorEasingInputTest: OneModifier cannot exclude extra keys, so the Alt+Shift and Ctrl+Alt+Shift
        // chords must not also adjust brightness until Unity ships stricter composites.
        if (!context.performed
            || evt == null
            || Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.shiftKey.isPressed)
        {
            return;
        }

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.Brightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetBrightness(evt, Mathf.Max(0f, value));
    }

    public static void AdjustColorFrequency(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        // GLSColorEasingInputTest: TwoModifiers cannot exclude extra keys, so the Ctrl+Alt+Shift strobe brightness
        // chord must not also adjust frequency until Unity ships stricter composites.
        if (!context.performed
            || evt == null
            || precision == null
            || Keyboard.current.shiftKey.isPressed)
        {
            return;
        }


        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (delta == 0)
            return;

        // customData.strobeInterval is a period in beats per cycle; use the ring zoom precision ladder for tweaks.
        if (evt.ChromaStrobeInterval is { } interval)
        {
            var newInterval = Mathf.Round((interval - (delta * GetStrobeIntervalChromaStep(precision))) * 1000f) / 1000f;
            // Do not allow a zero or negative interval; keep a floor so 1/interval remains finite.
            if (newInterval <= 0f)
                newInterval = 0.01f;
            if (newInterval <= 0.5f && delta == 1)
            {
                // If we scrolled strobe interval lower and we're at 1/2 or below, swap back to OEM fractions.
                GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, null);
            }
            else
            {
                GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, newInterval);
            }


            return;
        }

        // Native frequency is cycles per beat, displayed as 1/N.
        var newFrequency = evt.Frequency + delta;
        if (newFrequency < 0)
            newFrequency = 0;
        if (evt.Frequency == 0 && delta == -1)
        {
            // Scrolling past 1/1 switches to the custom float interval starting at 1.0 beats per cycle.
            GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, 1.0f);
        }
        else
        {
            GLSEventColorCommand.SetStrobeFrequencyOnly(evt, newFrequency);
        }
    }

    public static void AdjustColorStrobeBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.StrobeBrightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetStrobeBrightness(evt, Mathf.Max(0f, value));
    }

    // GLSColorEasingInputTest: Shift+scroll cycles off -> native fade -> authored customData.strobeEasing curves.
    // OneModifier cannot exclude extra keys, so Ctrl/Alt still suppress this chord until Unity ships stricter composites.
    public static bool CycleColorStrobeFade(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed
            || evt == null
            || Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.altKey.isPressed)
        {
            return false;
        }

        // GlsEasingCycleMatchesEditorOrder treats the absent key as the native InOutCubic slot while Linear remains an authored override.
        var current = evt.StrobeFade == 1
            ? evt.ChromaStrobeEasing ?? (int)EaseType.InOutCubic
            : (int)EaseType.None;
        var next = GetNextEasingValue(current, context, StrobeFadeEasingValues);
        if (next == (int)EaseType.None)
        {
            return GLSEventColorCommand.SetStrobeFadeEasing(evt, 0, null) != null;
        }

        return GLSEventColorCommand.SetStrobeFadeEasing(
            evt,
            1,
            next == (int)EaseType.InOutCubic ? null : next) != null;
    }

    // GLSColorEasingInputTest: Ctrl+Shift+scroll cycles None -> Linear -> authored customData.colorEasing curves;
    // the Alt guard keeps the Ctrl+Alt+Shift strobe brightness chord from toggling transitions.
    public static void AdjustColorEasing(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed || evt == null || Keyboard.current.altKey.isPressed)
        {
            return;
        }

        var current = evt.ChromaColorEasing ?? evt.Easing;
        var next = GetNextEasingValue(current, context, AllEasingValues);
        if (next <= (int)EaseType.Linear)
        {
            // The None/Linear slots own the native transition; customData.colorEasing is removed so OEM wins.
            GLSEventColorCommand.SetColorEasing(evt, next, null);
        }
        else
        {
            // Custom slots keep an authored interval easing and only promote Instant so the curve has a span.
            GLSEventColorCommand.SetColorEasing(
                evt,
                Math.Max(evt.Easing, (int)EaseType.Linear),
                next);
        }
    }

    // GLSColorEasingInputTest: the Alt+Shift+scroll chord owns the strobe track's customData.strobeColorEasing cycle;
    // the Ctrl guard keeps the Ctrl+Alt+Shift strobe brightness chord unambiguous.
    public static void AdjustStrobeColorEasing(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed || evt == null || Keyboard.current.ctrlKey.isPressed)
        {
            return;
        }

        // GlsEasingCycleMatchesEditorOrder keeps absent strobeColorEasing as None because Linear=0 is a
        // meaningful override: the unset track follows the interval easing, while Linear forces its own curve.
        var current = evt.ChromaStrobeColorEasing ?? (int)EaseType.None;
        var next = GetNextEasingValue(current, context, AllEasingValues);
        GLSEventColorCommand.SetStrobeColorEasing(evt, next == (int)EaseType.None ? null : next);
    }

    public static void MirrorColor(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (context.performed && evt != null) GLSEventColorCommand.SetColor(evt, (evt.Color + 1) % 2);
    }

    // GLSEasingTypeRibbonInputTest: alt+scroll on a color ribbon toggles the transition owner's
    // customData.easingType between absent RGB and authored HSV; Ctrl/Shift own the other ribbon chords.
    public static void CycleColorLerpType(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed
            || evt == null
            || Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.shiftKey.isPressed)
        {
            return;
        }

        GLSEventColorCommand.SetLerpType(evt, evt.CustomLerpType == "HSV" ? null : "HSV");
    }

    public static void AdjustRotation(InputAction.CallbackContext context, BaseLightRotationBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Rotation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * precision.GetCurrentRotationPrecision())) * 1_000f) / 1_000f;
        GLSEventRotationCommand.SetValue(evt, Mathf.Repeat(value, 360f));
    }

    public static void AdjustRotationLoop(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        GLSEventRotationCommand.SetLoop(evt, (evt.Loop + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + 5) % 5);
    }

    public static void AdjustRotationEasing(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.EaseType, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
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
        // Adjusting a YEETed node un-yeets it before adjusting. Otherwise you'd just be invisibly messing with your un-yeet value while just showing YEET the whole time.
        if (GLSEventTranslationCommand.IsYeet(evt.Translation))
        {
            GLSEventTranslationCommand.SetValue(evt, evt.Translation + GLSEventTranslationCommand.YeetOffset);
            return;
        }

        var value = Mathf.Round((evt.Translation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentTranslationPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventTranslationCommand.SetValue(evt, value);
    }

    public static void ToggleTranslationYeet(InputAction.CallbackContext context, BaseLightTranslationBase evt)
    {
        if (!context.performed || evt == null)
        {
            return;
        }

        GLSEventTranslationCommand.ToggleYeet(evt);
    }

    public static void AdjustTranslationEasing(InputAction.CallbackContext context, BaseLightTranslationBase evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.EaseType, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
    }

    public static void AdjustFloatFx(InputAction.CallbackContext context, BaseFxEventFloat evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Value + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentFloatFXPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventFloatFXCommand.SetValue(evt, value);
    }

    public static void AdjustFloatFxEasing(InputAction.CallbackContext context, BaseFxEventFloat evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.Easing, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
    }

    // Match the ring zoom precision ladder from the Basic Event zoom tweaks.
    private static float GetStrobeIntervalChromaStep(ScrollPrecisionController precision)
        => precision.CurrentPrecision switch
        {
            ScrollPrecision.Low => 1f,
            ScrollPrecision.Medium => 0.25f,
            ScrollPrecision.High => 0.05f,
            _ => 0.01f
        };

    // GlsEasingCycleMatchesEditorOrder follows the official editor's supported order and puts each custom true-InOut immediately after its Beat Saber IO counterpart.
    private static readonly EaseType[] EditorAndTrueInOutEasingValues =
    {
        EaseType.None,
        EaseType.Linear,
        EaseType.InQuadratic,
        EaseType.OutQuadratic,
        EaseType.InOutQuadratic,
        EaseType.InCircular,
        EaseType.OutCircular,
        EaseType.InOutCircular,
        EaseType.InBack,
        EaseType.OutBack,
        EaseType.BeatSaberInOutBack,
        EaseType.InOutBack,
        EaseType.InElastic,
        EaseType.OutElastic,
        EaseType.BeatSaberInOutElastic,
        EaseType.InOutElastic,
        EaseType.InBounce,
        EaseType.OutBounce,
        EaseType.BeatSaberInOutBounce,
        EaseType.InOutBounce
    };

    // GlsEasingCycleMatchesEditorOrder appends every remaining known curve in enum order so new easings cannot be silently omitted.
    private static readonly EaseType[] AllEasingValues = EditorAndTrueInOutEasingValues
        .Concat(((EaseType[])Enum.GetValues(typeof(EaseType)))
            .Where(v => !EditorAndTrueInOutEasingValues.Contains(v)))
        .ToArray();

    // GlsEasingCycleMatchesEditorOrder gives strobe its required IOCr-first branch before the shared order resumes at Linear.
    private static readonly EaseType[] StrobeFadeEasingValues = new[]
        {
            EaseType.None,
            EaseType.InOutCircular
        }
        .Concat(AllEasingValues.Where(v => v != EaseType.None && v != EaseType.InOutCircular))
        .ToArray();

    // All GLS node types cycle the same ordered easing list so inner and outer hover controls remain consistent.
    private static int GetNextEasing(int currentEasing, InputAction.CallbackContext context) =>
        GetNextEasingValue(currentEasing, context, AllEasingValues);

    private static int GetNextEasingValue(
        int currentEasing,
        InputAction.CallbackContext context,
        EaseType[] values)
    {
        var index = Array.IndexOf(values, (EaseType)currentEasing);
        return (int)values[((index < 0 ? 0 : index) + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + values.Length) % values.Length];
    }
}
