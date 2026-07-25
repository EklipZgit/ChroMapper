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
    }

    public void OnDestroy() => inputController.OnValueChanged -= HandleValueChanged;

    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value * 100f);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(value / 100f);

    // Cache CmData values so delayed CMUI initialization cannot repaint the FloatFX control to zero.
    public void ApplyCmDataState(float value)
    {
        valueInputField.SetValueAndCacheWithoutNotify(value * 100f);
        Debug.Log($"[CmData] Applied FloatFX view '{name}': value={value}.");
    }
}
