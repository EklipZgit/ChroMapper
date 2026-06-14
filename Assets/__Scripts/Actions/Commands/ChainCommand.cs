using Beatmap.Base;
using Beatmap.Helper;

public static class ChainCommand
{
    public static void SetSliceCount(BaseChain baseChain, int value)
    {
        var newChain = BeatmapFactory.Clone(baseChain);
        newChain.SliceCount = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(newChain, baseChain, "Update slice count",
                mergeType: ActionMergeType.ChainSliceCountTweak), perform: true);
    }

    public static void SetSquish(BaseChain baseChain, float value)
    {
        var newChain = BeatmapFactory.Clone(baseChain);
        newChain.Squish = value;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(newChain, baseChain, "Update squish",
                mergeType: ActionMergeType.ChainSquishTweak), perform: true);
    }
}

