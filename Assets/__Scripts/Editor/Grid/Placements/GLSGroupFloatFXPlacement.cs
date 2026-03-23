using Beatmap.Base;
using UnityEngine;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>
{
    [SerializeField] private GLSFloatFXPlacementInput placementInput;
    [SerializeField] private EasingsSelectionInput easingInput;

    public override void Start()
    {
        base.Start();
        placementInput.OnValueChanged += HandleValueChanged;
        easingInput.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        placementInput.OnValueChanged -= HandleValueChanged;
        easingInput.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Value = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Easing = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.FloatFXTrack;

    protected override BaseVfxEventEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat() } } } };
}
