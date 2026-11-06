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
    ModifyEventBoxColorDistributions,
    ModifyEventBoxStrobeColorDistributions,

    ModifyGLSEventEasing,
    ModifyGLSEventExtension,
    ModifyGLSEventAxis,

    ModifyGLSColorColor,
    ModifyGLSColorBrightness,
    ModifyGLSColorBrightnessAndEasing,
    ModifyGLSColorUsePrevious,
    ModifyGLSColorEasing,
    ModifyGLSColorFrequency,
    ModifyGLSColorStrobeBrightness,
    ModifyGLSColorStrobeFade,
    ModifyGLSColorStrobePulseWidth,
    ModifyGLSColorStrobeColorEasing,
    ModifyGLSColorLerpType,
    ModifyGLSColorDistributions,
    ModifyGLSStrobeColorDistributions,

    ModifyGLSRotationValue,
    ModifyGLSRotationDirection,
    ModifyGLSRotationLoop,
    ModifyGLSRotationEaseType,

    ModifyGLSTranslationValue,

    ModifyGLSFloatFXValue,

    RingRotationValueTweak,
    RingSpeedTweak,
    RingStepTweak,
    RingPropagationTweak,
    RingPropTweak,
    RingZoomStepTweak,
    RingZoomSpeedTweak,
    LaserSpeedTweak,
    LaserLockRotationTweak,

    LightLerpTypeTweak,
    LightEasingTweak,
}
