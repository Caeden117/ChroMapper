using System;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

public static class GLSEventColorCommand
{
    public static BaseLightColorBase SetColor(BaseLightColorBase evt, int value)
    {
        if (evt.Color == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Color = value;
        newEvt.CustomColor = null;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorColor);
    }

    public static BaseLightColorBase SetBrightness(
        BaseLightColorBase evt,
        float value,
        bool inPlace = false)
    {
        if (Mathf.Approximately(evt.Brightness, value))
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Brightness = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorBrightness,
            inPlace);
    }

    public static BaseLightColorBase SetBrightnessAndEasing(BaseLightColorBase evt, float value, EaseType ease)
    {
        if (Mathf.Approximately(evt.Brightness, value) && evt.Easing == (int)ease)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Brightness = value;
        newEvt.Easing = (int)ease;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorBrightnessAndEasing);
    }

    public static BaseLightColorBase SetUsePrevious(BaseLightColorBase evt, int value)
    {
        if (evt.UsePrevious == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.UsePrevious = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorUsePrevious);
    }

    public static BaseLightColorBase SetEasing(BaseLightColorBase evt, int value)
    {
        if (evt.Easing == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.Easing = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorEasing);
    }

    public static BaseLightColorBase SetColorEasing(
        BaseLightColorBase evt,
        int easing,
        int? colorEasing,
        bool inPlace = false)
    {
        if (evt.Easing == easing && evt.ChromaColorEasing == colorEasing)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Easing = easing;
        newEvt.ChromaColorEasing = colorEasing;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorEasing,
            inPlace);
    }

    public static BaseLightColorBase SetStrobeColorEasing(
        BaseLightColorBase evt,
        int? value,
        bool inPlace = false)
    {
        var promotedEasing = value.HasValue ? (int)EaseType.Linear : evt.Easing;
        if (evt.ChromaStrobeColorEasing == value && evt.Easing == promotedEasing)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.ChromaStrobeColorEasing = value;
        if (value.HasValue && newEvt.Easing == (int)EaseType.None)
            newEvt.Easing = promotedEasing;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeColorEasing,
            inPlace);
    }

    public static BaseLightColorBase SetStrobePulseWidth(
        BaseLightColorBase evt,
        float value,
        bool inPlace = false)
    {
        value = Mathf.Clamp01(value);
        if (evt.ChromaStrobePulseWidth == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.ChromaStrobePulseWidth = value;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobePulseWidth,
            inPlace);
    }

    public static BaseLightColorBase SetStrobeFadeEasing(
        BaseLightColorBase evt,
        int strobeFade,
        int? strobeEasing,
        bool inPlace = false)
    {
        if (evt.StrobeFade == strobeFade && evt.ChromaStrobeEasing == strobeEasing)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.StrobeFade = strobeFade;
        newEvt.ChromaStrobeEasing = strobeFade == 1 ? strobeEasing : null;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeFade,
            inPlace);
    }

    /// <summary>
    /// Sets a non-chroma Beat Saber 1/N frequency for strobes, nulling the chroma property.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static BaseLightColorBase SetStrobeFrequencyOnly(
        BaseLightColorBase evt,
        int value,
        bool inPlace = false)
    {
        if (evt.Frequency == value && evt.ChromaStrobeInterval == null)
            return null;
        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.Frequency = value;
        newEvt.ChromaStrobeInterval = null;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorFrequency,
            inPlace);
    }

    /// <summary>
    /// Sets a ChromaGLS strobeInterval frequency for strobe, and sets Frequency appropriately to the closest matching OEM frequency.
    /// If null, resets everything to no strobe and drops the ChromaGLS strobeInterval property.
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static BaseLightColorBase SetStrobeIntervalAndClosestFrequency(
        BaseLightColorBase evt,
        float? value,
        bool inPlace = false)
    {
        if (value is not null)
        {
            var expectedFrequency = value < 0.75f ? 2 : 1;
            if (evt.ChromaStrobeInterval is { } existing && Mathf.Approximately(existing, value.Value) && evt.Frequency == expectedFrequency)
                return null;
        }

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.ChromaStrobeInterval = value;
        if (value is not null)
            newEvt.Frequency = value < 0.75f ? 2 : 1;
        else
            newEvt.Frequency = 0;  // Reset back to no strobe when passing through strobeInterval 0.5
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorFrequency,
            inPlace);
    }

    public static BaseLightColorBase SetStrobeBrightness(
        BaseLightColorBase evt,
        float value,
        bool inPlace = false)
    {
        if (Mathf.Approximately(evt.StrobeBrightness, value))
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.StrobeBrightness = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeBrightness,
            inPlace);
    }

    public static BaseLightColorBase SetStrobeFade(BaseLightColorBase evt, int value)
    {
        if (evt.StrobeFade == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        newEvt.StrobeFade = value;
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorStrobeFade);
    }

    public static BaseLightColorBase SetLerpType(
        BaseLightColorBase evt,
        BasicEventColorLerpType value,
        bool inPlace = false)
    {
        if (evt.CustomLerpType == value)
            return null;

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt, inPlace);
        newEvt.CustomLerpType = value;
        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            ActionMergeType.ModifyGLSColorLerpType,
            inPlace);
    }

    public static BaseLightColorBase SetColorDistributions(BaseLightColorBase evt, string[] value, bool strobe)
    {
        value ??= Array.Empty<string>();
        var existing = strobe
            ? evt.StrobeColorDistributions
            : evt.ColorDistributions;
        if (existing.SequenceEqual(value))
        {
            return null;
        }

        var (newGroup, newEvt) = GLSCommonCommand.CopyGroupFrom(evt);
        if (strobe)
        {
            newEvt.StrobeColorDistributions = value;
        }
        else
        {
            newEvt.ColorDistributions = value;
        }

        newEvt.WriteCustom();
        return GLSCommonCommand.TriggerModifyEventAction(
            evt.EventBoxGroupData,
            newGroup,
            newEvt,
            strobe
                ? ActionMergeType.ModifyGLSStrobeColorDistributions
                : ActionMergeType.ModifyGLSColorDistributions);
    }
}
