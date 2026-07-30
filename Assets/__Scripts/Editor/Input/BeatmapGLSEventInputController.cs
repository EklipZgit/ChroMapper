using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;

// Track real Input System update boundaries so repeated wheel events can be suppressed without using Time.frameCount.
// // // PROBABLY THIS ISN'T ACTUALLY NEEDED, ATTEMPT TO REMOVE AFTER THINGS STABILIZE
public static class GLSInputUpdateTracker
{
    private static int registrationCount;
    private static int updateId;

    public static int CurrentUpdateId => updateId;

    public static void Register()
    {
        if (registrationCount++ != 0)
        {
            return;
        }

        InputSystem.onBeforeUpdate += AdvanceUpdate;
    }

    public static void Unregister()
    {
        if (registrationCount == 0 || --registrationCount != 0)
        {
            return;
        }

        InputSystem.onBeforeUpdate -= AdvanceUpdate;
    }

    private static void AdvanceUpdate() => updateId++;
}

public static class GLSEventInputHoverTracker
{
    private static int hoveredControllerCount;

    // Both inner and outer GLS controllers claim scroll precision only while their own raycast target is hovered.
    public static bool IsHovering => hoveredControllerCount > 0;

    public static void SetHovering(bool isHovering) => hoveredControllerCount += isHovering ? 1 : -1;
}

public abstract class BeatmapGLSEventInputController<TData> : BeatmapInputController<GLSEventContainer>
    where TData : BaseGLSEvent
{
    [SerializeField] protected ScrollPrecisionController ScrollPrecisionController;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    private bool wasHovering;

    protected override void LateUpdate()
    {
        if (wasHovering != IsHovering)
        {
            wasHovering = IsHovering;
            GLSEventInputHoverTracker.SetHovering(wasHovering);
        }

        base.LateUpdate();
    }

    protected virtual void OnDisable()
    {
        if (!wasHovering) return;
        wasHovering = false;
        GLSEventInputHoverTracker.SetHovering(false);
    }

    protected override bool ValidObject(GLSEventContainer container) => container.ObjectData is TData;
}
