using Beatmap.Base;
using UnityEngine;

public class GLSEventFloatFXPlacement : GLSEventPlacement<BaseVfxEventEventBoxGroup, BaseFxEventFloat>
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;
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
        QueuedData.Value = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    private void HandleEasingChanged(int value)
    {
        QueuedData.Easing = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseFxEventFloat GenerateOriginalData() => new();
}
