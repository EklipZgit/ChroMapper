using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>
{
    [SerializeField] private BeatmapGLSGroupColorInputController groupInputController;
    [SerializeField] private BeatmapGLSEventColorInputController eventInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.ColorTrack && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        eventInputController.OnColorChanged += HandleColorChanged;
        eventInputController.OnBrightnessChanged += HandleBrightnessChanged;
        eventInputController.OnFadeChanged += HandleEasingChanged;
        eventInputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        eventInputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        eventInputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        eventInputController.OnColorChanged -= HandleColorChanged;
        eventInputController.OnBrightnessChanged -= HandleBrightnessChanged;
        eventInputController.OnFadeChanged -= HandleEasingChanged;
        eventInputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        eventInputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        eventInputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
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
        QueuedData.Boxes[0].Events[0].UsePrevious = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightColorEventBoxGroup
            {
                Boxes = new()
                {
                    new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase { Brightness = 1f } } }
                }
            });
}
