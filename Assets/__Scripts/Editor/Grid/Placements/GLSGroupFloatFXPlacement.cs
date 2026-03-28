using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>
{
    [SerializeField] private BeatmapGLSEventFloatFXInputController inputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.FloatFXTrack && !inputController.IsHovering;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
    }

    public void OnDestroy()
    {
        inputController.OnValueChanged -= HandleValueChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
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

    private void HandleExtensionChanged(int value)
    {
        QueuedData.Boxes[0].Events[0].UsePrevious = value;
        GlsGroupAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseVfxEventEventBoxGroup GenerateOriginalData() =>
        BeatmapFactory.Clone(
            new BaseVfxEventEventBoxGroup
            {
                Boxes = new() { new BaseVfxEventEventBox { Events = new[] { new BaseFxEventFloat() } } }
            });

    protected override void HandlePlacementToData(PlacementInputState inputState)
    {
        base.HandlePlacementToData(inputState);
        foreach (var evt in QueuedData.Boxes.SelectMany(box => box.Events))
            evt.JsonTime = QueuedData.JsonTime + evt.RelativeJsonTime;
    }
}
