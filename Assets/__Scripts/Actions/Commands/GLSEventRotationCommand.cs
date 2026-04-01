using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventRotationCommand
{
    public static void SetValue(BaseLightRotationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Rotation, value)) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Rotation = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSRotationValue);
    }

    public static void SetDirection(BaseLightRotationBase evt, LightRotationDirection value)
    {
        if (evt.Direction == (int)value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Direction = (int)value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSRotationDirection);
    }

    public static void SetLoop(BaseLightRotationBase evt, int value)
    {
        if (evt.Loop == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Loop = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSRotationLoop);
    }
}
