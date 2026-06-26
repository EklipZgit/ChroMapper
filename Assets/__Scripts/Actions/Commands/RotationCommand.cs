using System;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public static class RotationCommand
{
    public static void PlaceEventInPlace(float time, bool clockwise, float prec)
    {
        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        var regc = BeatmapObjectContainerCollection.GetCollectionForType<RotationEventGridContainer>(
            ObjectType.RotationEvent);
        var evt = regc.MapObjects.Find(e =>
            e.SongBpmTime - epsilon < time
            && e.SongBpmTime + epsilon > time
            && e.ExecutionTime == ExecutionTime.Early);

        var modifier = clockwise ? 1 : -1;
        if (evt != null)
        {
            var newEvt = BeatmapFactory.Clone(evt);
            newEvt.Rotation = Mathf.Round((newEvt.Rotation + (modifier * prec)) * 1_000f) / 1_000f;
            BeatmapActionContainer.AddAction(
                new BeatmapObjectUpdatedAction(newEvt, evt, "Placed an Event."));
        }
        else
        {
            var newEvt = new BaseRotationEvent
            {
                JsonTime = time,
                ExecutionTime = ExecutionTime.Early,
                Rotation = Mathf.Round(modifier * prec * 1_000f) / 1_000f
            };
            BeatmapActionContainer.AddAction(
                new BeatmapObjectPlacementAction(newEvt, Array.Empty<BaseRotationEvent>(), "Placed an Event."));
        }
    }

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

    public static void SetRotation(BaseObject originalObject, float rotate)
    {
        var newObject = BeatmapFactory.Clone(originalObject);

        switch (newObject)
        {
            case BaseRotationEvent evt:
                {
                    evt.Rotation = rotate;
                    break;
                }
            case BaseGrid grid:
                {
                    grid.Rotation = (int)rotate;
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
