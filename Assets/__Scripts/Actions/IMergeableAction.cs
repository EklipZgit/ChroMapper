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
    
    ModifyRotationValue,

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
    // Keep repeated GLS axis wheel edits in one undoable gesture.
    ModifyGLSEventAxis,

    ModifyGLSColorColor,
    ModifyGLSColorBrightness,
    ModifyGLSColorBrightnessAndEasing,
    ModifyGLSColorUsePrevious,
    ModifyGLSColorEasing,
    ModifyGLSColorFrequency,
    ModifyGLSColorStrobeBrightness,
    ModifyGLSColorStrobeFade,
    ModifyGLSColorLerpType,

    ModifyGLSRotationValue,
    ModifyGLSRotationDirection,
    ModifyGLSRotationLoop,
    ModifyGLSRotationEaseType,

    ModifyGLSTranslationValue,

    ModifyGLSFloatFXValue,

    RingRotationValueTweak,
    RingSpeedTweak,
    RingStepTweak,
    // Keep propagation wheel edits mergeable without conflating them with ring step changes.
    RingPropagationTweak,
    RingPropTweak,
    RingZoomStepTweak,
    RingZoomSpeedTweak,
    // Keep Basic Event laser-speed and lock scrolls in their respective undoable gestures.
    LaserSpeedTweak,
    LaserLockRotationTweak,

    LightLerpTypeTweak,
    LightEasingTweak,
}
