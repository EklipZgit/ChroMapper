using Beatmap.Base;
using Beatmap.Enums;

// GLS marker icons keep OE-style SpriteRenderer layout while assigning one distinct curve glyph to every supported easing.
public enum GLSEventIconType
{
    None,
    Instant,
    Transition,
    EaseLinear,
    EaseInQuadratic,
    EaseOutQuadratic,
    EaseInOutQuadratic,
    EaseInSinusoidal,
    EaseOutSinusoidal,
    EaseInOutSinusoidal,
    EaseInCubic,
    EaseOutCubic,
    EaseInOutCubic,
    EaseInQuartic,
    EaseOutQuartic,
    EaseInOutQuartic,
    EaseInQuintic,
    EaseOutQuintic,
    EaseInOutQuintic,
    EaseInExponential,
    EaseOutExponential,
    EaseInOutExponential,
    EaseInCircular,
    EaseOutCircular,
    EaseInOutCircular,
    EaseInBack,
    EaseOutBack,
    EaseInOutBack,
    EaseInElastic,
    EaseOutElastic,
    EaseInOutElastic,
    EaseInBounce,
    EaseOutBounce,
    EaseInOutBounce,
    EaseBeatSaberInOutBack,
    EaseBeatSaberInOutElastic,
    EaseBeatSaberInOutBounce,
    RotationAutomatic,
    RotationClockwise,
    RotationCounterClockwise
}

// A fixed pair covers every Official Editor marker while keeping prefab renderer ownership explicit.
public readonly struct GLSEventIconState
{
    public GLSEventIconState(GLSEventIconType primary, GLSEventIconType secondary)
        : this(primary, secondary, GLSEventIconType.None)
    {
    }

    // ColorNodeTwoColumnLayout adds a third color-node slot so strobeColorEasing can render beside the
    // transition easing and strobeEasing markers.
    public GLSEventIconState(GLSEventIconType primary, GLSEventIconType secondary, GLSEventIconType tertiary)
    {
        Primary = primary;
        Secondary = secondary;
        Tertiary = tertiary;
    }

    public GLSEventIconType Primary { get; }
    public GLSEventIconType Secondary { get; }
    public GLSEventIconType Tertiary { get; }
}

public static class GLSEventIconResolver
{
    // Resolve only serialized event state so pooled appearance refreshes never search scene objects or allocate collections.
    public static GLSEventIconState Resolve(BaseGLSEvent evt)
    {
        return evt switch
        {
            BaseLightColorBase colorEvent when colorEvent.UsePrevious == 0 => ResolveColor(colorEvent),
            BaseLightRotationBase rotationEvent when rotationEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(rotationEvent.EaseType),
                ResolveRotationDirection(rotationEvent.Direction)),
            BaseLightTranslationBase translationEvent when translationEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(translationEvent.EaseType),
                GLSEventIconType.None),
            BaseFxEventFloat fxEvent when fxEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(fxEvent.Easing),
                GLSEventIconType.None),
            _ => new GLSEventIconState(GLSEventIconType.None, GLSEventIconType.None)
        };
    }

    // ColorNodeTwoColumnLayout: the top-left icon tracks the effective colorEasing curve so an authored
    // override cannot disagree with the abbreviation beneath it, the middle-right icon renders the actual
    // strobe fade curve, and the bottom-left icon exposes authored strobeColorEasing only.
    private static GLSEventIconState ResolveColor(BaseLightColorBase colorEvent)
    {
        var primary = colorEvent.Easing == (int)EaseType.None
            ? GLSEventIconType.Instant
            : ResolveEasing(colorEvent.ChromaColorEasing ?? colorEvent.Easing);
        var secondary = GLSEventCommon.IsStrobing(colorEvent)
            ? colorEvent.StrobeFade == 1
                ? ResolveEasing(colorEvent.ChromaStrobeEasing ?? (int)EaseType.InOutCubic)
                : GLSEventIconType.Instant
            : GLSEventIconType.None;
        var tertiary = colorEvent.ChromaStrobeColorEasing is { } strobeColorEasing
            ? ResolveEasing(strobeColorEasing)
            : GLSEventIconType.None;
        return new GLSEventIconState(primary, secondary, tertiary);
    }

    // An exhaustive direct mapping makes Alt-scroll sprite swaps constant-time and prevents distinct curves sharing artwork.
    private static GLSEventIconType ResolveEasing(int easing)
    {
        return (EaseType)easing switch
        {
            EaseType.Linear => GLSEventIconType.EaseLinear,
            EaseType.InQuadratic => GLSEventIconType.EaseInQuadratic,
            EaseType.OutQuadratic => GLSEventIconType.EaseOutQuadratic,
            EaseType.InOutQuadratic => GLSEventIconType.EaseInOutQuadratic,
            EaseType.InSinusoidal => GLSEventIconType.EaseInSinusoidal,
            EaseType.OutSinusoidal => GLSEventIconType.EaseOutSinusoidal,
            EaseType.InOutSinusoidal => GLSEventIconType.EaseInOutSinusoidal,
            EaseType.InCubic => GLSEventIconType.EaseInCubic,
            EaseType.OutCubic => GLSEventIconType.EaseOutCubic,
            EaseType.InOutCubic => GLSEventIconType.EaseInOutCubic,
            EaseType.InQuartic => GLSEventIconType.EaseInQuartic,
            EaseType.OutQuartic => GLSEventIconType.EaseOutQuartic,
            EaseType.InOutQuartic => GLSEventIconType.EaseInOutQuartic,
            EaseType.InQuintic => GLSEventIconType.EaseInQuintic,
            EaseType.OutQuintic => GLSEventIconType.EaseOutQuintic,
            EaseType.InOutQuintic => GLSEventIconType.EaseInOutQuintic,
            EaseType.InExponential => GLSEventIconType.EaseInExponential,
            EaseType.OutExponential => GLSEventIconType.EaseOutExponential,
            EaseType.InOutExponential => GLSEventIconType.EaseInOutExponential,
            EaseType.InCircular => GLSEventIconType.EaseInCircular,
            EaseType.OutCircular => GLSEventIconType.EaseOutCircular,
            EaseType.InOutCircular => GLSEventIconType.EaseInOutCircular,
            EaseType.InBack => GLSEventIconType.EaseInBack,
            EaseType.OutBack => GLSEventIconType.EaseOutBack,
            EaseType.InOutBack => GLSEventIconType.EaseInOutBack,
            EaseType.InElastic => GLSEventIconType.EaseInElastic,
            EaseType.OutElastic => GLSEventIconType.EaseOutElastic,
            EaseType.InOutElastic => GLSEventIconType.EaseInOutElastic,
            EaseType.InBounce => GLSEventIconType.EaseInBounce,
            EaseType.OutBounce => GLSEventIconType.EaseOutBounce,
            EaseType.InOutBounce => GLSEventIconType.EaseInOutBounce,
            EaseType.BeatSaberInOutBack => GLSEventIconType.EaseBeatSaberInOutBack,
            EaseType.BeatSaberInOutElastic => GLSEventIconType.EaseBeatSaberInOutElastic,
            EaseType.BeatSaberInOutBounce => GLSEventIconType.EaseBeatSaberInOutBounce,
            _ => GLSEventIconType.None
        };
    }

    // Rotation direction has its own OE sprite family and always occupies the secondary marker slot.
    private static GLSEventIconType ResolveRotationDirection(int direction)
    {
        return (LightRotationDirection)direction switch
        {
            LightRotationDirection.Clockwise => GLSEventIconType.RotationClockwise,
            LightRotationDirection.CounterClockwise => GLSEventIconType.RotationCounterClockwise,
            _ => GLSEventIconType.RotationAutomatic
        };
    }
}
