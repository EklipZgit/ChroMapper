using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventColorCommand
{
    public static BaseLightColorBase SetColor(BaseLightColorBase evt, int value)
    {
        if (evt.Color == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Color = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorColor);
    }

    public static BaseLightColorBase SetBrightness(BaseLightColorBase evt, float value)
    {
        if (Mathf.Approximately(evt.Brightness, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Brightness = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorBrightness);
    }

    public static BaseLightColorBase SetBrightnessAndEasing(BaseLightColorBase evt, float value, EaseType ease)
    {
        if (Mathf.Approximately(evt.Brightness, value) && evt.Easing == (int)ease) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Brightness = value;
        newEvt.Easing = (int)ease;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorBrightnessAndEasing);
    }

    public static BaseLightColorBase SetUsePrevious(BaseLightColorBase evt, int value)
    {
        if (evt.UsePrevious == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.UsePrevious = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorUsePrevious);
    }

    public static BaseLightColorBase SetEasing(BaseLightColorBase evt, int value)
    {
        if (evt.Easing == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Easing = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorEasing);
    }

    public static BaseLightColorBase SetFrequency(BaseLightColorBase evt, int value)
    {
        if (evt.Frequency == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Frequency = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorFrequency);
    }

    public static BaseLightColorBase SetStrobeBrightness(BaseLightColorBase evt, float value)
    {
        if (Mathf.Approximately(evt.StrobeBrightness, value)) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeBrightness = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeBrightness);
    }

    public static BaseLightColorBase SetStrobeFade(BaseLightColorBase evt, int value)
    {
        if (evt.StrobeFade == value) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeFade = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeFade);
    }
}
