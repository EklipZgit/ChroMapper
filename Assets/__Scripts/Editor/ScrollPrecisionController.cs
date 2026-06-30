using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// we use list here simply for the fact that we could possibly extend it
public class ScrollPrecisionController : MonoBehaviour, CMInput.IScrollPrecisionActions
{
    public event Action<ScrollPrecision> OnPrecisionChanged;

    public const int MaxPrecision = 4;
    [SerializeField] private ScrollPrecision currentPrecision = ScrollPrecision.Medium;

    public ScrollPrecision CurrentPrecision
    {
        get => currentPrecision;
        set
        {
            if (currentPrecision == value) return;
            currentPrecision = value;
            OnPrecisionChanged?.Invoke(currentPrecision);
        }
    }

    public List<float> BrightnessPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> RotationPrecision = new(MaxPrecision) { 1f, 2.5f, 15f, 30f };
    public List<float> TranslationPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> FloatFXPrecision = new(MaxPrecision) { 1f, 2.5f, 10f, 100f };
    public List<float> AngleOffsetPrecision = new(MaxPrecision) { 1f, 2f, 5f, 15f };
    public List<float> TimePrecision = new(MaxPrecision) { 0.01f, 0.1f, 0.25f, 1f };
    public List<float> PercentPrecision = new(MaxPrecision) { 1f, 5f, 10f, 50f };
    public List<float> MultiplierPrecision = new(MaxPrecision) { 0.01f, 0.025f, 0.1f, 0.5f };

    public float GetCurrentBrightnessPrecision() => BrightnessPrecision[(int)CurrentPrecision];
    public float GetCurrentRotationPrecision() => RotationPrecision[(int)CurrentPrecision];
    public float GetCurrentTranslationPrecision() => TranslationPrecision[(int)CurrentPrecision];
    public float GetCurrentFloatFXPrecision() => FloatFXPrecision[(int)CurrentPrecision];
    public float GetCurrentAngleOffsetPrecision() => AngleOffsetPrecision[(int)CurrentPrecision];
    public float GetCurrentTimePrecision() => TimePrecision[(int)CurrentPrecision];
    public float GetCurrentPercentPrecision() => PercentPrecision[(int)CurrentPrecision];
    public float GetCurrentMultiplierPrecision() => MultiplierPrecision[(int)CurrentPrecision];

    public void OnScroll(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertPrecisionScroll);
        CurrentPrecision = (ScrollPrecision)Math.Clamp((byte)CurrentPrecision - delta, 0, MaxPrecision - 1);
    }
}

public enum ScrollPrecision : byte
{
    Ultra,
    High,
    Medium,
    Low
}
