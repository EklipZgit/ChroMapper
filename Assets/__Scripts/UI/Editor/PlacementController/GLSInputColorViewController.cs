using System.Globalization;
using Beatmap.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GLSInputColorViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    [Header("Input Components")] [SerializeField]
    private TMP_InputField brightnessInputField;

    [SerializeField] private TMP_InputField strobeBrightnessInputField;
    [SerializeField] private TMP_InputField strobeFrequencyInputField;
    [SerializeField] private Toggle fadeToggle;
    [SerializeField] private Toggle strobeFadeToggle;

    public void Start()
    {
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        brightnessInputField.onValueChanged.AddListener(HandleBrightnessInputChanged);
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        strobeBrightnessInputField.onValueChanged.AddListener(HandleStrobeBrightnessInputChanged);
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        strobeFrequencyInputField.onValueChanged.AddListener(HandleStrobeFrequencyInputChanged);
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        fadeToggle.onValueChanged.AddListener(HandleFadeInputChanged);
        easingInputController.OnEasingChanged += HandleEasingChanged;
        strobeFadeToggle.onValueChanged.AddListener(HandleStrobeFadeInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        brightnessInputField.onValueChanged.RemoveListener(HandleBrightnessInputChanged);
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        strobeBrightnessInputField.onValueChanged.RemoveListener(HandleStrobeBrightnessInputChanged);
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        strobeFrequencyInputField.onValueChanged.RemoveListener(HandleStrobeFrequencyInputChanged);
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        fadeToggle.onValueChanged.RemoveListener(HandleFadeInputChanged);
        easingInputController.OnEasingChanged -= HandleEasingChanged;
        strobeFadeToggle.onValueChanged.RemoveListener(HandleStrobeFadeInputChanged);
    }

    // TODO: turns out it's not needed but just in case i'll leave it here atm
    private void HandleColorChanged(int value)
    {
        // QueuedData.Color = value;
    }

    private void HandleBrightnessChanged(float value) =>
        brightnessInputField.SetTextWithoutNotify((value * 100f).ToString(CultureInfo.InvariantCulture));

    private void HandleBrightnessInputChanged(string value)
    {
        if (float.TryParse(value, out var val)) inputController.NotifyBrightnessChanged(val / 100f);
    }

    private void HandleStrobeBrightnessChanged(float value) =>
        strobeBrightnessInputField.SetTextWithoutNotify((value * 100f).ToString(CultureInfo.InvariantCulture));

    private void HandleStrobeBrightnessInputChanged(string value)
    {
        if (float.TryParse(value, out var val)) inputController.NotifyStrobeBrightnessChanged(val / 100f);
    }

    private void HandleStrobeFrequencyChanged(int value) =>
        strobeFrequencyInputField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));

    private void HandleStrobeFrequencyInputChanged(string value)
    {
        if (int.TryParse(value, out var val)) inputController.NotifyStrobeFrequencyChanged(val);
    }

    private void HandleSoftStrobeChanged(int value) => strobeFadeToggle.SetIsOnWithoutNotify(value == 1);

    private void HandleStrobeFadeInputChanged(bool value) => inputController.NotifySoftStrobeChanged(value ? 1 : 0);

    private void HandleEasingChanged(int value) => fadeToggle.SetIsOnWithoutNotify(value >= 0);

    private void HandleFadeInputChanged(bool value) =>
        easingInputController.NotifyEasingChanged(value ? EaseType.Linear : EaseType.None);
}
