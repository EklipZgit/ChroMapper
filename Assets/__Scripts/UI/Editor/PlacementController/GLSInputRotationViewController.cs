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
        // Replay the placement owner's cached values after this inactive tab view has subscribed.
        inputController.RefreshViews();
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

    // Cache editor metadata values so delayed CMUI initialization cannot repaint the rotation controls to zero.
    public void ApplyEditorState(float rotation, int loop, int direction)
    {
        valueInputField.SetValueWithoutNotify(rotation);
        loopInputField.SetValueWithoutNotify(loop);
        counterClockwiseToggle.SetValueWithoutNotify(direction == (int)LightRotationDirection.CounterClockwise);
        automaticToggle.SetValueWithoutNotify(direction == (int)LightRotationDirection.Automatic);
        clockwiseToggle.SetValueWithoutNotify(direction == (int)LightRotationDirection.Clockwise);
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
