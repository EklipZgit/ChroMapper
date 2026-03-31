using System;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public class BeatmapEasingsSelectionInputController : MonoBehaviour, CMInput.IEasingsSelectionActions
{
    public event Action<int> OnEasingChanged;
    public event Action<int> OnExtensionChanged;

    private EaseType currentEase;

    private int easeCurve = 0;

    private int easeStandardIdx = 0;

    private EaseType[] easeStandard =
    {
        EaseType.InQuadratic,
        EaseType.OutQuadratic,
        EaseType.InOutQuadratic,
        EaseType.InCircular,
        EaseType.OutCircular,
        EaseType.InOutCircular
    };

    private int easeAlternativeIdx = 0;

    private EaseType[] easeAlternative =
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

    public void OnEasingCurve(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnEasingCurveHover(InputAction.CallbackContext context)
    {
        if (context.performed
            && GlobalIntersectionCache.HasHit
            && GlobalIntersectionCache.FirstHit.TryGetComponent<GLSEventContainer>(
                out var container)
            && container.ObjectData is BaseGLSEvent evt)
        {
            throw new NotImplementedException();
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
        if (context.performed
            && GlobalIntersectionCache.HasHit
            && GlobalIntersectionCache.FirstHit.TryGetComponent<GLSEventContainer>(
                out var container)
            && container.ObjectData is BaseGLSEvent evt)
            GLSEventEasingsCommand.SetEasing(evt, (int)EaseType.None);
    }

    public void OnEasingStandard(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnEasingStandardHover(InputAction.CallbackContext context)
    {
        if (context.performed
            && GlobalIntersectionCache.HasHit
            && GlobalIntersectionCache.FirstHit.TryGetComponent<GLSEventContainer>(
                out var container)
            && container.ObjectData is BaseGLSEvent evt)
        {
            throw new NotImplementedException();
        }
    }

    public void OnEasingAlternative(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            throw new NotImplementedException();
        }
    }

    public void OnEasingAlternativeHover(InputAction.CallbackContext context)
    {
        if (context.performed
            && GlobalIntersectionCache.HasHit
            && GlobalIntersectionCache.FirstHit.TryGetComponent<GLSEventContainer>(
                out var container)
            && container.ObjectData is BaseGLSEvent evt)
        {
            throw new NotImplementedException();
        }
    }

    public void NotifyEasingChanged(EaseType ease)
    {
        NotifyExtensionChanged(0);
        OnEasingChanged?.Invoke((int)ease);
    }

    private int extension;

    public void OnExtension(InputAction.CallbackContext context)
    {
        if (context.performed) NotifyExtensionChanged(++extension % 2);
    }

    public void OnExtensionHover(InputAction.CallbackContext context)
    {
        if (context.performed
            && GlobalIntersectionCache.HasHit
            && GlobalIntersectionCache.FirstHit.TryGetComponent<GLSEventContainer>(
                out var container)
            && container.ObjectData is BaseGLSEvent evt)
            GLSEventEasingsCommand.SetExtension(evt, extension);
    }

    public void NotifyExtensionChanged(int value)
    {
        extension = value;
        OnExtensionChanged?.Invoke(extension);
    }
}
