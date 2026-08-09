using UnityEngine;

public class GLSInputFloatFXViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent valueInputField;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentFloatFXPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleValueInputChanged);
        // Replay the placement owner's cached value after this inactive tab view has subscribed.
        inputController.RefreshViews();
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
    }


    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value * 100f);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(value / 100f);

    // Cache editor metadata values so delayed CMUI initialization cannot repaint the FloatFX control to zero.
    public void ApplyEditorState(float value)
    {
        valueInputField.SetValueWithoutNotify(value * 100f);
    }
}
