using Beatmap.Base;
using UnityEngine;

public class
    GLSEventTranslationPlacement : GLSEventPlacement<BaseLightTranslationEventBoxGroup, BaseLightTranslationBase>
{
    [SerializeField] private BeatmapGLSEventTranslationInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

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
        QueuedData.Translation = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.EaseType = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightTranslationBase GenerateOriginalData() => new();
}
