using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;

public static class GLSEventRotationCommand
{
    public static BaseLightRotationBase SetValue(BaseLightRotationBase evt, float value)
    {
        if (Mathf.Approximately(evt.Rotation, value)) return null;
        // Trace the exact source event before cloning while diagnosing stale outer-preview targets.
        LogMutation("rotation", evt, value);
        var (newGroup, newEvt) = CopyAndLog(evt);
        newEvt.Rotation = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationValue);
    }

    public static BaseLightRotationBase SetDirection(BaseLightRotationBase evt, LightRotationDirection value)
    {
        if (evt.Direction == (int)value) return null;
        // Trace the exact source event before cloning while diagnosing stale outer-preview targets.
        LogMutation("direction", evt, (int)value);
        var (newGroup, newEvt) = CopyAndLog(evt);
        newEvt.Direction = (int)value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationDirection);
    }

    public static void SetEaseType(BaseLightRotationBase evt, int value)
    {
        if (evt.EaseType == value) return;
        // Trace the exact source event before cloning while diagnosing stale outer-preview targets.
        LogMutation("ease", evt, value);
        var (newGroup, newEvt) = CopyAndLog(evt);
        newEvt.EaseType = value;
        GLSCommonCommand.TriggerModifyEventBoxAction(
            evt.EventBoxGroupData,
            newGroup,
            ActionMergeType.ModifyGLSRotationEaseType);
    }

    public static BaseLightRotationBase SetLoop(BaseLightRotationBase evt, int value)
    {
        if (evt.Loop == value) return null;
        // Trace the exact source event before cloning while diagnosing stale outer-preview targets.
        LogMutation("loop", evt, value);
        var (newGroup, newEvt) = CopyAndLog(evt);
        newEvt.Loop = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSRotationLoop);
    }

    private static void LogMutation(string property, BaseLightRotationBase evt, float value)
    {
        var group = evt.EventBoxGroupData;
        var box = evt.EventBoxData;
        // Keep malformed event diagnostics safe even when a stale preview has no valid box index.
        var eventGroupMatchesBox = group != null && evt.BoxIndex >= 0 && evt.BoxIndex < group.ReadOnlyBoxes.Count &&
            ReferenceEquals(box, group.ReadOnlyBoxes[evt.BoxIndex]);
        Debug.Log(
            $"[GLS Rotation Mutation] {property}={value}: groupId={group?.ID}, groupTime={group?.JsonTime}, " +
            $"groupType={group?.GetType().Name}, boxIndex={evt.BoxIndex}, boxEvents={box?.ReadOnlyEvents.Count}, " +
            $"eventOffset={evt.RelativeJsonTime}, eventTime={evt.JsonTime}, eventGroupMatchesBox={eventGroupMatchesBox}.");
    }

    private static (BaseEventBoxGroup group, BaseLightRotationBase evt) CopyAndLog(BaseLightRotationBase evt)
    {
        var (group, copiedEvent) = GLSCommonCommand.CopyGroupFrom(evt);
        var eventCount = 0;
        foreach (var box in group.ReadOnlyBoxes)
        {
            eventCount += box.ReadOnlyEvents.Count;
        }

        // Record whether cloning preserved the source event topology before an action replaces the group.
        Debug.Log(
            $"[GLS Rotation Mutation] clone: groupId={group.ID}, time={group.JsonTime}, boxes={group.ReadOnlyBoxes.Count}, " +
            $"events={eventCount}, copiedBox={copiedEvent.BoxIndex}, " +
            $"copiedOffset={copiedEvent.RelativeJsonTime}.");
        return (group, copiedEvent);
    }
}
