using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventRotationCommand
{
    public static BaseLightRotationBase SetValue(BaseLightRotationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Rotation, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Rotation = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationValue);
    }

    public static BaseLightRotationBase SetDirection(BaseLightRotationBase evt, LightRotationDirection value)
    {
        if (evt.Direction == (int)value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Direction = (int)value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationDirection);
    }

    public static BaseLightRotationBase SetLoop(BaseLightRotationBase evt, int value)
    {
        if (evt.Loop == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Loop = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationLoop);
    }
}
