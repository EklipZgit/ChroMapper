using Beatmap.Base;
using Beatmap.Helper;
using UnityEngine;

public static class RotationCommand
{
    public static void RotateObject(BaseObject originalObject, bool clockwise, float prec)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        var modifier = clockwise ? 1 : -1;
        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    evt.Rotation = Mathf.Round((evt.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
                    break;
                }
            case BaseGrid grid:
                {
                    grid.Rotation += Mathf.RoundToInt(modifier * prec);
                    break;
                }
        }

        if (newObject.CompareTo(originalObject) == 0) return;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
    }
    
    public static void Invert(BaseRotationEvent originalObject)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        newObject.Rotation *= -1;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
    }

    public static void ModifyHover(BaseRotationEvent originalObject, int modifier, float prec)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        newObject.Rotation = Mathf.Round((newObject.Rotation + (modifier * prec)) * 1_000f) / 1_000f;

        if (newObject.CompareTo(originalObject) == 0) return;

        BeatmapActionContainer.AddAction(
            new BeatmapObjectUpdatedAction(
                newObject,
                originalObject,
                mergeType: ActionMergeType.ModifyRotationValue),
            perform: true);
    }
}
