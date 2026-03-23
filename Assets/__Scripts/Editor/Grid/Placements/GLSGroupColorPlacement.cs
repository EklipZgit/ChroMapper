using Beatmap.Base;
using UnityEngine;

public class GLSGroupColorPlacement : GLSGroupPlacement<BaseLightColorEventBoxGroup, GLSGroupColorGridContainer>
{
    [SerializeField] private GLSColorPlacementInput placementInput;

    public override void Start()
    {
        base.Start();
        placementInput.OnBrightnessChanged += HandleBrightnessChanged;
        placementInput.OnColorChanged += HandleColorChanged;
        placementInput.OnExtensionPerformed += HandleExtensionsPerformed;
        placementInput.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        placementInput.OnBrightnessChanged -= HandleBrightnessChanged;
        placementInput.OnColorChanged -= HandleColorChanged;
        placementInput.OnExtensionPerformed -= HandleExtensionsPerformed;
        placementInput.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleBrightnessChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Brightness = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleColorChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Color = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleExtensionsPerformed(bool value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = value ? 1 : 0;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = 0;
        QueuedData.Boxes[0].Events[0].Easing = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.ColorTrack;

    protected override BaseLightColorEventBoxGroup GenerateOriginalData() =>
        new()
        {
            Boxes = new()
            {
                new BaseLightColorEventBox { Events = new[] { new BaseLightColorBase { Brightness = 1f } } }
            }
        };
}
