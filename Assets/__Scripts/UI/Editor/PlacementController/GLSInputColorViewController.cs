// GLS shift controls consume the shared compact-string codec owned by the beatmap color model.
using Beatmap.Base;
using UnityEngine;

public class GLSInputColorViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent brightnessInputField;
    [SerializeField] private TextBoxFloatComponent strobeBrightnessInputField;
    [SerializeField] private TextBoxIntComponent strobeFrequencyInputField;
    [SerializeField] private ToggleComponent fadeToggle;
    [SerializeField] private ToggleComponent strobeFadeToggle;
    // Serialized event-scope rows keep layout ownership in the Unity scene and prevent narrow runtime clones from corrupting the existing color-options row.
    [SerializeField] private TextBoxComponent shiftsInputField;
    [SerializeField] private TextBoxComponent strobeShiftsInputField;

    public void Start()
    {
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnFadeChanged += HandleEasingChanged;
        brightnessInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleBrightnessInputChanged);
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        strobeBrightnessInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentBrightnessPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleStrobeBrightnessInputChanged);
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        strobeFrequencyInputField.OnValueChanged(HandleStrobeFrequencyInputChanged);
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        inputController.OnShiftsChanged += HandleShiftsChanged;
        inputController.OnStrobeShiftsChanged += HandleStrobeShiftsChanged;
        fadeToggle.OnValueChanged(HandleFadeInputChanged);
        easingInputController.OnEasingChanged += HandleEasingChanged;
        strobeFadeToggle.OnValueChanged(HandleStrobeFadeInputChanged);
        // Bind the scene-authored controls without restoring the prefab labels that the vertical event layout intentionally replaces with separate label rows.
        ConfigureShiftInput(shiftsInputField, false);
        ConfigureShiftInput(strobeShiftsInputField, true);
        // Replay the placement owner's cached values after this inactive tab view has subscribed.
        inputController.RefreshViews();
    }

    public void OnDestroy()
    {
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnFadeChanged -= HandleEasingChanged;
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        inputController.OnShiftsChanged -= HandleShiftsChanged;
        inputController.OnStrobeShiftsChanged -= HandleStrobeShiftsChanged;
        easingInputController.OnEasingChanged -= HandleEasingChanged;
    }


    // TODO: turns out it's not needed but just in case i'll leave it here atm
    private void HandleColorChanged(int value)
    {
        // QueuedData.Color = value;
    }

    // Cache replayed placement values so delayed CMUI initialization cannot repaint these controls with prefab defaults.
    private void HandleBrightnessChanged(float value) => brightnessInputField.SetValueWithoutNotify(value * 100f);

    private void HandleBrightnessInputChanged(float value) => inputController.NotifyBrightnessChanged(value / 100f);

    private void HandleStrobeBrightnessChanged(float value) =>
        strobeBrightnessInputField.SetValueWithoutNotify(value * 100f);

    private void HandleStrobeBrightnessInputChanged(float value) =>
        inputController.NotifyStrobeBrightnessChanged(value / 100f);

    private void HandleStrobeFrequencyChanged(int value) => strobeFrequencyInputField.SetValueWithoutNotify(value);

    private void HandleStrobeFrequencyInputChanged(int value) => inputController.NotifyStrobeFrequencyChanged(value);

    private void HandleSoftStrobeChanged(int value) => strobeFadeToggle.SetValueWithoutNotify(value == 1);

    private void HandleStrobeFadeInputChanged(bool value) => inputController.NotifySoftStrobeChanged(value ? 1 : 0);

    private void HandleShiftsChanged(string[] value)
    {
        if (shiftsInputField != null)
        {
            shiftsInputField.SetValueWithoutNotify(GLSColorShift.ToEditorText(value));
        }
    }

    private void HandleStrobeShiftsChanged(string[] value)
    {
        if (strobeShiftsInputField != null)
        {
            strobeShiftsInputField.SetValueWithoutNotify(GLSColorShift.ToEditorText(value));
        }
    }

    private void HandleEasingChanged(int value) => fadeToggle.SetValueWithoutNotify(value >= 0);

    // Cache every GLS color control so opening its tab cannot repaint saved values with component defaults.
    public void ApplyEditorState(
        float brightness,
        float strobeBrightness,
        int strobeFrequency,
        int easing,
        int strobeFade,
        string[] shifts = null,
        string[] strobeShifts = null)
    {
        brightnessInputField.SetValueWithoutNotify(brightness * 100f);
        strobeBrightnessInputField.SetValueWithoutNotify(strobeBrightness * 100f);
        strobeFrequencyInputField.SetValueWithoutNotify(strobeFrequency);
        // Cache the CMUI values too, otherwise ToggleComponent.Start redraws its default false state after load.
        fadeToggle.SetValueWithoutNotify(easing >= 0);
        strobeFadeToggle.SetValueWithoutNotify(strobeFade == 1);
        if (shiftsInputField != null)
        {
            shiftsInputField.SetValueWithoutNotify(GLSColorShift.ToEditorText(shifts));
        }
        if (strobeShiftsInputField != null)
        {
            strobeShiftsInputField.SetValueWithoutNotify(GLSColorShift.ToEditorText(strobeShifts));
        }
    }

    // Fade must notify the GLS color owner directly because generic easing suppresses an unchanged cached Linear value.
    private void HandleFadeInputChanged(bool value) => inputController.NotifyFadeChanged(value ? 0 : -1);

    // Semicolon-separated compact instructions map one-to-one to the JSON string arrays; the tooltip documents f composition and mode B behavior at the point of editing.
    private void ConfigureShiftInput(TextBoxComponent input, bool strobe)
    {
        // The event-level scene owns standalone labels, so configuring only the input avoids dereferencing the removed prefab label container.
        input.WithMaximumLength(1024)
            .OnEndEdit(value => inputController.NotifyShiftsChanged(GLSColorShift.FromEditorText(value), strobe));
        // Reuse the Tooltip already supplied by the CMUI prefab so the scene does not accumulate duplicate pointer handlers.
        var tooltip = input.GetComponent<Tooltip>();
        tooltip.enabled = true;
        tooltip.TooltipOverride = "Separate {targets},{signedOffset},{easing} entries with semicolons.";
        tooltip.AdvancedTooltip =
            "Targets: hsv or rgb (first model wins), plus independent f. Easing is spatial across affected chunks only (mode B), from zero on the first chunk to the full offset on the last.";
        tooltip.AppearDelay = 0.25f;
    }
}
