using Beatmap.Base;
using UnityEngine;

public static class GLSEventFloatFXCommand
{
    public static void SetValue(BaseFxEventFloat evt, float value)
    {
        if (Mathf.Approximately(evt.Value, value)) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Value = value;
        GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newGroup);
    }
}
