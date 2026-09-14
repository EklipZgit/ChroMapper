using System;
using System.Linq;
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

    // GLSColorEasingInputTest: the color-easing cycle stores custom curves in customData.colorEasing while the
    // interval's native transition keeps its own Easing value, and the OEM Linear slot removes the key.
    public static BaseLightColorBase SetColorEasing(BaseLightColorBase evt, int easing, int? colorEasing)
    {
        if (evt.Easing == easing && evt.ChromaColorEasing == colorEasing) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Easing = easing;
        newEvt.ChromaColorEasing = colorEasing;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorEasing);
    }

    // GLSColorEasingInputTest: authoring a strobe color easing on an instant node promotes it to a Linear
    // transition so the authored curve has an interval to drive; the unset slot removes the key.
    public static BaseLightColorBase SetStrobeColorEasing(BaseLightColorBase evt, int? value)
    {
        var promotedEasing = value.HasValue ? (int)EaseType.Linear : evt.Easing;
        if (evt.ChromaStrobeColorEasing == value && evt.Easing == promotedEasing) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.ChromaStrobeColorEasing = value;
        if (value.HasValue && newEvt.Easing == (int)EaseType.None)
            newEvt.Easing = promotedEasing;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeColorEasing);
    }

    // GLSColorEasingInputTest: the strobe fade cycle owns both the fade flag and its customData.strobeEasing
    // override; the OEM fade keeps sf=1 with no key so the native InOutCubic curve takes over.
    public static BaseLightColorBase SetStrobeFadeEasing(BaseLightColorBase evt, int strobeFade, int? strobeEasing)
    {
        if (evt.StrobeFade == strobeFade && evt.ChromaStrobeEasing == strobeEasing) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeFade = strobeFade;
        newEvt.ChromaStrobeEasing = strobeFade == 1 ? strobeEasing : null;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeFade);
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

    // GLSEasingTypeRibbonInputTest: customData.easingType serializes only the authored HSV state; RGB and
    // unknown strings are the absent default, and the command targets the transition's owning node so
    // ribbon chords can merge like the other color edits.
    public static BaseLightColorBase SetLerpType(BaseLightColorBase evt, string value)
    {
        var normalized = value == "HSV" ? "HSV" : null;
        if (evt.CustomLerpType == normalized) return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.CustomLerpType = normalized;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorLerpType);
    }

    // Event shift controls replace only their selected ordered array and retain the node's remaining customData through WriteCustom.
    public static BaseLightColorBase SetShifts(BaseLightColorBase evt, string[] value, bool strobe)
    {
        value ??= Array.Empty<string>();
        var existing = strobe
            ? evt.StrobeShifts
            : evt.Shifts;
        if (existing.SequenceEqual(value))
        {
            return null;
        }

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        if (strobe)
        {
            newEvt.StrobeShifts = value;
        }
        else
        {
            newEvt.Shifts = value;
        }

        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            strobe
                ? ActionMergeType.ModifyGLSStrobeColorShifts
                : ActionMergeType.ModifyGLSColorShifts);
    }
}
