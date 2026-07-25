using Beatmap.Enums;
using UnityEngine;

public class GLSInputRotationViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent valueInputField;
    [SerializeField] private TextBoxIntComponent loopInputField;

    [SerializeField] private ToggleComponent counterClockwiseToggle;
    [SerializeField] private ToggleComponent automaticToggle;
    [SerializeField] private ToggleComponent clockwiseToggle;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentRotationPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleValueInputChanged)
            .OnValueChanged(HandleValueInputChanged);

        inputController.OnLoopChanged += HandleLoopChanged;
        loopInputField
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnEndEdit(HandleLoopInputChanged)
            .OnValueChanged(HandleLoopInputChanged);

        inputController.OnDirectionChanged += HandleDirectionChanged;
        counterClockwiseToggle.OnValueChanged(HandleCounterClockwiseToggleInputChanged);
        automaticToggle.OnValueChanged(HandleAutomaticToggleInputChanged);
        clockwiseToggle.OnValueChanged(HandleClockwiseToggleInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        inputController.OnLoopChanged -= HandleLoopChanged;
        inputController.OnDirectionChanged -= HandleDirectionChanged;
    }

    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(Mathf.Repeat(value, 360f));

    private void HandleLoopChanged(int value) => loopInputField.SetValueWithoutNotify(value);
    private void HandleLoopInputChanged(int value) => inputController.NotifyLoopChanged(value);

    // Cache CmData values so delayed CMUI initialization cannot repaint the rotation controls to zero.
    public void ApplyCmDataState(float rotation, int loop, int direction)
    {
        valueInputField.SetValueAndCacheWithoutNotify(rotation);
        loopInputField.SetValueAndCacheWithoutNotify(loop);
        counterClockwiseToggle.SetValueAndCacheWithoutNotify(direction == (int)LightRotationDirection.CounterClockwise);
        automaticToggle.SetValueAndCacheWithoutNotify(direction == (int)LightRotationDirection.Automatic);
        clockwiseToggle.SetValueAndCacheWithoutNotify(direction == (int)LightRotationDirection.Clockwise);
        Debug.Log($"[CmData] Applied rotation view '{name}': rotation={rotation}, loop={loop}, direction={direction}.");
    }

    private void HandleDirectionChanged(int value)
    {
        counterClockwiseToggle.SetValueWithoutNotify(value == (int)LightRotationDirection.CounterClockwise);
        automaticToggle.SetValueWithoutNotify(value == (int)LightRotationDirection.Automatic);
        clockwiseToggle.SetValueWithoutNotify(value == (int)LightRotationDirection.Clockwise);
    }

    private void HandleCounterClockwiseToggleInputChanged(bool _) =>
        inputController.NotifyDirectionChanged((int)LightRotationDirection.CounterClockwise);

    private void HandleAutomaticToggleInputChanged(bool _) =>
        inputController.NotifyDirectionChanged((int)LightRotationDirection.Automatic);

    private void HandleClockwiseToggleInputChanged(bool _) =>
        inputController.NotifyDirectionChanged((int)LightRotationDirection.Clockwise);
}
