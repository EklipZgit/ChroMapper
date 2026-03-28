using System.Globalization;
using TMPro;
using UnityEngine;

public class GLSInputRotationViewController : ToggleableViewController
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;

    [Header("Input Components")] [SerializeField]
    private TMP_InputField valueInputField;

    [SerializeField] private TMP_InputField loopInputField;

    public void Start()
    {
        inputController.OnValueChanged += HandleValueChanged;
        valueInputField.onValueChanged.AddListener(HandleValueInputChanged);
        inputController.OnLoopChanged += HandleLoopChanged;
        loopInputField.onValueChanged.AddListener(HandleLoopInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        valueInputField.onValueChanged.RemoveListener(HandleValueInputChanged);
        inputController.OnLoopChanged -= HandleLoopChanged;
        loopInputField.onValueChanged.RemoveListener(HandleLoopInputChanged);
    }

    private void HandleValueChanged(float value) =>
        valueInputField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));

    private void HandleValueInputChanged(string value)
    {
        if (float.TryParse(value, out var val)) inputController.NotifyValueChanged(Mathf.Repeat(val, 360f));
    }

    private void HandleLoopChanged(int value) =>
        loopInputField.SetTextWithoutNotify(value.ToString(CultureInfo.InvariantCulture));

    private void HandleLoopInputChanged(string value)
    {
        if (int.TryParse(value, out var val)) inputController.NotifyValueChanged(val);
    }
}
