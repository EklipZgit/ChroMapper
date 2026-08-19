using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;

public class InputEasingViewController : ToggleableViewController, IEditorStateProvider
{
    [SerializeField] private BeatmapEasingsSelectionInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ToggleComponent extensionToggle;

    [SerializeField] private ToggleComponent curveInToggle;
    [SerializeField] private ToggleComponent curveOutToggle;
    [SerializeField] private ToggleComponent curveInOutToggle;

    [SerializeField] private ToggleComponent easeNoneToggle;
    [SerializeField] private ToggleComponent easeLinearToggle;
    [SerializeField] private ToggleComponent easeQuadToggle;
    [SerializeField] private ToggleComponent easeCircularToggle;
    [SerializeField] private ToggleComponent easeBounceToggle;
    [SerializeField] private ToggleComponent easeBackToggle;
    [SerializeField] private ToggleComponent easeElasticToggle;

    public void Start()
    {
        inputController.OnExtensionChanged += HandleExtensionChanged;
        inputController.OnEasingChanged += HandleEasingChanged;

        extensionToggle.OnValueChanged(HandleExtensionInputChanged);
        // Attach to the toggle's selectable because it receives pointer hover events, and read the binding at display time after remaps.
        AddExtensionTooltip();

        curveInToggle.OnValueChanged(HandleCurveInInputChanged);
        curveOutToggle.OnValueChanged(HandleCurveOutInputChanged);
        curveInOutToggle.OnValueChanged(HandleCurveInOutInputChanged);

        easeNoneToggle.OnValueChanged(HandleEaseNoneInputChanged);
        easeLinearToggle.OnValueChanged(HandleEaseLinearInputChanged);
        easeQuadToggle.OnValueChanged(HandleEaseQuadInputChanged);
        easeCircularToggle.OnValueChanged(HandleEaseCircularInputChanged);
        easeBounceToggle.OnValueChanged(HandleEaseBounceInputChanged);
        easeBackToggle.OnValueChanged(HandleEaseBackInputChanged);
        easeElasticToggle.OnValueChanged(HandleEaseElasticInputChanged);

        // Restore the visible easing menu only after all of its toggle callbacks are attached.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnExtensionChanged -= HandleExtensionChanged;
        inputController.OnEasingChanged -= HandleEasingChanged;
    }

    // Let this view own the selected easing menu state rather than a deferred global restore.
    public string StateKey => "easingMenu";

    // Store the menu values exposed by its input controller at save time.
    public void CaptureEditorState(JSONObject data)
    {
        data["easing"] = inputController.CurrentEasing;
        data["extension"] = inputController.CurrentExtension;
    }

    // Apply this menu's cached values when metadata becomes available after Start.
    public void LoadEditorState(JSONNode data)
    {
        // Keep controller defaults for fields absent from older editor-state documents.
        var easing = data.HasKey("easing")
            ? data["easing"].AsInt
            : inputController.CurrentEasing;
        var extension = data.HasKey("extension")
            ? data["extension"].AsInt
            : inputController.CurrentExtension;
        inputController.RestoreMenuState(easing, extension);
        ApplyEditorState(easing, extension);
    }

    private void HandleExtensionInputChanged(bool value) => inputController.NotifyExtensionChanged(value ? 1 : 0);
    private void HandleExtensionChanged(int value) => extensionToggle.SetValueWithoutNotify(value == 1);

    private void AddExtensionTooltip()
    {
        var tooltipTarget = extensionToggle.Selectable != null
            ? extensionToggle.Selectable.gameObject
            : extensionToggle.gameObject;
        var tooltip = tooltipTarget.GetComponent<Tooltip>() ?? tooltipTarget.AddComponent<Tooltip>();
        // TODO: Localize this tooltip before Stable so the new remappable hint follows the rest of the UI.
        tooltip.TooltipOverride = "Extend the previous light event";
        tooltip.AdvancedTooltip = "Extend the previous light event";
        tooltip.AppearDelay = 0.25f;
        tooltip.HotkeyActionMap = "Easings Selection";
        tooltip.HotkeyActionName = "Extension";
        tooltip.HotkeyDisplayPrefix = "Press ";
    }

    // Apply editor metadata directly to the rendered toggles after map loading, bypassing input-event timing.
    public void ApplyEditorState(int easing, int extension)
    {
        HandleEasingChanged(easing);
        // Cache the CMUI value too, otherwise ToggleComponent.Start redraws its default false state after load.
        extensionToggle.SetValueWithoutNotify(extension == 1);
    }

    // lol, lmao even
    private void HandleEasingChanged(int value)
    {
        curveInToggle.SetValueWithoutNotify(false);
        curveOutToggle.SetValueWithoutNotify(false);
        curveInOutToggle.SetValueWithoutNotify(false);

        easeNoneToggle.SetValueWithoutNotify(false);
        easeLinearToggle.SetValueWithoutNotify(false);
        easeQuadToggle.SetValueWithoutNotify(false);
        easeCircularToggle.SetValueWithoutNotify(false);
        easeBounceToggle.SetValueWithoutNotify(false);
        easeBackToggle.SetValueWithoutNotify(false);
        easeElasticToggle.SetValueWithoutNotify(false);

        var easeString = ((EaseType)value).ToString();

        if (easeString.StartsWith("InOut"))
            curveInOutToggle.SetValueWithoutNotify(true);
        else if (easeString.StartsWith("Out"))
            curveOutToggle.SetValueWithoutNotify(true);
        else
            curveInToggle.SetValueWithoutNotify(true);

        switch (value)
        {
            case (int)EaseType.None:
                easeNoneToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.Linear:
                easeLinearToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.InQuadratic:
            case (int)EaseType.OutQuadratic:
            case (int)EaseType.InOutQuadratic:
                easeQuadToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.InCircular:
            case (int)EaseType.OutCircular:
            case (int)EaseType.InOutCircular:
                easeCircularToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.InBounce:
            case (int)EaseType.OutBounce:
            case (int)EaseType.InOutBounce:
                easeBounceToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.InBack:
            case (int)EaseType.OutBack:
            case (int)EaseType.InOutBack:
                easeBackToggle.SetValueWithoutNotify(true);
                break;
            case (int)EaseType.InElastic:
            case (int)EaseType.OutElastic:
            case (int)EaseType.InOutElastic:
                easeElasticToggle.SetValueWithoutNotify(true);
                break;
        }
    }

    private void HandleCurveInInputChanged(bool _)
    {
        if (easeQuadToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InQuadratic);
        else if (easeCircularToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InCircular);
        else if (easeBounceToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InBounce);
        else if (easeBackToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InBack);
        else if (easeElasticToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InElastic);
        else if (easeNoneToggle.Value)
            inputController.NotifyEasingChanged(EaseType.None);
        else
            inputController.NotifyEasingChanged(EaseType.Linear);
    }

    private void HandleCurveOutInputChanged(bool _)
    {
        if (easeQuadToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutQuadratic);
        else if (easeCircularToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutCircular);
        else if (easeBounceToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutBounce);
        else if (easeBackToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutBack);
        else if (easeElasticToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutElastic);
        else if (easeNoneToggle.Value)
            inputController.NotifyEasingChanged(EaseType.None);
        else
            inputController.NotifyEasingChanged(EaseType.Linear);
    }

    private void HandleCurveInOutInputChanged(bool _)
    {
        if (easeQuadToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutQuadratic);
        else if (easeCircularToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutCircular);
        else if (easeBounceToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutBounce);
        else if (easeBackToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutBack);
        else if (easeElasticToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutElastic);
        else if (easeNoneToggle.Value)
            inputController.NotifyEasingChanged(EaseType.None);
        else
            inputController.NotifyEasingChanged(EaseType.Linear);
    }

    private void HandleEaseNoneInputChanged(bool obj) => inputController.NotifyEasingChanged(EaseType.None);

    private void HandleEaseLinearInputChanged(bool obj) => inputController.NotifyEasingChanged(EaseType.Linear);

    private void HandleEaseQuadInputChanged(bool obj)
    {
        if (curveOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutQuadratic);
        else if (curveInOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutQuadratic);
        else
            inputController.NotifyEasingChanged(EaseType.InQuadratic);
    }

    private void HandleEaseCircularInputChanged(bool obj)
    {
        if (curveOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutCircular);
        else if (curveInOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutCircular);
        else
            inputController.NotifyEasingChanged(EaseType.InCircular);
    }

    private void HandleEaseBounceInputChanged(bool obj)
    {
        if (curveOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutBounce);
        else if (curveInOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutBounce);
        else
            inputController.NotifyEasingChanged(EaseType.InBounce);
    }

    private void HandleEaseBackInputChanged(bool obj)
    {
        if (curveOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutBack);
        else if (curveInOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutBack);
        else
            inputController.NotifyEasingChanged(EaseType.InBack);
    }

    private void HandleEaseElasticInputChanged(bool obj)
    {
        if (curveOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.OutElastic);
        else if (curveInOutToggle.Value)
            inputController.NotifyEasingChanged(EaseType.InOutElastic);
        else
            inputController.NotifyEasingChanged(EaseType.InElastic);
    }
}
