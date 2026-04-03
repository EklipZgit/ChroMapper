using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class GLSEventColorPlacement : GLSEventPlacement<BaseLightColorEventBoxGroup, BaseLightColorBase>
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnFadeChanged += HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
    }

    public void OnDestroy()
    {
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnFadeChanged -= HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    private void HandleColorChanged(int value)
    {
        QueuedData.Color = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Brightness = Mathf.Max(value, 0f);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleStrobeFrequencyChanged(int value)
    {
        QueuedData.Frequency = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleStrobeBrightnessChanged(float value)
    {
        QueuedData.StrobeBrightness = Mathf.Max(value, 0f);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleSoftStrobeChanged(int value)
    {
        QueuedData.StrobeFade = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Easing = (int)(value >= 0 ? EaseType.Linear : EaseType.None);
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionChanged(int value)
    {
        QueuedData.UsePrevious = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorBase GenerateOriginalData() => new();
}
