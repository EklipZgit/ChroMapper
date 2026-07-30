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
        newEvt.CustomColor = null;
        newEvt.WriteCustom();
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

    /// <summary>
    /// Sets a non-chroma Beat Saber 1/N frequency for strobes, nulling the chroma property.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static BaseLightColorBase SetStrobeFrequencyOnly(BaseLightColorBase evt, int value)
    {
        if (evt.Frequency == value && evt.ChromaStrobeInterval == null)
            return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Frequency = value;
        newEvt.ChromaStrobeInterval = null;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorFrequency);
    }

    /// <summary>
    /// Sets a ChromaGLS strobeInterval frequency for strobe, and sets Frequency appropriately to the closest matching OEM frequency.
    /// If null, resets everything to no strobe and drops the ChromaGLS strobeInterval property.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static BaseLightColorBase SetStrobeIntervalAndClosestFrequency(BaseLightColorBase evt, float? value)
    {
        if (value is not null)
        {
            var expectedFrequency = value < 0.75f ? 2 : 1;
            if (evt.ChromaStrobeInterval is { } existing && Mathf.Approximately(existing, value.Value) && evt.Frequency == expectedFrequency)
                return null;
        }

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.ChromaStrobeInterval = value;
        if (value is not null)
            newEvt.Frequency = value < 0.75f ? 2 : 1;
        else
            newEvt.Frequency = 0;  // Reset back to no strobe when passing through strobeInterval 0.5
        newEvt.WriteCustom();
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

    public static void SetLerpType(BaseLightColorBase evt, string value)
    {
        if (evt.CustomLerpType == value) return;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.CustomLerpType = value;
        newEvt.WriteCustom();
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSColorLerpType);
    }
}
