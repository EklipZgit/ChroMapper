using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using SimpleJSON;
using UnityEngine;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>, IEditorStateProvider
{
    [SerializeField] private BeatmapGLSGroupColorInputController groupInputController;
    [SerializeField] private BeatmapGLSEventColorInputController eventInputController;
    [SerializeField] private ColorPicker colorPicker;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.ColorTrack && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        // Unity objects require their overloaded null comparison when resolving cross-prefab scene references.
        if (colorPicker == null)
            colorPicker = ColourPicker.ActivePicker;
        if (colorPicker == null)
            colorPicker = FindObjectOfType<ColorPicker>();
        eventInputController.OnColorChanged += HandleColorChanged;
        eventInputController.OnBrightnessChanged += HandleBrightnessChanged;
        eventInputController.OnFadeChanged += HandleEasingChanged;
        eventInputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        eventInputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        eventInputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        // Keep outer GLS group placement synchronized with the shared primary/secondary/white selector.
        ColorTypeController.OnColorChanged += HandleColorChanged;
        // Restore the outer GLS color preview after its menu subscriptions are active.
        EditorStateService.Register(this);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        eventInputController.OnColorChanged -= HandleColorChanged;
        eventInputController.OnBrightnessChanged -= HandleBrightnessChanged;
        eventInputController.OnFadeChanged -= HandleEasingChanged;
        eventInputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        eventInputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        eventInputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        // Unsubscribe the outer GLS group placement from the shared color selector when it is destroyed.
        ColorTypeController.OnColorChanged -= HandleColorChanged;
    }

    // Keep this outer GLS color-group preview in its own map metadata node.
    public string StateKey => "colorGroup";

    // Let the owner write its queued GLS color group without global object discovery.
    public void CaptureEditorState(JSONObject data) => GLSPlacementEditorState.WriteColor(data, QueuedData.Boxes[0].Events[0]);

    // Apply only this placement's cached color-group data after map metadata loads.
    public void LoadEditorState(JSONNode data)
    {
        var queuedEvent = QueuedData.Boxes[0].Events[0];
        // Keep late metadata loads identical to Start-time restoration for every GLS color control.
        GLSPlacementEditorState.RestoreColorPlacementState(data, queuedEvent, eventInputController);
    }

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        base.HandlePlacementToData(inputState);
        var firstEvt = QueuedData.Boxes[0].Events[0];
        // Extension nodes inherit color from the previous node and must not carry independent Chroma RGB data.
        if (firstEvt.UsePrevious == 0 && EventPlacement.CanPlaceChromaEvents && colorPicker != null)
        {
            firstEvt.CustomColor = colorPicker.CurrentColor;
        }
        else
        {
            firstEvt.CustomColor = null;
        }
        // Apply the independent strobe picker only to the group's non-extension starter node.
        firstEvt.StrobeColor = firstEvt.UsePrevious == 0 && StrobeColorPickerController.Instance is { IsEnabled: true } strobePicker
            ? strobePicker.CurrentColor
            : null;
        firstEvt.SaveCustom();
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleColorChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Color = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Brightness = Mathf.Max(value, 0f);
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleStrobeFrequencyChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Frequency = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleStrobeBrightnessChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].StrobeBrightness = Mathf.Max(value, 0f);
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleSoftStrobeChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].StrobeFade = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Easing = (int)(value >= 0 ? EaseType.Linear : EaseType.None);
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionChanged(int value)
    {
        var firstEvt = QueuedData.Boxes[0].Events[0];
        firstEvt.UsePrevious = value;
        // Remove both custom color channels immediately when this node becomes an extension node.
        if (value != 0)
        {
            firstEvt.CustomColor = null;
            firstEvt.StrobeColor = null;
            firstEvt.SaveCustom();
        }
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightColorEventBoxGroup
            {
                Boxes = new()
                {
                    // The placement menu starts at zero brightness; do not create the outer event at the 100% default.
                    new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase() } }
                }
            });
}
