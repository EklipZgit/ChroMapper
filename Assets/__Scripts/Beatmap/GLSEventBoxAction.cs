using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;

public class GLSEventBoxAction
{
    public static void AddEventBox(BaseEventBoxGroup group, int targetIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.Insert(targetIndex, new BaseLightColorEventBox());
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.Insert(targetIndex, new BaseLightRotationEventBox());
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.Insert(targetIndex, new BaseLightTranslationEventBox());
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.Insert(targetIndex, new BaseVfxEventEventBox());
                break;
        }

        TriggerAction(group, newGroup);
    }

    public static void DeleteEventBox(BaseEventBoxGroup group, int targetIndex)
    {
        if (targetIndex < 0 || group.ReadOnlyBoxes.Count <= targetIndex) return;
        var newGroup = BeatmapFactory.Clone(group);
        switch (newGroup)
        {
            case BaseLightColorEventBoxGroup lcebg:
                lcebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseLightRotationEventBoxGroup lrebg:
                lrebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseLightTranslationEventBoxGroup ltebg:
                ltebg.Boxes.RemoveAt(targetIndex);
                break;
            case BaseVfxEventEventBoxGroup ffebg:
                ffebg.Boxes.RemoveAt(targetIndex);
                break;
        }

        TriggerAction(group, newGroup);
    }

    public static void SetType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Type == value) return;
        newBox.IndexFilter.Type = value;
        TriggerAction(group, newGroup);
    }

    public static void SetParam0(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value : value - 1;
        if (newBox.IndexFilter.Param0 == newValue) return;
        newBox.IndexFilter.Param0 = newValue;

        TriggerAction(group, newGroup);
    }

    public static void SetParam1(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;

        var newValue = newBox.IndexFilter.Type == (int)IndexFilterType.Division ? value - 1 : value;
        if (newBox.IndexFilter.Param1 == newValue) return;
        newBox.IndexFilter.Param1 = newValue;

        TriggerAction(group, newGroup);
    }

    public static void SetReverse(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Reverse == value) return;
        newBox.IndexFilter.Reverse = value;
        TriggerAction(group, newGroup);
    }

    public static void SetChunk(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Chunks == value) return;
        newBox.IndexFilter.Chunks = value;
        TriggerAction(group, newGroup);
    }

    public static void SetRandom(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Random == value) return;
        newBox.IndexFilter.Random = value;
        TriggerAction(group, newGroup);
    }

    public static void SetSeed(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.Seed == value) return;
        newBox.IndexFilter.Seed = value;
        TriggerAction(group, newGroup);
    }

    public static void SetLimit(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || Mathf.Approximately(newBox.IndexFilter.Limit, value)) return;
        newBox.IndexFilter.Limit = value;
        TriggerAction(group, newGroup);
    }

    public static void SetLimitAffectsType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.IndexFilter.LimitAffectsType == value) return;
        newBox.IndexFilter.LimitAffectsType = value;
        TriggerAction(group, newGroup);
    }

    public static void SetBeatDistributionType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.BeatDistributionType == value) return;
        newBox.BeatDistributionType = value;
        TriggerAction(group, newGroup);
    }

    public static void SetBeatDistribution(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || Mathf.Approximately(newBox.BeatDistribution, value)) return;
        newBox.BeatDistribution = value;
        TriggerAction(group, newGroup);
    }

    public static void SetAxis(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                if (lreb.Axis == value) return;
                lreb.Axis = value;
                TriggerAction(group, newGroup);
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.Axis == value) return;
                lteb.Axis = value;
                TriggerAction(group, newGroup);
                break;
        }
    }

    public static void SetFlip(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightRotationEventBox lreb:
                if (lreb.Flip == value) return;
                lreb.Flip = value;
                TriggerAction(group, newGroup);
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.Flip == value) return;
                lteb.Flip = value;
                TriggerAction(group, newGroup);
                break;
        }
    }

    public static void SetValueDistribution(float value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        float newValue;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                newValue = value / 100f;
                if (lceb.BrightnessDistribution == newValue) return;
                lceb.BrightnessDistribution = newValue;
                break;
            case BaseLightRotationEventBox lreb:
                newValue = value;
                if (lreb.RotationDistribution == newValue) return;
                lreb.RotationDistribution = newValue;
                break;
            case BaseLightTranslationEventBox lteb:
                newValue = value / 100f;
                if (lteb.TranslationDistribution == newValue) return;
                lteb.TranslationDistribution = newValue;
                break;
            case BaseVfxEventEventBox ffeb:
                newValue = value / 100f;
                if (ffeb.VfxDistribution == newValue) return;
                ffeb.VfxDistribution = newValue;
                break;
        }

        TriggerAction(group, newGroup);
    }

    public static void SetValueDistributionType(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessDistributionType == value) return;
                lceb.BrightnessDistributionType = value;
                break;
            case BaseLightRotationEventBox lreb:
                if (lreb.RotationDistributionType == value) return;
                lreb.RotationDistributionType = value;
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.TranslationDistributionType == value) return;
                lteb.TranslationDistributionType = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxDistributionType == value) return;
                ffeb.VfxDistributionType = value;
                break;
        }

        TriggerAction(group, newGroup);
    }

    public static void SetAffectFirst(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null) return;
        switch (newBox)
        {
            case BaseLightColorEventBox lceb:
                if (lceb.BrightnessAffectFirst == value) return;
                lceb.BrightnessAffectFirst = value;
                break;
            case BaseLightRotationEventBox lreb:
                if (lreb.RotationAffectFirst == value) return;
                lreb.RotationAffectFirst = value;
                break;
            case BaseLightTranslationEventBox lteb:
                if (lteb.TranslationAffectFirst == value) return;
                lteb.TranslationAffectFirst = value;
                break;
            case BaseVfxEventEventBox ffeb:
                if (ffeb.VfxAffectFirst == value) return;
                ffeb.VfxAffectFirst = value;
                break;
        }

        TriggerAction(group, newGroup);
    }

    public static void SetEasing(int value, BaseEventBoxGroup group, int boxIndex)
    {
        var newGroup = BeatmapFactory.Clone(group);
        var newBox = newGroup.ReadOnlyBoxes.ElementAtOrDefault(boxIndex);
        if (newBox == null || newBox.Easing == value) return;
        newBox.Easing = value;

        TriggerAction(group, newGroup);
    }

    private static void TriggerAction(BaseEventBoxGroup oldGroup, BaseEventBoxGroup newGroup)
    {
        var action = new BeatmapObjectPlacementAction(newGroup, new[] { oldGroup }, "Modified event box group.");
        action.Redo();
        BeatmapActionContainer.AddAction(action);
    }
}
