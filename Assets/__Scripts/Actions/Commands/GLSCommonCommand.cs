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

    public static void TriggerAction(BaseEventBoxGroup oldGroup, BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, new[] { oldGroup }, "Modified event box group.");
        action.Redo();
        BeatmapActionContainer.AddAction(action);
    }
}
