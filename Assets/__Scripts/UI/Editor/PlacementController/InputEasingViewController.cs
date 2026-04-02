using UnityEngine;

public class InputEasingViewController : ToggleableViewController
{
    [SerializeField] private BeatmapEasingsSelectionInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ToggleComponent extensionToggle;

    public void Start()
    {
        inputController.OnExtensionChanged += HandleExtensionChanged;
        extensionToggle.OnValueChanged(HandleExtensionInputChanged);
    }

    public void OnDestroy() => inputController.OnEasingChanged -= HandleExtensionChanged;

    private void HandleExtensionInputChanged(bool value) => inputController.NotifyExtensionChanged(value ? 1 : 0);
    private void HandleExtensionChanged(int value) => extensionToggle.SetValueWithoutNotify(value == 1);
}
