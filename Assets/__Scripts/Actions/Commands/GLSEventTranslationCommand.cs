using Beatmap.Base;
using UnityEngine;

public static class GLSEventTranslationCommand
{
    public static BaseLightTranslationBase SetValue(BaseLightTranslationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Translation, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Translation = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSTranslationValue);
    }
}
