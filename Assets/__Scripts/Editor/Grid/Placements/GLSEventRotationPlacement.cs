using Beatmap.Base;
using UnityEngine;

public class
    GLSEventRotationPlacement : GLSEventPlacement<BaseLightRotationEventBoxGroup, BaseLightRotationBase>, EditorStateService.IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventRotationInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        inputController.OnLoopChanged += HandleLoopChanged;
        inputController.OnDirectionChanged += HandleDirectionChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        var savedState = EditorStateService.Register(this);
        if (savedState != null)
        {
            GLSPlacementEditorState.ReadRotation(savedState, QueuedData);
            inputController.NotifyValueChanged(QueuedData.Rotation);
            inputController.NotifyLoopChanged(QueuedData.Loop);
            inputController.NotifyDirectionChanged(QueuedData.Direction);
        }
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnValueChanged -= HandleValueChanged;
        inputController.OnLoopChanged -= HandleLoopChanged;
        inputController.OnDirectionChanged -= HandleDirectionChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the inner GLS rotation preview state with its placement owner.
    public string StateKey => "rotationEvent";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteRotation(data, QueuedData);

    // Apply only this placement's cached rotation-node data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        GLSPlacementEditorState.ReadRotation(data, QueuedData);
        inputController.NotifyValueChanged(QueuedData.Rotation);
        inputController.NotifyLoopChanged(QueuedData.Loop);
        inputController.NotifyDirectionChanged(QueuedData.Direction);
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

    private void HandleExtensionChanged(int value)
    {
        QueuedData.UsePrevious = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightRotationBase GenerateOriginalData() => new();
}
