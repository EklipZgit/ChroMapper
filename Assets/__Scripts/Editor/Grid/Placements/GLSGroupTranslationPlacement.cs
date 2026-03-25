using System.Linq;
using Beatmap.Base;
using UnityEngine;

public class
    GLSGroupTranslationPlacement : GLSGroupPlacement<BaseLightTranslationEventBoxGroup,
    GLSGroupTranslationGridContainer>
{
    [SerializeField] private BeatmapGLSTranslationInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.TranslationTracks.Any(x => x) && !inputController.IsHovering;

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
        QueuedData.Boxes[0].Events[0].Translation = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].EaseType = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightTranslationEventBoxGroup GenerateOriginalData() =>
        new()
        {
            Boxes = new() { new BaseLightTranslationEventBox { Events = new[] { new BaseLightTranslationBase() } } }
        };
}
