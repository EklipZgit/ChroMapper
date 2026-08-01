using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
public static class GLSEventInputHoverTracker
{
    private static int hoveredControllerCount;

    // Both inner and outer GLS controllers claim scroll precision only while their own hover target is active.
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
