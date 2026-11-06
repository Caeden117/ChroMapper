using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSEventHoverMutation
{
    // Keep inner and outer GLS hover mutations identical while each controller owns target resolution.
    public static bool AdjustColorBrightness(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed
            || evt == null
            || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustColorBrightness(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = CMMath.RoundToDecimals(evt.Brightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f)));
        return GLSEventColorCommand.SetBrightness(evt, Mathf.Max(0f, value), inPlace) != null;
    }

    public static bool AdjustColorFrequency(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed
            || evt == null
            || precision == null
            || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustColorFrequency(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (delta == 0)
            return false;

        // customData.strobeInterval is a period in beats per cycle; use the ring zoom precision ladder for tweaks.
        if (evt.ChromaStrobeInterval is { } interval)
        {
            var newInterval = CMMath.RoundToDecimals(interval - (delta * GetStrobeIntervalChromaStep(precision)));
            // Do not allow a zero or negative interval; keep a floor so 1/interval remains finite.
            if (newInterval <= 0f)
                newInterval = 0.01f;
            if (newInterval <= 0.5f && delta == 1)
            {
                // If we scrolled strobe interval lower and we're at 1/2 or below, swap back to OEM fractions.
                return GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, null, inPlace) != null;
            }
            else
            {
                return GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, newInterval, inPlace) != null;
            }
        }

        // Native frequency is cycles per beat, displayed as 1/N.
        var newFrequency = evt.Frequency + delta;
        if (newFrequency < 0)
            newFrequency = 0;
        if (evt.Frequency == 0 && delta == -1)
        {
            // Scrolling past 1/1 switches to the custom float interval starting at 1.0 beats per cycle.
            return GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, 1.0f, inPlace) != null;
        }
        else
        {
            return GLSEventColorCommand.SetStrobeFrequencyOnly(evt, newFrequency, inPlace) != null;
        }
    }

    public static bool AdjustColorStrobeBrightness(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustColorStrobeBrightness(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = CMMath.RoundToDecimals(evt.StrobeBrightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f)));
        return GLSEventColorCommand.SetStrobeBrightness(evt, Mathf.Max(0f, value), inPlace) != null;
    }

    public static bool AdjustColorStrobePulseWidth(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || context.ReadValue<float>() == 0f)
            return false;

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustColorStrobePulseWidth(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = CMMath.RoundToDecimals(evt.ChromaStrobePulseWidth + (delta * precision.GetCurrentStrobePulseWidthPrecision()));
        return GLSEventColorCommand.SetStrobePulseWidth(evt, value, inPlace) != null;
    }

    public static bool CycleColorStrobeFade(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        bool inPlace = false)
    {
        if (!context.performed
            || evt == null
            || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => CycleColorStrobeFade(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var current = evt.StrobeFade == 1
            ? evt.ChromaStrobeEasing ?? (int)EaseType.InOutCubic
            : (int)EaseType.None;
        var next = GetNextEasingValue(current, context, StrobeFadeEasingValues);
        if (next == (int)EaseType.None)
        {
            return GLSEventColorCommand.SetStrobeFadeEasing(evt, 0, null, inPlace) != null;
        }

        return GLSEventColorCommand.SetStrobeFadeEasing(
            evt,
            1,
            next == (int)EaseType.InOutCubic ? null : next,
            inPlace) != null;
    }

    public static bool AdjustColorEasing(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustColorEasing(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var current = evt.ChromaColorEasing ?? evt.Easing;
        var next = GetNextEasingValue(current, context, AllEasingValues);
        if (next <= (int)EaseType.Linear)
        {
            return GLSEventColorCommand.SetColorEasing(evt, next, null, inPlace) != null;
        }
        else
        {
            return GLSEventColorCommand.SetColorEasing(
                evt,
                Math.Max(evt.Easing, (int)EaseType.Linear),
                next,
                inPlace) != null;
        }
    }

    public static bool AdjustStrobeColorEasing(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustStrobeColorEasing(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var current = evt.ChromaStrobeColorEasing ?? (int)EaseType.None;
        var next = GetNextEasingValue(current, context, AllEasingValues);
        return GLSEventColorCommand.SetStrobeColorEasing(evt, next == (int)EaseType.None ? null : next, inPlace) != null;
    }

    public static void MirrorColor(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (context.performed && evt != null && InputChordOwnership.Owns(context))
        {
            GLSEventColorCommand.SetColor(evt, (evt.Color + 1) % 2);
        }
    }

    public static void CycleColorLerpType(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed
            || evt == null
            || !InputChordOwnership.Owns(context))
        {
            return;
        }

        if (GLSSelectionHoverMutation.TryApply(
                evt,
                node => ToggleColorLerpType(context, node, inPlace: true),
                out _))
        {
            return;
        }

        ToggleColorLerpType(context, evt);
    }

    public static bool ToggleColorLerpType(
        InputAction.CallbackContext context,
        BaseLightColorBase evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        return GLSEventColorCommand.SetLerpType(
            evt,
            evt.CustomLerpType == BasicEventColorLerpType.TrueHSV
                ? BasicEventColorLerpType.RGB
                : BasicEventColorLerpType.TrueHSV,
            inPlace) != null;
    }

    public static bool AdjustRotation(
        InputAction.CallbackContext context,
        BaseLightRotationBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustRotation(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var value = CMMath.RoundToDecimals(evt.Rotation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * precision.GetCurrentRotationPrecision()));
        return GLSEventRotationCommand.SetValue(evt, Mathf.Repeat(value, 360f), inPlace) != null;
    }

    public static bool AdjustRotationLoop(
        InputAction.CallbackContext context,
        BaseLightRotationBase evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustRotationLoop(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        return GLSEventRotationCommand.SetLoop(evt, (evt.Loop + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + 5) % 5, inPlace) != null;
    }

    public static bool AdjustEasing(
        InputAction.CallbackContext context,
        BaseGLSEvent evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustEasing(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var nextEasing = GetNextEasingValue(evt.Easing, context, AllEasingValues);
        return GLSEventEasingCommand.SetEasing(evt, nextEasing, inPlace) != null;
    }

    public static bool CycleRotationDirection(
        InputAction.CallbackContext context,
        BaseLightRotationBase evt,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => CycleRotationDirection(context, node, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var values = (LightRotationDirection[])Enum.GetValues(typeof(LightRotationDirection));
        var index = Array.IndexOf(values, (LightRotationDirection)evt.Direction);
        return GLSEventRotationCommand.SetDirection(evt, values[((index < 0 ? 0 : index) + 1) % values.Length], inPlace) != null;
    }

    public static bool AdjustTranslation(
        InputAction.CallbackContext context,
        BaseLightTranslationBase evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustTranslation(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }
        // Adjusting a YEETed node un-yeets it before adjusting. Otherwise you'd just be invisibly messing with your un-yeet value while just showing YEET the whole time.
        if (GLSEventTranslationCommand.IsYeet(evt.Translation))
        {
            return GLSEventTranslationCommand.SetValue(evt, evt.Translation + GLSEventTranslationCommand.YeetOffset, inPlace) != null;
        }

        var value = CMMath.RoundToDecimals(evt.Translation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentTranslationPrecision() / 100f)));
        return GLSEventTranslationCommand.SetValue(evt, value, inPlace) != null;
    }

    public static void ToggleTranslationYeet(InputAction.CallbackContext context, BaseLightTranslationBase evt)
    {
        if (!context.performed || evt == null || !InputChordOwnership.Owns(context))
        {
            return;
        }

        GLSEventTranslationCommand.ToggleYeet(evt);
    }

    public static bool AdjustFloatFx(
        InputAction.CallbackContext context,
        BaseFxEventFloat evt,
        ScrollPrecisionController precision,
        bool inPlace = false)
    {
        if (!context.performed || evt == null || (!inPlace && !InputChordOwnership.Owns(context)))
        {
            return false;
        }

        if (!inPlace && GLSSelectionHoverMutation.TryApply(
                evt,
                node => AdjustFloatFx(context, node, precision, inPlace: true),
                out var selectionChanged))
        {
            return selectionChanged;
        }

        var value = CMMath.RoundToDecimals(evt.Value + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentFloatFXPrecision() / 100f)));
        return GLSEventFloatFXCommand.SetValue(evt, value, inPlace) != null;
    }

    // Match the ring zoom precision ladder from the Basic Event zoom tweaks.
    private static float GetStrobeIntervalChromaStep(ScrollPrecisionController precision)
        => precision.CurrentPrecision switch
        {
            ScrollPrecision.Low => 1f,
            ScrollPrecision.Medium => 0.25f,
            ScrollPrecision.High => 0.05f,
            _ => 0.01f
        };

    // Keep the official editor's easing order, with each standard InOut curve after its Beat Saber variant.
    private static readonly EaseType[] EditorAndTrueInOutEasingValues =
    {
        EaseType.None,
        EaseType.Linear,
        EaseType.InQuadratic,
        EaseType.OutQuadratic,
        EaseType.InOutQuadratic,
        EaseType.InCircular,
        EaseType.OutCircular,
        EaseType.InOutCircular,
        EaseType.InBack,
        EaseType.OutBack,
        EaseType.BeatSaberInOutBack,
        EaseType.InOutBack,
        EaseType.InElastic,
        EaseType.OutElastic,
        EaseType.BeatSaberInOutElastic,
        EaseType.InOutElastic,
        EaseType.InBounce,
        EaseType.OutBounce,
        EaseType.BeatSaberInOutBounce,
        EaseType.InOutBounce
    };

    private static readonly EaseType[] AllEasingValues = EditorAndTrueInOutEasingValues
        .Concat(((EaseType[])Enum.GetValues(typeof(EaseType)))
            .Where(v => !EditorAndTrueInOutEasingValues.Contains(v)))
        .ToArray();

    private static readonly EaseType[] StrobeFadeEasingValues = new[]
        {
            EaseType.None,
            EaseType.InOutCircular
        }
        .Concat(AllEasingValues.Where(v => v != EaseType.None && v != EaseType.InOutCircular))
        .ToArray();

    private static int GetNextEasingValue(
        int currentEasing,
        InputAction.CallbackContext context,
        EaseType[] values)
    {
        var index = Array.IndexOf(values, (EaseType)currentEasing);
        return (int)values[((index < 0 ? 0 : index) + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + values.Length) % values.Length];
    }
}
