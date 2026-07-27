using Beatmap.Base;
using UnityEngine;

public class
    GLSEventTranslationPlacement : GLSEventPlacement<BaseLightTranslationEventBoxGroup, BaseLightTranslationBase>, EditorStateService.IEditorStateProvider
{
    [SerializeField] private BeatmapGLSEventTranslationInputController inputController;

    public override void Start()
    {
        base.Start();
        inputController.OnValueChanged += HandleValueChanged;
        EasingInputController.OnEasingChanged += HandleEasingChanged;
        EasingInputController.OnExtensionChanged += HandleExtensionChanged;
        // Restore after this placement has connected its input callbacks.
        var savedState = EditorStateService.Register(this);
        if (savedState != null) GLSPlacementEditorState.ReadTranslation(savedState, QueuedData);
    }

    public void OnDestroy()
    {
        EditorStateService.Unregister(this);
        inputController.OnValueChanged -= HandleValueChanged;
        EasingInputController.OnEasingChanged -= HandleEasingChanged;
        EasingInputController.OnExtensionChanged -= HandleExtensionChanged;
    }

    // Keep the inner GLS translation preview state with its placement owner.
    public string StateKey => "translationEvent";
    public void CaptureEditorState(SimpleJSON.JSONObject data) => GLSPlacementEditorState.WriteTranslation(data, QueuedData);

    // Apply only this placement's cached translation-node data after map metadata loads.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        GLSPlacementEditorState.ReadTranslation(data, QueuedData);
        inputController.NotifyValueChanged(QueuedData.Translation);
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

    private void HandleExtensionChanged(int value)
    {
        QueuedData.UsePrevious = value;
        GlsEventAppearance.SetAppearance(PlacementVisualContainer, false);
    }

    protected override BaseLightTranslationBase GenerateOriginalData() => new();
}
