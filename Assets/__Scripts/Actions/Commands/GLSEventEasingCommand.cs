using Beatmap.Base;

public static class GLSEventEasingCommand
{
    public static BaseGLSEvent SetEasing(BaseGLSEvent evt, int value)
    {
        switch (evt)
        {
            case BaseLightColorBase lcb:
                // Preserve the selected color easing in memory so hover scroll can cycle past Linear.
                if (lcb.Easing == value) return null;
                var (newCGroup, newCEvt) = GLSCommonCommand.CopyGroupFrom(lcb);
                newCEvt.Easing = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newCGroup,
                    newCEvt,
                    ActionMergeType.ModifyGLSEventEasing);
            case BaseLightRotationBase lrb:
                if (lrb.EaseType == value) return null;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.EaseType = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newRGroup,
                    newREvt,
                    ActionMergeType.ModifyGLSEventEasing);
            case BaseLightTranslationBase ltb:
                if (ltb.EaseType == value) return null;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.EaseType = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newTGroup,
                    newTEvt,
                    ActionMergeType.ModifyGLSEventEasing);
            case BaseFxEventFloat fx:
                if (fx.Easing == value) return null;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.Easing = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newFGroup,
                    newFEvt,
                    ActionMergeType.ModifyGLSEventEasing);
        }

        return null;
    }

    public static BaseGLSEvent SetExtension(BaseGLSEvent evt, int value)
    {
        switch (evt)
        {
            case BaseLightColorBase lcb:
                if (lcb.UsePrevious == value) return null;
                var (newCGroup, newCEvt) = GLSCommonCommand.CopyGroupFrom(lcb);
                newCEvt.UsePrevious = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newCGroup,
                    newCEvt,
                    ActionMergeType.ModifyGLSEventExtension);
            case BaseLightRotationBase lrb:
                if (lrb.UsePrevious == value) return null;
                var (newRGroup, newREvt) = GLSCommonCommand.CopyGroupFrom(lrb);
                newREvt.UsePrevious = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newRGroup,
                    newREvt,
                    ActionMergeType.ModifyGLSEventExtension);
            case BaseLightTranslationBase ltb:
                if (ltb.UsePrevious == value) return null;
                var (newTGroup, newTEvt) = GLSCommonCommand.CopyGroupFrom(ltb);
                newTEvt.UsePrevious = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newTGroup,
                    newTEvt,
                    ActionMergeType.ModifyGLSEventExtension);
            case BaseFxEventFloat fx:
                if (fx.UsePrevious == value) return null;
                var (newFGroup, newFEvt) = GLSCommonCommand.CopyGroupFrom(fx);
                newFEvt.UsePrevious = value;
                return GLSCommonCommand.TriggerModifyEventAction(
                    evt.EventBoxGroupData,
                    newFGroup,
                    newFEvt,
                    ActionMergeType.ModifyGLSEventExtension);
        }

        return null;
    }
}
