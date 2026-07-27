using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditModeContext : MonoBehaviour, CMInput.IEditModeActions, EditorStateService.IEditorStateProvider
{
    [SerializeField] private EditingMode editingMode = EditingMode.Gameplay;

    public EditingMode EditingMode
    {
        get => editingMode;
        set
        {
            if (editingMode == value) return;
            editingMode = value;
            NotifyChanged();
        }
    }

    public event Action<EditingMode> OnEditModeChanged;
    // Keep the active workspace tab with the context that publishes tab changes.
    public string StateKey => "editingMode";
    public void NotifyChanged() => OnEditModeChanged?.Invoke(EditingMode);

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        NotifyChanged();
    }

    private void Start()
    {
        var savedState = EditorStateService.Register(this);
        if (savedState != null)
        {
            LoadEditorState(savedState);
        }
    }

    // Remove the workspace context from saves after its scene has been destroyed.
    private void OnDestroy() => EditorStateService.Unregister(this);

    // Save the currently selected editor workspace tab.
    public void CaptureEditorState(SimpleJSON.JSONObject data) => data["mode"] = (int)EditingMode;

    // Let the context notify every tab view when its saved workspace mode is restored.
    public void LoadEditorState(SimpleJSON.JSONNode data)
    {
        if (data.HasKey("mode"))
        {
            EditingMode = (EditingMode)data["mode"].AsInt;
        }
    }

    public void OnGameplayEdit(InputAction.CallbackContext context)
    {
        if (context.performed) EditingMode = EditingMode.Gameplay;
    }

    public void OnGLSEdit(InputAction.CallbackContext context)
    {
        if (context.performed) EditingMode = EditingMode.GLS;
    }

    public void OnBasicEventEdit(InputAction.CallbackContext context)
    {
        if (context.performed) EditingMode = EditingMode.BasicEvent;
    }
}
