using System.Globalization;
using TMPro;
using UnityEngine;

public class GLSInputTranslationViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventTranslationInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ScrollPrecisionController scrollPrecisionController;

    [SerializeField] private TextBoxFloatComponent valueInputField;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField
            .WithScrollPrecision(scrollPrecisionController.GetCurrentTranslationPrecision)
            .WithInvertScroll(() => Settings.Instance.InvertScrollEventValue)
            .OnValueChanged(HandleValueInputChanged);
    }

    public void OnDestroy() => inputController.OnValueChanged -= HandleValueChanged;

    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value * 100f);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(value / 100f);
}
