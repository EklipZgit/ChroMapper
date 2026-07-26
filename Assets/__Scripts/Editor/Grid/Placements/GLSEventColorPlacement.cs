using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public class GLSEventColorPlacement : GLSEventPlacement<BaseLightColorEventBoxGroup, BaseLightColorBase>
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;
    [SerializeField] private ColorPicker colorPicker;

    private int selectedColor;

    public override void Start()
    {
        base.Start();
        // Unity objects require their overloaded null comparison when resolving cross-prefab scene references.
        if (colorPicker == null)
            colorPicker = ColourPicker.ActivePicker;
        if (colorPicker == null)
            colorPicker = FindObjectOfType<ColorPicker>();
        selectedColor = QueuedData.Color;
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnFadeChanged += HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        ColorTypeController.OnColorChanged += HandleColorChanged;
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
        ColorTypeController.OnColorChanged -= HandleColorChanged;
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        base.HandlePlacementToData(inputState);
        QueuedData.Color = selectedColor;
        // Extension nodes inherit color from the previous node and must not carry independent Chroma RGB data.
        if (QueuedData.UsePrevious == 0 && EventPlacement.CanPlaceChromaEvents && colorPicker != null)
        {
            QueuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            QueuedData.CustomColor = null;
        }
        // Apply the independent strobe picker only to non-extension GLS color nodes.
        QueuedData.StrobeColor = QueuedData.UsePrevious == 0 && StrobeColorPickerController.Instance is { IsEnabled: true } strobePicker
            ? strobePicker.CurrentColor
            : null;
        QueuedData.SaveCustom();
        if (PlacementVisualContainer != null) GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleColorChanged(int value)
    {
        selectedColor = value;
        QueuedData.Color = selectedColor;
        // Extension nodes inherit color from the previous node and must not carry independent Chroma RGB data.
        if (QueuedData.UsePrevious == 0 && EventPlacement.CanPlaceChromaEvents && colorPicker != null)
        {
            QueuedData.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            QueuedData.CustomColor = null;
        }
        // Keep the queued strobe override aligned when the primary GLS color changes.
        QueuedData.StrobeColor = QueuedData.UsePrevious == 0 && StrobeColorPickerController.Instance is { IsEnabled: true } strobePicker
            ? strobePicker.CurrentColor
            : null;
        QueuedData.SaveCustom();
        if (PlacementVisualContainer != null) GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
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
        // Remove both custom color channels immediately when this node becomes an extension node.
        if (value != 0)
        {
            QueuedData.CustomColor = null;
            QueuedData.StrobeColor = null;
            QueuedData.SaveCustom();
        }
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorBase GenerateOriginalData() => new();
}
