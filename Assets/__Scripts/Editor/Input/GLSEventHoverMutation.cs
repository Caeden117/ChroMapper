using System;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.InputSystem;

public static class GLSEventHoverMutation
{
    // Keep inner and outer GLS hover mutations identical while each controller owns target resolution.
    public static void AdjustColorBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.Brightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetBrightness(evt, Mathf.Max(0f, value));
    }

    public static void AdjustColorFrequency(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null || precision == null)
            return;

        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        if (delta == 0)
            return;

        // customData.strobeInterval is a period in beats per cycle; use the ring zoom precision ladder for tweaks.
        if (evt.ChromaStrobeInterval is { } interval)
        {
            var newInterval = Mathf.Round((interval - (delta * GetStrobeIntervalChromaStep(precision))) * 1000f) / 1000f;
            // Do not allow a zero or negative interval; keep a floor so 1/interval remains finite.
            if (newInterval <= 0f)
                newInterval = 0.01f;
            if (newInterval <= 0.5f && delta == 1)
            {
                // If we scrolled strobe interval lower and we're at 1/2 or below, swap back to OEM fractions.
                GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, null);
            }
            else
            {
                GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, newInterval);
            }


            return;
        }

        // Native frequency is cycles per beat, displayed as 1/N.
        var newFrequency = evt.Frequency + delta;
        if (newFrequency < 0)
            newFrequency = 0;
        if (evt.Frequency == 0 && delta == -1)
        {
            // Scrolling past 1/1 switches to the custom float interval starting at 1.0 beats per cycle.
            GLSEventColorCommand.SetStrobeIntervalAndClosestFrequency(evt, 1.0f);
        }
        else
        {
            GLSEventColorCommand.SetStrobeFrequencyOnly(evt, newFrequency);
        }
    }

    public static void AdjustColorStrobeBrightness(InputAction.CallbackContext context, BaseLightColorBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var delta = context.GetScrollDirection(Settings.Instance.InvertScrollEventValue);
        var value = Mathf.Round((evt.StrobeBrightness + (delta * (precision.GetCurrentBrightnessPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventColorCommand.SetStrobeBrightness(evt, Mathf.Max(0f, value));
    }

    // Shift+scroll must not also toggle Strobe Fade when a more specific GLS color chord includes Ctrl or Alt.
    public static bool ToggleColorStrobeFade(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed
            || evt == null
            || Keyboard.current.ctrlKey.isPressed
            || Keyboard.current.altKey.isPressed)
        {
            return false;
        }

        return GLSEventColorCommand.SetStrobeFade(evt, evt.StrobeFade == 1 ? 0 : 1) != null;
    }

    // The authored input action owns chord disambiguation; this helper only applies its resolved mutation.
    public static void AdjustColorEasing(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (!context.performed || evt == null) return;

        // GLS color events currently serialize only instant (None) or interpolated (Linear) transitions.
        var easing = evt.Easing == (int)EaseType.None
            ? EaseType.Linear
            : EaseType.None;
        GLSEventEasingCommand.SetEasing(evt, (int)easing);
    }

    public static void MirrorColor(InputAction.CallbackContext context, BaseLightColorBase evt)
    {
        if (context.performed && evt != null) GLSEventColorCommand.SetColor(evt, (evt.Color + 1) % 2);
    }

    public static void AdjustRotation(InputAction.CallbackContext context, BaseLightRotationBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Rotation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * precision.GetCurrentRotationPrecision())) * 1_000f) / 1_000f;
        GLSEventRotationCommand.SetValue(evt, Mathf.Repeat(value, 360f));
    }

    public static void AdjustRotationLoop(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        GLSEventRotationCommand.SetLoop(evt, (evt.Loop + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + 5) % 5);
    }

    public static void AdjustRotationEasing(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.EaseType, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
    }

    public static void CycleRotationDirection(InputAction.CallbackContext context, BaseLightRotationBase evt)
    {
        if (!context.performed || evt == null) return;
        var values = (LightRotationDirection[])Enum.GetValues(typeof(LightRotationDirection));
        var index = Array.IndexOf(values, (LightRotationDirection)evt.Direction);
        GLSEventRotationCommand.SetDirection(evt, values[((index < 0 ? 0 : index) + 1) % values.Length]);
    }

    public static void AdjustTranslation(InputAction.CallbackContext context, BaseLightTranslationBase evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        // GLSTranslationYeetTest requires the first Alt-scroll pulse to restore a YEET node without also applying its direction.
        if (GLSEventTranslationCommand.IsYeet(evt.Translation))
        {
            GLSEventTranslationCommand.SetValue(evt, evt.Translation + GLSEventTranslationCommand.YeetOffset);
            return;
        }

        var value = Mathf.Round((evt.Translation + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentTranslationPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventTranslationCommand.SetValue(evt, value);
    }

    public static void ToggleTranslationYeet(InputAction.CallbackContext context, BaseLightTranslationBase evt)
    {
        if (!context.performed || evt == null)
        {
            return;
        }

        GLSEventTranslationCommand.ToggleYeet(evt);
    }

    public static void AdjustTranslationEasing(InputAction.CallbackContext context, BaseLightTranslationBase evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.EaseType, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
    }

    public static void AdjustFloatFx(InputAction.CallbackContext context, BaseFxEventFloat evt, ScrollPrecisionController precision)
    {
        if (!context.performed || evt == null) return;
        var value = Mathf.Round((evt.Value + (context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) * (precision.GetCurrentFloatFXPrecision() / 100f))) * 1_000f) / 1_000f;
        GLSEventFloatFXCommand.SetValue(evt, value);
    }

    public static void AdjustFloatFxEasing(InputAction.CallbackContext context, BaseFxEventFloat evt)
    {
        if (!context.performed || evt == null) return;
        var nextEasing = GetNextEasing(evt.Easing, context);
        GLSEventEasingCommand.SetEasing(evt, nextEasing);
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

    // All GLS node types cycle the same ordered easing list so inner and outer hover controls remain consistent.
    private static int GetNextEasing(int currentEasing, InputAction.CallbackContext context)
    {
        var values = (EaseType[])Enum.GetValues(typeof(EaseType));
        var index = Array.IndexOf(values, (EaseType)currentEasing);
        return (int)values[((index < 0 ? 0 : index) + context.GetScrollDirection(Settings.Instance.InvertScrollEventValue) + values.Length) % values.Length];
    }
}
