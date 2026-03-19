using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class EditModeContext : MonoBehaviour, CMInput.IEditModeActions
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
    public void NotifyChanged() => OnEditModeChanged?.Invoke(EditingMode);

    private void OnValidate()
    {
        if (Application.isPlaying) return;
        NotifyChanged();
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
