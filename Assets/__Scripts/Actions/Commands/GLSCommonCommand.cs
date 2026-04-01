using System;
using Beatmap.Base;
using Beatmap.Helper;

public static class GLSCommonCommand
{
    public static (BaseEventBoxGroup group, TEvent evt) CopyGroupFrom<TEvent>(TEvent evt)
        where TEvent : BaseGLSEvent
    {
        var newEvtIdx = Array.IndexOf((TEvent[])evt.EventBoxData.ReadOnlyEvents, evt);
        var newGroup = BeatmapFactory.Clone(evt.EventBoxGroupData);
        var newEvt = newGroup.ReadOnlyBoxes[evt.BoxIndex].ReadOnlyEvents[newEvtIdx] as TEvent;
        return (newGroup, newEvt);
    }

    public static void TriggerPlaceAction(
        BaseEventBoxGroup oldGroup,
        BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, oldGroup, "Modified event box group.");
        BeatmapActionContainer.AddAction(action, true);
    }

    public static void TriggerModifyEventBoxAction(
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
    }
}
