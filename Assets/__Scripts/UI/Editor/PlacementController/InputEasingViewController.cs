using System;
using System.Linq;
using Beatmap.Enums;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class InputEasingViewController : ToggleableViewController, IEditorStateProvider
{
    [SerializeField] private BeatmapEasingsSelectionInputController inputController;

    [Header("Input Components")] [SerializeField]
    private ToggleComponent extensionToggle;

    [SerializeField] private DropdownComponent easingDropdown;
    [SerializeField] private DropdownComponent curveDropdown;

    // The column root owns whole-panel visibility. The two easing rows and the extension row own
    // their own context visibility through the inherited show/extend targets.
    [SerializeField] private GameObject controlsPanel;

    // The menu order is curated, so each entry names the easing emitted for the family's "In"
    // direction while None, Linear, and the Catmull-Rom modes stay directionless and keep
    // their own ids. Never derive an easing id from the dropdown index.
    private static readonly EaseType[] familyOrder =
    {
        EaseType.None,
        EaseType.Linear,
        EaseType.InQuadratic,
        EaseType.InSinusoidal,
        EaseType.InCubic,
        EaseType.InQuartic,
        EaseType.InQuintic,
        EaseType.InExponential,
        EaseType.InCircular,
        EaseType.InBack,
        EaseType.InElastic,
        EaseType.InBounce,
        EaseType.CatmullRom,
        EaseType.CatmullRomCentripetal,
        EaseType.CatmullRomChordal
    };

    private static readonly string[] familyLabelKeys =
    {
        "place.gls.easings.none",
        "place.gls.easings.linear",
        "place.gls.easings.quadratic",
        "place.gls.easings.sine",
        "place.gls.easings.cubic",
        "place.gls.easings.quartic",
        "place.gls.easings.quintic",
        "place.gls.easings.exponential",
        "place.gls.easings.circular",
        "place.gls.easings.back",
        "place.gls.easings.elastic",
        "place.gls.easings.bounce",
        "place.gls.easings.catmullrom.uniform",
        "place.gls.easings.catmullrom.centripetal",
        "place.gls.easings.catmullrom.chordal"
    };

    // Direction option keys line up with EaseCurve ordinals: In, Out, InOut, BeatSaberInOut.
    private static readonly string[] curveLabelKeys =
    {
        "place.gls.easings.direction.in",
        "place.gls.easings.direction.out",
        "place.gls.easings.direction.inout",
        "place.gls.easings.direction.inout.beatsaber"
    };

    private Tooltip easingWarningTooltip;
    private EaseCurve lastCurve = EaseCurve.In;
    private bool curveOptionsIncludeBeatSaber;

    public void Start()
    {
        inputController.OnExtensionChanged += HandleExtensionChanged;
        inputController.OnEasingChanged += HandleEasingChanged;

        extensionToggle.OnValueChanged(HandleExtensionInputChanged);
        // Attach to the toggle's selectable because it receives pointer hover events, and read the binding at display time after remaps.
        AddExtensionTooltip();

        easingWarningTooltip = easingDropdown.GetComponent<Tooltip>();
        if (easingWarningTooltip != null)
        {
            easingWarningTooltip.AppearDelay = 0.25f;
        }

        easingDropdown.WithOptions(familyLabelKeys.Select(LocalizedOption));
        easingDropdown.OnValueChanged(HandleEasingOptionChanged);
        curveDropdown.WithOptions(curveLabelKeys.Take(3).Select(LocalizedOption));
        curveDropdown.OnValueChanged(HandleCurveOptionChanged);

        // Replay the input controller's live state only after the callbacks exist so delayed CMUI
        // cached values land through SetValueWithoutNotify instead of looking like user input.
        HandleEasingChanged(inputController.CurrentEasing);
        HandleExtensionChanged(inputController.CurrentExtension);

        // Restore the visible easing menu only after all of its dropdown callbacks are attached.
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
        // Restoring Extension rewrites the menu easing to Linear through placement callbacks, so
        // repaint with the controller's effective values rather than the requested document values.
        ApplyEditorState(inputController.CurrentEasing, inputController.CurrentExtension);
    }

    private static string LocalizedOption(string key) =>
        LocalizationSettings.StringDatabase.GetLocalizedString("Mapper", key);

    private void HandleExtensionInputChanged(bool value) => inputController.NotifyExtensionChanged(value ? 1 : 0);
    private void HandleExtensionChanged(int value) => extensionToggle.SetValueWithoutNotify(value == 1);

    // Color contexts keep the extension row without the easing rows, so panel visibility is the
    // union of both flags rather than a single show target.
    public void SetVisibility(bool showEasings, bool showExtension)
    {
        Show(showEasings);
        Extend(showExtension);
        controlsPanel.SetActive(showEasings || showExtension);
    }

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

    // Apply editor metadata directly to the rendered dropdowns after map loading, bypassing input-event timing.
    public void ApplyEditorState(int easing, int extension)
    {
        HandleEasingChanged(easing);
        // Cache the CMUI value too, otherwise ToggleComponent.Start redraws its default false state after load.
        extensionToggle.SetValueWithoutNotify(extension == 1);
    }

    private void HandleEasingOptionChanged(int index)
    {
        var family = familyOrder[Mathf.Clamp(index, 0, familyOrder.Length - 1)];
        if (!BeatmapEasingsSelectionInputController.HasEaseCurve(family))
        {
            inputController.NotifyEasingChanged(family);
            return;
        }

        // Families without a Beat Saber InOut variant convert the remembered direction to InOut.
        var curve = lastCurve == EaseCurve.BeatSaberInOut && !HasBeatSaberInOutVariant(family)
            ? EaseCurve.InOut
            : lastCurve;
        inputController.NotifyEasingChanged(BeatmapEasingsSelectionInputController.SetEaseCurve(family, curve));
    }

    private void HandleCurveOptionChanged(int index)
    {
        // Setting a dropdown value programmatically invokes the callback even while the row is
        // grayed out, so stale choices must never rewrite a nondirectional easing.
        if (!BeatmapEasingsSelectionInputController.HasEaseCurve((EaseType)inputController.CurrentEasing))
        {
            return;
        }

        var family = BeatmapEasingsSelectionInputController.GetEaseFamily(
            (EaseType)inputController.CurrentEasing);
        var curve = (EaseCurve)Mathf.Clamp(index, 0, (int)EaseCurve.BeatSaberInOut);
        inputController.NotifyEasingChanged(BeatmapEasingsSelectionInputController.SetEaseCurve(family, curve));
    }

    private void HandleEasingChanged(int value)
    {
        var ease = (EaseType)value;
        var family = BeatmapEasingsSelectionInputController.GetEaseFamily(ease);
        var familyIndex = Array.IndexOf(familyOrder, family);
        if (familyIndex >= 0)
        {
            easingDropdown.SetValueWithoutNotify(familyIndex);
        }

        if (!BeatmapEasingsSelectionInputController.HasEaseCurve(ease))
        {
            // Nondirectional families gray out the row without flushing the remembered direction.
            curveDropdown.Selectable.interactable = false;
        }
        else
        {
            var curve = BeatmapEasingsSelectionInputController.GetEaseCurve(ease);
            lastCurve = curve;
            var includeBeatSaber = HasBeatSaberInOutVariant(family);
            if (includeBeatSaber != curveOptionsIncludeBeatSaber)
            {
                curveOptionsIncludeBeatSaber = includeBeatSaber;
                var optionCount = includeBeatSaber ? curveLabelKeys.Length : curveLabelKeys.Length - 1;
                curveDropdown.WithOptions(curveLabelKeys.Take(optionCount).Select(LocalizedOption));
            }

            curveDropdown.Selectable.interactable = true;
            curveDropdown.SetValueWithoutNotify((int)curve);
        }

        ApplyFamilyWarning(family);
    }

    private void ApplyFamilyWarning(EaseType family)
    {
        if (easingWarningTooltip == null)
        {
            return;
        }

        var key = FamilyWarningKey(family);
        if (key == null)
        {
            easingWarningTooltip.enabled = false;
            return;
        }

        easingWarningTooltip.LocalizedTooltip = new LocalizedString
        {
            TableReference = "Mapper",
            TableEntryReference = key,
            FallbackState = FallbackBehavior.UseFallback
        };
        easingWarningTooltip.enabled = true;
    }

    private static bool HasBeatSaberInOutVariant(EaseType family) =>
        family is EaseType.InBack or EaseType.InElastic or EaseType.InBounce;

    // Only the extended ChromaGLS families and the experimental splines carry playback requirements.
    private static string FamilyWarningKey(EaseType family) => family switch
    {
        EaseType.InSinusoidal => "place.gls.easings.sine.tooltip",
        EaseType.InCubic => "place.gls.easings.cubic.tooltip",
        EaseType.InQuartic => "place.gls.easings.quartic.tooltip",
        EaseType.InQuintic => "place.gls.easings.quintic.tooltip",
        EaseType.InExponential => "place.gls.easings.exponential.tooltip",
        EaseType.CatmullRom or EaseType.CatmullRomCentripetal or EaseType.CatmullRomChordal =>
            "place.gls.easings.catmullrom.tooltip",
        _ => null
    };
}
