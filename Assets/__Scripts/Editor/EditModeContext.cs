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
            ToggleActionMaps(editingMode, value);
            editingMode = value;
            NotifyChanged();
        }
    }

    private readonly Type[] gameplayActionMaps = {
        typeof(CMInput.INoteObjectsActions), typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IArcObjectsActions), typeof(CMInput.IArcPlacementActions),
        typeof(CMInput.IChainObjectsActions), typeof(CMInput.IChainPlacementActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.IBPMChangeObjectsActions),
        typeof(CMInput.INJSEventObjectsActions),
    };
    
    private readonly Type[] basicEventActionMaps =
    {
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IEventGridActions),
    };
    
    private readonly Type[] glsActionMaps =
    {
        typeof(CMInput.IGLSGroupTabsActions),
        typeof(CMInput.IGLSGroupSelectActions),
        typeof(CMInput.IGLSColorObjectsActions),
        typeof(CMInput.IGLSRotationObjectsActions),
        typeof(CMInput.IGLSTranslationObjectsActions),
        typeof(CMInput.IGLSFloatFXObjectsActions),
        typeof(CMInput.IEasingsSelectionActions)
    };

    private int fpsCount;
    public void Update()
    {
        fpsCount++;
        if (fpsCount >= 10)
        {
            print(CMInputCallbackInstaller.IsActionMapDisabled(typeof(CMInput.IGLSColorObjectsActions)));
            fpsCount = 0;
        }
    }
    
    private readonly Type editModeType = typeof(EditModeContext);

    public void Start()
    {
        CMInputCallbackInstaller.DisableActionMaps(editModeType, basicEventActionMaps);
        CMInputCallbackInstaller.DisableActionMaps(editModeType, glsActionMaps);
    }

    public void OnDestroy()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, gameplayActionMaps);
        CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, basicEventActionMaps);
        CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, glsActionMaps);
    }

    private void ToggleActionMaps(EditingMode previous, EditingMode current)
    {
        // GLS and EventBox share similar context
        if ((previous == EditingMode.GLS && current == EditingMode.EventBox)
            || (previous == EditingMode.EventBox && current == EditingMode.GLS))
            return;

        switch (previous)
        {
            case EditingMode.Gameplay:
                CMInputCallbackInstaller.DisableActionMaps(editModeType, gameplayActionMaps);
                break;
            case EditingMode.BasicEvent:
                CMInputCallbackInstaller.DisableActionMaps(editModeType, basicEventActionMaps);
                break;
            case EditingMode.GLS:
            case EditingMode.EventBox:
                CMInputCallbackInstaller.DisableActionMaps(editModeType, glsActionMaps);
                break;
        }

        switch (current)
        {
            case EditingMode.Gameplay:
                CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, gameplayActionMaps);
                break;
            case EditingMode.BasicEvent:
                CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, basicEventActionMaps);
                break;
            case EditingMode.GLS:
            case EditingMode.EventBox:
                CMInputCallbackInstaller.ClearDisabledActionMaps(editModeType, glsActionMaps);
                break;
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
