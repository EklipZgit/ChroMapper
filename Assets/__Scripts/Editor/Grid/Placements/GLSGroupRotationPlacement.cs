using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class
    GLSGroupRotationPlacement : GLSGroupPlacement<BaseLightRotationEventBoxGroup, GLSGroupRotationGridContainer>
{
    [SerializeField] private BeatmapGLSGroupRotationInputController groupInputController;
    [SerializeField] private BeatmapGLSEventRotationInputController eventInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.RotationTracks.Any(x => x) && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        eventInputController.OnValueChanged += HandleValueChanged;
        eventInputController.OnLoopChanged += HandleLoopChanged;
        eventInputController.OnDirectionChanged += HandleDirectionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
    }

    public void OnDestroy()
    {
        eventInputController.OnValueChanged -= HandleValueChanged;
        eventInputController.OnLoopChanged -= HandleLoopChanged;
        eventInputController.OnDirectionChanged -= HandleDirectionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
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

    private void HandleExtensionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightRotationEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightRotationEventBoxGroup()
            {
                Boxes = new() { new BaseLightRotationEventBox { Events = new[] { new BaseLightRotationBase() } } }
            });
}
