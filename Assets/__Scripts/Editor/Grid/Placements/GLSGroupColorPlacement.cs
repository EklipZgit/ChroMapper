using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.ColorTrack && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnStrobeFrequencyChanged += HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged += HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged += HandleSoftStrobeChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnStrobeFrequencyChanged -= HandleStrobeFrequencyChanged;
        inputController.OnStrobeBrightnessChanged -= HandleStrobeBrightnessChanged;
        inputController.OnSoftStrobeChanged -= HandleSoftStrobeChanged;
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

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        base.HandlePlacementToData(inputState);
        foreach (var evt in QueuedData.Boxes.SelectMany(box => box.Events))
            evt.JsonTime = QueuedData.JsonTime + evt.RelativeJsonTime;
    }
}
