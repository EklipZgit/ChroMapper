using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class
    GLSGroupRotationPlacement : GLSGroupPlacement<BaseLightRotationEventBoxGroup, GLSGroupRotationGridContainer>
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.RotationTracks.Any(x => x) && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        inputController.OnLoopChanged += HandleLoopChanged;
        inputController.OnDirectionChanged += HandleDirectionChanged;
        easingInputController.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        inputController.OnLoopChanged -= HandleLoopChanged;
        inputController.OnDirectionChanged -= HandleDirectionChanged;
        easingInputController.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Rotation = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleLoopChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Loop = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleDirectionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Direction = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].EaseType = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightRotationEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseLightRotationEventBox { Events = new[] { new BaseLightRotationBase() } } } };
}
