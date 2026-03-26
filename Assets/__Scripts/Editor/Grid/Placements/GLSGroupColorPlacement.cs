using Beatmap.Base;
using UnityEngine;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>
{
    [SerializeField] private BeatmapGLSEventColorInputController inputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.ColorTrack && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnBrightnessChanged += HandleBrightnessChanged;
        inputController.OnColorChanged += HandleColorChanged;
        inputController.OnExtensionPerformed += HandleExtensionsPerformed;
        inputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        inputController.OnBrightnessChanged -= HandleBrightnessChanged;
        inputController.OnColorChanged -= HandleColorChanged;
        inputController.OnExtensionPerformed -= HandleExtensionsPerformed;
        inputController.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Brightness = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleColorChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Color = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionsPerformed(bool value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = value ? 1 : 0;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = 0;
        QueuedData.Boxes[0].Events[0].Easing = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightColorEventBoxGroup GenerateOriginalData() =>
        new()
        {
            Boxes = new()
            {
                new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase { Brightness = 1f } } }
            }
        };
}
