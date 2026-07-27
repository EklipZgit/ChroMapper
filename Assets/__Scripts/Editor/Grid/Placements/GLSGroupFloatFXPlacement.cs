using System.Linq;
using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public class GLSGroupFloatFXPlacement : GLSGroupPlacement<BaseVfxEventEventBoxGroup, GLSGroupFloatFXGridContainer>, EditorStateService.IEditorStateProvider
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
        // Restore after this placement has connected its input callbacks.
        var savedState = EditorStateService.Register(this);
        if (savedState != null) GLSPlacementEditorState.ReadFloatFx(savedState, QueuedData.Boxes[0].Events[0]);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        eventInputController.OnValueChanged -= HandleValueChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the outer GLS FloatFX preview state with its placement owner.
    public string StateKey => "floatFxGroup";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteFloatFx(data, QueuedData.Boxes[0].Events[0]);

    // Apply only this placement's cached FloatFX-group data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        var queuedEvent = QueuedData.Boxes[0].Events[0];
        GLSPlacementEditorState.ReadFloatFx(data, queuedEvent);
        eventInputController.NotifyValueChanged(queuedEvent.Value);
        GLSPlacementEditorState.RefreshFloatFxViews(queuedEvent);
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
