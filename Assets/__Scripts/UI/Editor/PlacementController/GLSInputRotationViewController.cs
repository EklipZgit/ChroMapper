using UnityEngine;

public class GLSInputRotationViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent valueInputField;
    [SerializeField] private TextBoxIntComponent loopInputField;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentRotationPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleValueInputChanged);
        inputController.OnLoopChanged += HandleLoopChanged;
        loopInputField.OnValueChanged(HandleLoopInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        inputController.OnLoopChanged -= HandleLoopChanged;
    }

    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(Mathf.Repeat(value, 360f));
    private void HandleLoopChanged(int value) => loopInputField.SetValueWithoutNotify(value);
    private void HandleLoopInputChanged(int value) => inputController.NotifyValueChanged(value);
}
