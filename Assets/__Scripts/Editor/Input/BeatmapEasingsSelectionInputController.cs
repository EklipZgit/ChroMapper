using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapEasingsSelectionInputController : BeatmapInputController<GLSEventContainer>,
                                                      CMInput.IEasingsSelectionActions
{
    public event Action<int> OnEasingChanged;
    public event Action<int> OnExtensionChanged;

    private EaseType currentEase;
    private EaseCurve currentCurve;

    private static readonly List<EaseType> easeStandard = new()
    {
        EaseType.InQuadratic,
        EaseType.OutQuadratic,
        EaseType.InOutQuadratic,
        EaseType.InCircular,
        EaseType.OutCircular,
        EaseType.InOutCircular
    };

    private static readonly List<EaseType> easeAlternative = new()
    {
        EaseType.InBounce,
        EaseType.OutBounce,
        EaseType.InOutBounce,
        EaseType.InBack,
        EaseType.OutBack,
        EaseType.InOutBack,
        EaseType.InElastic,
        EaseType.OutElastic,
        EaseType.InOutElastic
    };

    // you're about to witness bizarre
    public void OnEasingCurve(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentEase is EaseType.None or EaseType.Linear) return;

            currentCurve = GetEaseCurve(currentEase);
            currentEase -= (int)currentCurve;
            currentCurve = (EaseCurve)(((int)currentCurve + 1) % 3);
            currentEase += (int)currentCurve;
            NotifyEasingChanged(currentEase);
        }
    }

    public void OnEasingCurveHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var ease = (EaseType)(HoveredObject.EventData switch
            {
                BaseLightColorBase lcb => lcb.Easing,
                BaseLightRotationBase lrb => lrb.EaseType,
                BaseLightTranslationBase ltb => ltb.EaseType,
                BaseFxEventFloat fx => fx.Easing,
                _ => 0
            });

            if (ease is EaseType.None or EaseType.Linear) return;

            var easeCurve = GetEaseCurve(ease);
            if (easeCurve != currentCurve)
                ease = ease - (int)easeCurve + (int)currentCurve;
            else
            {
                ease -= (int)currentCurve;
                currentCurve = (EaseCurve)(((int)currentCurve + 1) % 3);
                ease += (int)currentCurve;
            }

            GLSEventEasingCommand.SetEasing(HoveredObject.EventData, (int)ease);
            NotifyEasingChanged(ease);
        }
    }

    public void OnEasingNone(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            currentEase = EaseType.None;
            NotifyEasingChanged(currentEase);
        }
    }

    public void OnEasingNoneHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            GLSEventEasingCommand.SetEasing(HoveredObject.EventData, (int)EaseType.None);
            NotifyEasingChanged(EaseType.None);
        }
    }

    public void OnEasingStandard(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentEase == EaseType.Linear)
                currentEase = EaseType.InQuadratic + (int)currentCurve;
            else if (currentEase == EaseType.None)
                currentEase = EaseType.None;
            else if (easeStandard.Contains(currentEase))
            {
                var idx = easeStandard.IndexOf(currentEase) + 3;
                currentEase = idx >= easeStandard.Count ? easeStandard[idx] : EaseType.Linear;
            }
            else
                currentEase = easeStandard[(int)currentCurve];

            NotifyEasingChanged(currentEase);
        }
    }

    public void OnEasingStandardHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var ease = (EaseType)(HoveredObject.EventData switch
            {
                BaseLightColorBase lcb => lcb.Easing,
                BaseLightRotationBase lrb => lrb.EaseType,
                BaseLightTranslationBase ltb => ltb.EaseType,
                BaseFxEventFloat fx => fx.Easing,
                _ => 0
            });

            var easeCurve = (int)(ease is EaseType.Linear or EaseType.None ? currentCurve : GetEaseCurve(ease));
            if (ease == EaseType.Linear)
                ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.InQuadratic + easeCurve;
            else if (ease == EaseType.None)
                ease = easeStandard.Contains(currentEase) ? currentEase : EaseType.Linear;
            else if (!IsSameEaseType(ease, currentEase) && easeStandard.Contains(currentEase))
                ease = currentEase - (int)GetEaseCurve(currentEase) + easeCurve;
            else if (easeStandard.Contains(ease))
            {
                ease -= easeCurve;
                var idx = easeStandard.IndexOf(ease) + 3;
                if (idx >= easeStandard.Count)
                    ease = EaseType.Linear;
                else
                {
                    ease = easeStandard[idx];
                    ease += easeCurve;
                }
            }
            else
                ease = easeStandard[easeCurve];

            GLSEventEasingCommand.SetEasing(HoveredObject.EventData, (int)ease);
            NotifyEasingChanged(ease);
        }
    }

    public void OnEasingAlternative(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            currentEase = easeAlternative.Contains(currentEase)
                ? easeAlternative[(easeAlternative.IndexOf(currentEase) + 3) % easeAlternative.Count]
                : easeAlternative[(int)currentCurve];
            NotifyEasingChanged(currentEase);
        }
    }

    public void OnEasingAlternativeHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var ease = (EaseType)(HoveredObject.EventData switch
            {
                BaseLightColorBase lcb => lcb.Easing,
                BaseLightRotationBase lrb => lrb.EaseType,
                BaseLightTranslationBase ltb => ltb.EaseType,
                BaseFxEventFloat fx => fx.Easing,
                _ => 0
            });

            var easeCurve = (int)GetEaseCurve(ease);
            if (IsSameEaseType(ease, currentEase) && easeAlternative.Contains(ease))
            {
                ease -= easeCurve;
                ease = easeAlternative[(easeAlternative.IndexOf(ease) + 3) % easeAlternative.Count];
                ease += easeCurve;
            }
            else if (!IsSameEaseType(ease, currentEase) && easeAlternative.Contains(currentEase))
                ease = currentEase - (int)GetEaseCurve(currentEase) + easeCurve;
            else
                ease = easeAlternative[(int)currentCurve];

            GLSEventEasingCommand.SetEasing(HoveredObject.EventData, (int)ease);
            NotifyEasingChanged(ease);
        }
    }

    public void NotifyEasingChanged(EaseType value)
    {
        NotifyExtensionChanged(0);
        if (currentEase == value) return;
        currentEase = value;
        if (value is not EaseType.Linear and not EaseType.None) currentCurve = GetEaseCurve(value);
        OnEasingChanged?.Invoke((int)value);
    }

    private int extension;

    public void OnExtension(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyExtensionChanged(++extension % 2);
    }

    public void OnExtensionHover(InputAction.CallbackContext context)
    {
        if (context.performed && IsHovering)
        {
            var e = HoveredObject.EventData switch
            {
                BaseLightColorBase lcb => lcb.UsePrevious,
                BaseLightRotationBase lrb => lrb.UsePrevious,
                BaseLightTranslationBase ltb => ltb.UsePrevious,
                BaseFxEventFloat fx => fx.UsePrevious,
                _ => 0
            };

            GLSEventEasingCommand.SetExtension(HoveredObject.EventData, (e + 1) % 2);
        }
    }

    public void NotifyExtensionChanged(int value)
    {
        if (extension == value) return;
        extension = value;
        OnExtensionChanged?.Invoke(extension);
    }

    // TODO: these are gigahorrible, but easy way out
    private static bool IsSameEaseType(EaseType a, EaseType b) => a - (int)GetEaseCurve(a) == b - (int)GetEaseCurve(b);

    private static EaseCurve GetEaseCurve(EaseType ease)
    {
        var easeCurve = ease.ToString();
        if (easeCurve.StartsWith("InOut")) return EaseCurve.InOut;
        if (easeCurve.StartsWith("Out")) return EaseCurve.Out;
        return EaseCurve.In;
    }
}
