using Beatmap.Base;
using UnityEngine;

public static class GLSEventFloatFXCommand
{
    public static BaseFxEventFloat SetValue(BaseFxEventFloat evt, float value)
    {
        if (Mathf.Approximately(evt.Value, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Value = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSFloatFXValue);
    }
}
