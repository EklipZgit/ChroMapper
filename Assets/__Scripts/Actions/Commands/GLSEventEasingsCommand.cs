using Beatmap.Base;
using Beatmap.Enums;

public static class GLSEventEasingsCommand
{
    public static void SetEasing(BaseGLSEvent evt, int value)
    {
        switch (evt)
        {
            case BaseLightColorBase lcb:
                value = (int)(value >= 0 ? EaseType.Linear : EaseType.None);
                if (lcb.Easing == value) return;
                var (newCGroup, newCEvt) = GLSCommonCommand.CopyGroupFrom(lcb);
                newCEvt.Easing = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newCGroup);
                break;
            case BaseLightRotationBase lrb:
                if (lrb.EaseType == value) return;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.EaseType = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newRGroup);
                break;
            case BaseLightTranslationBase ltb:
                if (ltb.EaseType == value) return;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.EaseType = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newTGroup);
                break;
            case BaseFxEventFloat fx:
                if (fx.Easing == value) return;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.Easing = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newFGroup);
                break;
        }
    }

    public static void SetExtension(BaseGLSEvent evt, int value)
    {
        switch (evt)
        {
            case BaseLightColorBase lcb:
                if (lcb.UsePrevious == value) return;
                var (newCGroup, newCEvt) = GLSCommonCommand.CopyGroupFrom(lcb);
                newCEvt.UsePrevious = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newCGroup);
                break;
            case BaseLightRotationBase lrb:
                if (lrb.UsePrevious == value) return;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.UsePrevious = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newRGroup);
                break;
            case BaseLightTranslationBase ltb:
                if (ltb.UsePrevious == value) return;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.UsePrevious = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newTGroup);
                break;
            case BaseFxEventFloat fx:
                if (fx.UsePrevious == value) return;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.UsePrevious = value;
                GLSCommonCommand.TriggerAction(evt.EventBoxGroupData, newFGroup);
                break;
        }
    }
}
