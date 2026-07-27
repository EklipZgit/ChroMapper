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
        // Pull this view's values after its controls have completed their own initialization.
        EditorStateService.OnMapDataLoaded += LoadEditorState;
        LoadEditorState();
    }

    public void OnDestroy()
    {
        EditorStateService.OnMapDataLoaded -= LoadEditorState;
        inputController.OnValueChanged -= HandleValueChanged;
    }

    // Restore only this view's rendered controls from the saved inner GLS FloatFX node.
    private void LoadEditorState()
    {
        var data = EditorStateService.GetState("floatFxEvent");
        if (data != null)
        {
            ApplyEditorState(data["value"].AsFloat);
        }
    }

    private void HandleValueChanged(float value) => valueInputField.SetValueWithoutNotify(value * 100f);
    private void HandleValueInputChanged(float value) => inputController.NotifyValueChanged(value / 100f);

    // Cache editor metadata values so delayed CMUI initialization cannot repaint the FloatFX control to zero.
    public void ApplyEditorState(float value)
    {
        valueInputField.SetValueAndCacheWithoutNotify(value * 100f);
    }
}
