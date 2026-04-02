using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;

public abstract class BeatmapGLSEventInputController<TData> : BeatmapInputController<GLSEventContainer>
    where TData : BaseGLSEvent
{
    [SerializeField] protected ScrollPrecisionController ScrollPrecisionController;
    [SerializeField] protected BeatmapEasingsSelectionInputController EasingInputController;

    protected override bool ValidObject(GLSEventContainer container) => container.ObjectData is TData;
}
