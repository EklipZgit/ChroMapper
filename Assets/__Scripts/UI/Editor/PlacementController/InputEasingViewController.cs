using UnityEngine;
using UnityEngine.UI;

public class InputEasingViewController : ToggleableViewController
{
    [SerializeField] private BeatmapEasingsSelectionInputController inputController;

    [Header("Input Components")] [SerializeField]
    private Toggle extensionToggle;

    public void Start()
    {
        inputController.OnExtensionChanged += HandleExtensionChanged;
        extensionToggle.onValueChanged.AddListener(HandleExtensionInputChanged);
    }

    public void OnDestroy()
    {
        inputController.OnEasingChanged -= HandleExtensionChanged;
        extensionToggle.onValueChanged.RemoveListener(HandleExtensionInputChanged);
    }

    private void HandleExtensionInputChanged(bool value) => inputController.NotifyExtensionChanged(value ? 1 : 0);
    private void HandleExtensionChanged(int value) => extensionToggle.SetIsOnWithoutNotify(value == 1);
}
