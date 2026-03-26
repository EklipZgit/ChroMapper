using Beatmap.Base;
using UnityEngine;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.FloatFXTrack && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        easingInputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        easingInputController.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Value = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Easing = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseVfxEventEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat() } } } };
}
