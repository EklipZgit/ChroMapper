using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>
{
    [SerializeField] private BeatmapGLSGroupFloatFXInputController groupInputController;
    [SerializeField] private BeatmapGLSEventFloatFXInputController eventInputController;

    public override bool CanPlace =>
        base.CanPlace && GlsGroupTrack.TrackDefinition.FloatFXTrack && !groupInputController.IsHovering;

    public override void Start()
    {
        base.Start();
        eventInputController.OnValueChanged += HandleValueChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
    }

    public void OnDestroy()
    {
        eventInputController.OnValueChanged -= HandleValueChanged;
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
}
