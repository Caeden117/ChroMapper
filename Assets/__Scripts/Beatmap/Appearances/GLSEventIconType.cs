using Beatmap.Base;
using Beatmap.Enums;

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
    RotationCounterClockwise,

    NoEasingStep
}

public readonly struct GLSEventIconState
{
    public GLSEventIconState(GLSEventIconType primary, GLSEventIconType secondary)
        : this(primary, secondary, GLSEventIconType.None)
    {
    }

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
    public static GLSEventIconState Resolve(BaseGLSEvent evt)
    {
        return evt switch
        {
            BaseLightColorBase colorEvent when colorEvent.UsePrevious == 0 => ResolveColor(colorEvent),
            BaseLightRotationBase { UsePrevious: 1, Loop: > 0 } rotationExtension => new GLSEventIconState(
                rotationExtension.Easing == (int)EaseType.None
                    ? GLSEventIconType.NoEasingStep
                    : ResolveEasing(rotationExtension.Easing),
                GLSEventIconType.None),
            BaseLightRotationBase rotationEvent when rotationEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(rotationEvent.Easing),
                ResolveRotationDirection(rotationEvent.Direction)),
            BaseLightTranslationBase translationEvent when translationEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(translationEvent.Easing),
                GLSEventIconType.None),
            BaseFxEventFloat fxEvent when fxEvent.UsePrevious == 0 => new GLSEventIconState(
                ResolveEasing(fxEvent.Easing),
                GLSEventIconType.None),
            _ => new GLSEventIconState(GLSEventIconType.None, GLSEventIconType.None)
        };
    }

    private static GLSEventIconState ResolveColor(BaseLightColorBase colorEvent)
    {
        var primary = colorEvent.Easing == (int)EaseType.None
            ? GLSEventIconType.NoEasingStep
            : ResolveEasing(colorEvent.ChromaColorEasing ?? colorEvent.Easing);
        var secondary = GLSEventCommon.IsStrobing(colorEvent)
            ? colorEvent.StrobeFade == 1
                ? ResolveEasing(colorEvent.ChromaStrobeEasing ?? (int)EaseType.InOutCubic)
                : GLSEventIconType.NoEasingStep
            : GLSEventIconType.None;
        var tertiary = colorEvent.ChromaStrobeColorEasing is { } strobeColorEasing
            ? ResolveEasing(strobeColorEasing)
            : GLSEventIconType.None;
        return new GLSEventIconState(primary, secondary, tertiary);
    }

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
