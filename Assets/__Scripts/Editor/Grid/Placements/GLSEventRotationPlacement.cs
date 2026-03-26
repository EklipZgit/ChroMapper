using Beatmap.Base;
using UnityEngine;

public class
    GLSEventRotationPlacement : GLSEventPlacement<BaseLightRotationEventBoxGroup, BaseLightRotationBase>
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;
    [SerializeField] private BeatmapEasingsSelectionInputController easingInputController;

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
        QueuedData.Rotation = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleLoopChanged(int value)
    {
        QueuedData.Loop = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleDirectionChanged(int value)
    {
        QueuedData.Direction = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.EaseType = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightRotationBase GenerateOriginalData() => new();
}
