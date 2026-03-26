using Beatmap.Base;
using UnityEngine;

public class GLSEventColorPlacement : GLSEventPlacement<BaseLightColorEventBoxGroup, BaseLightColorBase>
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnExtensionPerformed += HandleExtensionsPerformed;
        inputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnExtensionPerformed -= HandleExtensionsPerformed;
        inputController.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Brightness = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleColorChanged(int value)
    {
        QueuedData.Color = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionsPerformed(bool value)
    {
        QueuedData.UsePrevious = value ? 1 : 0;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.UsePrevious = 0;
        QueuedData.Easing = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorBase GenerateOriginalData() => new();
}
