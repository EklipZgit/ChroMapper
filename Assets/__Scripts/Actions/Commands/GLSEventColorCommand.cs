using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class GLSEventColorCommand
{
    public static void SetColor(BaseLightColorBase evt, int value)
    {
        if (evt.Color == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Color = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorColor);
    }

    public static void SetBrightness(BaseLightColorBase evt, float value)
    {
        if (Mathf.Approximately(evt.Brightness, value)) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Brightness = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorBrightness);
    }

    public static void SetBrightnessAndEasing(BaseLightColorBase evt, float value, EaseType ease)
    {
        if (Mathf.Approximately(evt.Brightness, value) && evt.Easing == (int)ease) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Brightness = value;
        newEvt.Easing = (int)ease;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorBrightnessAndEasing);
    }

    public static void SetUsePrevious(BaseLightColorBase evt, int value)
    {
        if (evt.UsePrevious == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.UsePrevious = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorUsePrevious);
    }

    public static void SetEasing(BaseLightColorBase evt, int value)
    {
        if (evt.Easing == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Easing = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorEasing);
    }

    public static void SetFrequency(BaseLightColorBase evt, int value)
    {
        if (evt.Frequency == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Frequency = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorFrequency);
    }

    public static void SetStrobeBrightness(BaseLightColorBase evt, float value)
    {
        if (Mathf.Approximately(evt.StrobeBrightness, value)) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeBrightness = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorStrobeBrightness);
    }

    public static void SetStrobeFade(BaseLightColorBase evt, int value)
    {
        if (evt.StrobeFade == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeFade = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorStrobeFade);
    }
}
