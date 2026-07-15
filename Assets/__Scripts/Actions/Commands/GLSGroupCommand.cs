using Beatmap.Base;
using Beatmap.Helper;

public static class GLSGroupCommand
{
    public static BaseEventBoxGroup Mirror(BaseEventBoxGroup group)
    {
        if (group is not BaseLightColorEventBoxGroup lcebg) return null;
        var newGroup = BeatmapFactory.Clone(lcebg);
        foreach (var box in newGroup.Boxes)
        foreach (var evt in box.Events)
            evt.Color = (evt.Color + 1) % 3;
        return GLSCommonCommand.TriggerPlaceAction(group, newGroup);
    }
}
