using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class GLSGroupCommand
{
    public static void Mirror(BaseEventBoxGroup group)
    {
        if (group is not BaseLightColorEventBoxGroup lcebg) return;
        var newGroup = BeatmapFactory.Clone(lcebg);
        foreach (var box in newGroup.Boxes)
        foreach (var evt in box.Events)
            evt.Color = (evt.Color + 1) % 3;
        GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }
}
