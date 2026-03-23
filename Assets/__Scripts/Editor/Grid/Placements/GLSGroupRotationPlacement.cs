using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class
    GLSGroupRotationPlacement : GLSGroupPlacement<BaseLightRotationEventBoxGroup, GLSGroupRotationGridContainer>
{
    [SerializeField] private BeatmapGLSRotationPlacementInput placementInput;
    [SerializeField] private BeatmapEasingsSelectionInput easingInput;

    public override void Start()
    {
        base.Start();
        placementInput.OnValueChanged += HandleValueChanged;
        placementInput.OnLoopChanged += HandleLoopChanged;
        placementInput.OnDirectionChanged += HandleDirectionChanged;
        easingInput.OnEasingChanged += HandleEasingChanged;
    }

    public void OnDestroy()
    {
        placementInput.OnValueChanged -= HandleValueChanged;
        placementInput.OnLoopChanged -= HandleLoopChanged;
        placementInput.OnDirectionChanged -= HandleDirectionChanged;
        easingInput.OnEasingChanged -= HandleEasingChanged;
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Rotation = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleLoopChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Loop = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleDirectionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].Direction = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].EaseType = value;
        glsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    public override bool CanPlace => base.CanPlace && GlsEventTrack.TrackDefinition.RotationTracks.Any(x => x);

    protected override BaseLightRotationEventBoxGroup GenerateOriginalData() =>
        new() { Boxes = new() { new BaseLightRotationEventBox { Events = new[] { new BaseLightRotationBase() } } } };
}
