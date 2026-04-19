using Beatmap.Base;
using UnityEngine;

public static class GLSEventTranslationCommand
{
    public static void SetValue(BaseLightTranslationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Translation, value)) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Translation = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(evt.EventBoxGroupData, newGroup,
            ActionMergeType.ModifyGLSTranslationValue);
    }
}
