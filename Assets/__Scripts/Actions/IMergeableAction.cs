public interface IMergeableAction
{
    ActionMergeType MergeType { get; set; }
    int MergeCount { get; set; }

    public IMergeableAction TryMerge(IMergeableAction previous);
    public bool CanMerge(IMergeableAction previous);
    public IMergeableAction DoMerge(IMergeableAction previous);
}

public enum ActionMergeType
{
    None,
    NoteDirectionChange,
    NotePreciseDirectionTweak,
    ArcHeadDirectionChange,
    ArcTailDirectionChange,
    ArcHeadMultTweak,
    ArcTailMultTweak,
    ChainSliceCountTweak,
    ChainSquishTweak,
    WallDurationTweak,
    WallLowerBoundTweak,
    WallUpperBoundTweak,
    EventMainTweak,
    EventAltTweak,
    BPMValueTweak,

    ModifyNJSEventValue,
    ModifyNJSEventEase,
    ModifyNJSEventExtension,

    ReorderEventBox,

    ModifyEventBoxFilterType,
    ModifyEventBoxFilterParam0,
    ModifyEventBoxFilterParam1,
    ModifyEventBoxFilterReverse,
    ModifyEventBoxFilterChunk,
    ModifyEventBoxFilterRandom,
    ModifyEventBoxFilterSeed,
    ModifyEventBoxFilterLimit,
    ModifyEventBoxFilterLimitAffectsType,

    ModifyEventBoxBeatDistributionType,
    ModifyEventBoxBeatDistribution,
    ModifyEventBoxAxis,
    ModifyEventBoxFlip,
    ModifyEventBoxValueDistribution,
    ModifyEventBoxValueDistributionType,
    ModifyEventBoxAffectFirst,
    ModifyEventBoxEasing,

    ModifyGLSEventEasing,
    ModifyGLSEventExtension,

    ModifyGLSColorColor,
    ModifyGLSColorBrightness,
    ModifyGLSColorBrightnessAndEasing,
    ModifyGLSColorUsePrevious,
    ModifyGLSColorEasing,
    ModifyGLSColorFrequency,
    ModifyGLSColorStrobeBrightness,
    ModifyGLSColorStrobeFade,

    ModifyGLSRotationValue,
    ModifyGLSRotationDirection,
    ModifyGLSRotationLoop,

    ModifyGLSTranslationValue,

    ModifyGLSFloatFXValue,
}
