using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class
    GLSGroupTranslationPlacement : GLSGroupPlacement<BaseLightTranslationEventBoxGroup,
    GLSGroupTranslationGridContainer>
{
    [SerializeField] private BeatmapGLSEventTranslationInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.TranslationTracks.Any(x => x) && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        easingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        easingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    private void HandleValueChanged(float value)
    {
        QueuedData.Boxes[0].Events[0].Translation = value;
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

    protected override BaseLightTranslationEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseLightTranslationEventBoxGroup()
            {
                Boxes = new()
                {
                    new BaseLightTranslationEventBox { Events = new[] { new BaseLightTranslationBase() } }
                }
            });
}
