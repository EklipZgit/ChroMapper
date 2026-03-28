using System.Globalization;
using TMPro;
using UnityEngine;

public class GLSInputFloatFXViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;

    [Header("Input Components")] [SerializeField]
    private TMP_InputField valueInputField;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField.onValueChanged.AddListener(HandleValueInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        valueInputField.onValueChanged.RemoveListener(HandleValueInputChanged);
    }

    private void HandleValueChanged(float value) =>
        valueInputField.SetTextWithoutNotify((value * 100f).ToString(CultureInfo.InvariantCulture));

    private void HandleValueInputChanged(string value)
    {
        if (float.TryParse(value, out var val)) inputController.NotifyValueChanged(val / 100f);
    }
}
