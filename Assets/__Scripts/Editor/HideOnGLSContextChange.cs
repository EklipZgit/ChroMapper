using Beatmap.Base;
using UnityEngine;

public class HideOnGLSContextChange : MonoBehaviour
{
    [SerializeField] private EditModeContext editContext;
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;


    [SerializeField] private GameObject targetColorMain;
    [SerializeField] private GameObject[] targetColor;
    [SerializeField] private GameObject targetRotationMain;
    [SerializeField] private GameObject[] targetRotation;
    [SerializeField] private GameObject targetTranslationMain;
    [SerializeField] private GameObject[] targetTranslation;
    [SerializeField] private GameObject targetFloatFXMain;
    [SerializeField] private GameObject[] targetFloatFX;

    [SerializeField] private GameObject[] targetShared;

    private void Start()
    {
        editContext.OnEditModeChanged += HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        HandleEditModeChanged(editContext.EditingMode);
    }

    private void OnDestroy()
    {
        editContext.OnEditModeChanged -= HandleEditModeChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
    }

    private void HandleEditModeChanged(EditingMode mode)
    {
        ToggleMain();
        ToggleTarget(mode.HasFlag(EditingMode.EventBox) ? glsEventGridProvider.GroupContext : null);
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        ToggleMain();
        if (editContext.EditingMode.HasFlag(EditingMode.EventBox)) ToggleTarget(group);
    }

    private void ToggleMain()
    {
        if (editContext.EditingMode.HasFlag(EditingMode.EventBox) && glsEventGridProvider.GroupContext != null)
        {
            targetColorMain.SetActive(glsEventGridProvider.GroupContext is BaseLightColorEventBoxGroup);
            targetRotationMain.SetActive(glsEventGridProvider.GroupContext is BaseLightRotationEventBoxGroup);
            targetTranslationMain.SetActive(glsEventGridProvider.GroupContext is BaseLightTranslationEventBoxGroup);
            targetFloatFXMain.SetActive(glsEventGridProvider.GroupContext is BaseVfxEventEventBoxGroup);
        }
        else
        {
            targetColorMain.SetActive(true);
            targetRotationMain.SetActive(true);
            targetTranslationMain.SetActive(true);
            targetFloatFXMain.SetActive(true);
        }

        foreach (var o in targetShared)
        {
            o.SetActive(
                editContext.EditingMode.HasFlag(EditingMode.EventBox)
                && glsEventGridProvider.GroupContext is BaseLightRotationEventBoxGroup
                    or BaseLightTranslationEventBoxGroup
                    or BaseVfxEventEventBoxGroup);
        }
    }

    private void ToggleTarget(BaseEventBoxGroup group)
    {
        foreach (var o in targetColor) o.SetActive(group is BaseLightColorEventBoxGroup);
        foreach (var o in targetRotation) o.SetActive(group is BaseLightRotationEventBoxGroup);
        foreach (var o in targetTranslation) o.SetActive(group is BaseLightTranslationEventBoxGroup);
        foreach (var o in targetFloatFX) o.SetActive(group is BaseVfxEventEventBoxGroup);
    }
}
