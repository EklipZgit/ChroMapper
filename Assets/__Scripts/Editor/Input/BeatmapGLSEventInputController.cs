using Beatmap.Base;
using Beatmap.Containers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

public abstract class BeatmapGLSEventInputController<TData> : BeatmapInputController<GLSEventContainer>
    where TData : BaseGLSEvent
{
    protected override bool ValidObject(GLSEventContainer container) => container.ObjectData is TData;
}
