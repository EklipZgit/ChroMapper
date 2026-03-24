using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

public abstract class BeatmapGLSGroupInputController<TData> : BeatmapInputController<GLSGroupContainer>,
                                                              CMInput.IGLSGroupSelectActions
    where TData : BaseEventBoxGroup
{
    [SerializeField] private GLSEventGridProvider eventGridProvider;
    protected override bool ValidObject(GLSGroupContainer container) => container.EventBoxGroupData is TData;

    public void OnEnterGroup(InputAction.CallbackContext context)
    {
        if (context.performed && editContext.EditingMode.HasFlag(EditingMode.GLS) && IsHovering)
        {
            eventGridProvider.GroupContext = HoveredObject.EventBoxGroupData;
            editContext.EditingMode = EditingMode.EventBox;
        }
    }

    public void OnExitGroup(InputAction.CallbackContext context)
    {
        if (context.performed && editContext.EditingMode.HasFlag(EditingMode.EventBox))
        {
            editContext.EditingMode = EditingMode.GLS;
        }
    }
}
