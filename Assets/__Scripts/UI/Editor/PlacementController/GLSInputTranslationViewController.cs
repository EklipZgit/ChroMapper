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

    // Cache CmData values so delayed CMUI initialization cannot repaint the translation control to zero.
    public void ApplyCmDataState(float translation)
    {
        valueInputField.SetValueAndCacheWithoutNotify(translation * 100f);
        Debug.Log($"[CmData] Applied translation view '{name}': translation={translation}.");
    }
}
