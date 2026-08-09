using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSCommonCommand
{
    // Apply the dedicated authored axis action without inspecting or overlapping physical modifier state.
    public static void CycleEventAxis(InputAction.CallbackContext context, BaseGLSEvent evt)
    {
        if (!context.performed || evt == null)
            return;

        var direction = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (direction == 0)
            return;

        switch (evt)
        {
            case BaseLightRotationBase rotation:
                CycleRotationEventAxis(rotation, direction);
                break;
            case BaseLightTranslationBase translation:
                CycleTranslationEventAxis(translation, direction);
                break;
        }
    }

    // Move a rotation event between axis tracks while preserving every sibling on the source track.
    private static void CycleRotationEventAxis(BaseLightRotationBase evt, int direction)
    {
        if (evt.EventBoxGroupData is not BaseLightRotationEventBoxGroup originalGroup
            || evt.EventBoxData is not BaseLightRotationEventBox
            || !TryFindEventIndex(evt, out var eventIndex))
        {
            Debug.LogError("[GLSAxisScroll] Rotation event has invalid group or box ownership.");
            return;
        }

        var editedGroup = BeatmapFactory.Clone(originalGroup);
        var sourceBox = editedGroup.Boxes[evt.BoxIndex];
        var targetAxis = CycleAxis(sourceBox.Axis, direction);
        MoveEventToAxisTrack(
            editedGroup.Boxes,
            evt.BoxIndex,
            eventIndex,
            targetAxis,
            static box => box.Axis,
            static (box, axis) => box.Axis = axis);

        RebindGroup(editedGroup);
        TriggerModifyEventBoxAction(originalGroup, editedGroup, ActionMergeType.ModifyGLSEventAxis);
    }

    // Move a translation event between axis tracks while preserving every sibling on the source track.
    private static void CycleTranslationEventAxis(BaseLightTranslationBase evt, int direction)
    {
        if (evt.EventBoxGroupData is not BaseLightTranslationEventBoxGroup originalGroup
            || evt.EventBoxData is not BaseLightTranslationEventBox
            || !TryFindEventIndex(evt, out var eventIndex))
        {
            Debug.LogError("[GLSAxisScroll] Translation event has invalid group or box ownership.");
            return;
        }

        var editedGroup = BeatmapFactory.Clone(originalGroup);
        var sourceBox = editedGroup.Boxes[evt.BoxIndex];
        var targetAxis = CycleAxis(sourceBox.Axis, direction);
        MoveEventToAxisTrack(
            editedGroup.Boxes,
            evt.BoxIndex,
            eventIndex,
            targetAxis,
            static box => box.Axis,
            static (box, axis) => box.Axis = axis);

        RebindGroup(editedGroup);
        TriggerModifyEventBoxAction(originalGroup, editedGroup, ActionMergeType.ModifyGLSEventAxis);
    }

    // Reuse an existing destination-axis track, creating one only when the destination axis does not exist.
    private static (bool createdDestination, bool removedSource) MoveEventToAxisTrack<TBox>(
        List<TBox> boxes,
        int sourceBoxIndex,
        int eventIndex,
        int targetAxis,
        Func<TBox, int> getAxis,
        Action<TBox, int> setAxis)
        where TBox : BaseEventBox
    {
        var sourceBox = boxes[sourceBoxIndex];
        var movedEvent = sourceBox.ReadOnlyEvents[eventIndex];
        TBox targetBox = null;
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            if (boxIndex != sourceBoxIndex && getAxis(boxes[boxIndex]) == targetAxis)
            {
                targetBox = boxes[boxIndex];
                break;
            }
        }

        var createdDestination = targetBox == null;
        if (createdDestination)
        {
            // Clone track configuration only when the destination axis does not exist yet.
            targetBox = BeatmapFactory.Clone(sourceBox);
            setAxis(targetBox, targetAxis);
            targetBox.SetEvents(new BaseGLSEvent[] { movedEvent });
        }
        else
        {
            // Insert after existing equal-time events so their JSON order remains deterministic; Array.Sort is unstable on ties.
            var insertionIndex = 0;
            while (insertionIndex < targetBox.ReadOnlyEvents.Count
                   && targetBox.ReadOnlyEvents[insertionIndex].RelativeJsonTime <= movedEvent.RelativeJsonTime)
            {
                insertionIndex++;
            }

            var targetEvents = new BaseGLSEvent[targetBox.ReadOnlyEvents.Count + 1];
            for (var i = 0; i < insertionIndex; i++)
                targetEvents[i] = targetBox.ReadOnlyEvents[i];
            targetEvents[insertionIndex] = movedEvent;
            for (var i = insertionIndex; i < targetBox.ReadOnlyEvents.Count; i++)
                targetEvents[i + 1] = targetBox.ReadOnlyEvents[i];
            targetBox.SetEvents(targetEvents);
        }

        var remainingEvents = new BaseGLSEvent[sourceBox.ReadOnlyEvents.Count - 1];
        var destination = 0;
        for (var i = 0; i < sourceBox.ReadOnlyEvents.Count; i++)
        {
            if (i != eventIndex)
                remainingEvents[destination++] = sourceBox.ReadOnlyEvents[i];
        }

        sourceBox.SetEvents(remainingEvents);
        var removedSource = sourceBox.ReadOnlyEvents.Count == 0;
        // Vacated source tracks disappear before the newly-created destination track is appended.
        if (removedSource)
            boxes.RemoveAt(sourceBoxIndex);
        if (createdDestination)
            boxes.Add(targetBox);
        // Keep event-box tracks in stable X/Y/Z order regardless of which direction created the destination.
        SortAxisTracks(boxes, getAxis);
        return (createdDestination, removedSource);
    }

    // Preserve relative order within each axis while normalizing the three axis groups.
    private static void SortAxisTracks<TBox>(List<TBox> boxes, Func<TBox, int> getAxis)
    {
        var orderedBoxes = new List<TBox>(boxes.Count);
        for (var axis = (int)Axis.X; axis <= (int)Axis.Z; axis++)
        {
            for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
            {
                if (getAxis(boxes[boxIndex]) == axis)
                    orderedBoxes.Add(boxes[boxIndex]);
            }
        }

        // Preserve malformed or future axis values after the supported X/Y/Z tracks instead of dropping data.
        for (var boxIndex = 0; boxIndex < boxes.Count; boxIndex++)
        {
            var axis = getAxis(boxes[boxIndex]);
            if (axis < (int)Axis.X || axis > (int)Axis.Z)
                orderedBoxes.Add(boxes[boxIndex]);
        }

        boxes.Clear();
        boxes.AddRange(orderedBoxes);
    }

    // Resolve the cloned child by stable box and event indexes without scanning the beatmap.
    private static bool TryFindEventIndex(BaseGLSEvent evt, out int eventIndex)
    {
        eventIndex = -1;
        if (evt.BoxIndex < 0
            || evt.EventBoxGroupData == null
            || evt.BoxIndex >= evt.EventBoxGroupData.ReadOnlyBoxes.Count
            || !ReferenceEquals(evt.EventBoxGroupData.ReadOnlyBoxes[evt.BoxIndex], evt.EventBoxData))
        {
            return false;
        }

        var events = evt.EventBoxData.ReadOnlyEvents;
        for (var i = 0; i < events.Count; i++)
        {
            if (ReferenceEquals(events[i], evt))
            {
                eventIndex = i;
                return true;
            }
        }

        return false;
    }

    // Keep wheel direction reversible while wrapping through the three supported axes.
    private static int CycleAxis(int currentAxis, int direction) =>
        (currentAxis + Math.Sign(direction) + 3) % 3;

    // Rebind every edited child after a box split so inner lanes and outer previews share valid ownership.
    private static void RebindGroup<TBox>(BaseEventBoxGroup<TBox> group)
        where TBox : BaseEventBox
    {
        for (var boxIndex = 0; boxIndex < group.Boxes.Count; boxIndex++)
        {
            var box = group.Boxes[boxIndex];
            foreach (var evt in box.ReadOnlyEvents)
            {
                evt.EventBoxData = box;
                evt.EventBoxGroupData = group;
                evt.BoxIndex = boxIndex;
                evt.JsonTime = group.JsonTime + evt.RelativeJsonTime;
            }
        }

        group.ResortOrderedEvents();
        group.SaveCustom();
    }

    public static (BaseEventBoxGroup group, TEvent evt) CopyGroupFrom<TEvent>(TEvent evt)
        where TEvent : BaseGLSEvent
    {
        var newEvtIdx = Array.IndexOf((TEvent[])evt.EventBoxData.ReadOnlyEvents, evt);
        var newGroup = BeatmapFactory.Clone(evt.EventBoxGroupData);
        var newEvt = newGroup.ReadOnlyBoxes[evt.BoxIndex].ReadOnlyEvents[newEvtIdx] as TEvent;
        return (newGroup, newEvt);
    }

    public static BaseEventBoxGroup TriggerPlaceAction(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, oldGroup, "Modified event box group.");
        BeatmapActionContainer.AddAction(action, true);
        return newGroup;
    }

    public static BaseEventBoxGroup TriggerModifyEventBoxAction(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup,
        ActionMergeType actionMergeType)
    {
        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            oldGroup,
            "Modified event box group.",
            actionMergeType);
        BeatmapActionContainer.AddAction(action, true);
        return newGroup;
    }

    public static TEvent TriggerModifyEventAction<TEvent>(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup,
        TEvent newEvt,
        ActionMergeType actionMergeType)
        where TEvent : BaseGLSEvent
    {
        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            oldGroup,
            "Modified event box.",
            actionMergeType);
        BeatmapActionContainer.AddAction(action, true);
        return newEvt;
    }
}
