using Beatmap.Base;
using Beatmap.Enums;

public static class GLSEventEasingCommand
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
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newCGroup,
                    ActionMergeType.ModifyGLSEventEasing);
                break;
            case BaseLightRotationBase lrb:
                if (lrb.EaseType == value) return;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.EaseType = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newRGroup,
                    ActionMergeType.ModifyGLSEventEasing);
                break;
            case BaseLightTranslationBase ltb:
                if (ltb.EaseType == value) return;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.EaseType = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newTGroup,
                    ActionMergeType.ModifyGLSEventEasing);
                break;
            case BaseFxEventFloat fx:
                if (fx.Easing == value) return;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.Easing = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newFGroup,
                    ActionMergeType.ModifyGLSEventEasing);
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
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newCGroup,
                    ActionMergeType.ModifyGLSEventExtension);
                break;
            case BaseLightRotationBase lrb:
                if (lrb.UsePrevious == value) return;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.UsePrevious = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newRGroup,
                    ActionMergeType.ModifyGLSEventExtension);
                break;
            case BaseLightTranslationBase ltb:
                if (ltb.UsePrevious == value) return;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.UsePrevious = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newTGroup,
                    ActionMergeType.ModifyGLSEventExtension);
                break;
            case BaseFxEventFloat fx:
                if (fx.UsePrevious == value) return;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.UsePrevious = value;
                GLSCommonCommand.TriggerModifyEventBoxAction(
                    evt.EventBoxGroupData,
                    newFGroup,
                    ActionMergeType.ModifyGLSEventExtension);
                break;
        }
    }
}
