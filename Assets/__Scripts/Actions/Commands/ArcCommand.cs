using Beatmap.Base;
using Beatmap.Helper;

public static class ArcCommand
{
    public static void SetHeadControlPointLengthMultiplier(BaseArc baseArc, float value)
    {
        var newArc = BeatmapFactory.Clone(baseArc);
        newArc.HeadControlPointLengthMultiplier = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(newArc, baseArc, "Update head multiplier",
                mergeType: ActionMergeType.ArcHeadMultTweak),
            perform: true);
    }

    public static void SetTailControlPointLengthMultiplier(BaseArc baseArc, float value)
    {
        var newArc = BeatmapFactory.Clone(baseArc);
        newArc.TailControlPointLengthMultiplier = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(newArc, baseArc, "Update tail multiplier",
                mergeType: ActionMergeType.ArcTailMultTweak),
            perform: true);
    }
}

