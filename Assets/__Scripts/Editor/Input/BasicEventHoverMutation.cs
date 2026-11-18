using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Shared;
using UnityEngine;

public static class BasicEventHoverMutation
{
    public static ActionMergeType? TweakMain(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision,
        TrackDefinitionsSO definitions,
        bool controlHeld,
        bool selectHeld)
    {
        var isRingRot = IsRingRotationEvent(evt, definitions);
        var isRingZoom = IsRingZoomEvent(evt, definitions);

        if (IsLaserSpeedEvent(evt, definitions))
        {
            if (selectHeld)
                return null;

            // Preserve integer values in i while using custom speed only when fractional precision is required.
            return TweakLaserSpeed(evt, modifier, precision);
        }

        if (isRingRot)
        {
            if (selectHeld)
                return null;

            // The first Alt+Scroll establishes a 90-degree magnitude and an explicit direction without also applying precision.
            if (!evt.CustomRingRotation.HasValue)
            {
                evt.CustomRingRotation = ScrollPrecisionController.DefaultRingRotation;
                evt.CustomDirection = modifier > 0
                    ? 1
                    : 0;

                return ActionMergeType.RingRotationValueTweak;
            }

            // Negative magnitude plus an explicit direction is equivalent to positive magnitude with the direction inverted.
            if (evt.CustomRingRotation.Value < 0f
                && evt.CustomDirection.HasValue)
            {
                evt.CustomRingRotation = -evt.CustomRingRotation.Value;
                evt.CustomDirection = evt.CustomDirection.Value == 1
                    ? 0
                    : 1;
            }

            TweakCustomFloat(
                evt,
                modifier,
                evt.CustomRingRotation,
                precision.GetCurrentRotationPrecision(),
                0f,
                false,
                ScrollPrecisionController.DefaultRingRotation,
                v => evt.CustomRingRotation = v);
            return ActionMergeType.RingRotationValueTweak;
        }

        if (isRingZoom)
        {
            if (selectHeld)
                return null;

            // SmoothStepRingZoom only applies to The Second's ring.
            if (IsSmoothStepRingZoomEvent(evt, definitions))
            {
                return TweakSmoothStepRingZoom(evt, modifier, precision);
            }

            TweakCustomFloat(
                evt,
                modifier,
                evt.CustomStep,
                precision.GetCurrentRingZoomStepPrecision(),
                null,
                false,
                ScrollPrecisionController.DefaultRingZoomStep,
                v => evt.CustomStep = v);
            return ActionMergeType.RingZoomStepTweak;
        }

        if (definitions.GetBasicOrDefault(evt.Type).Kind == BasicEventKind.Lights
            && !evt.IsColorBoostEvent())
        {
            if (controlHeld || selectHeld)
                return null;

            var prec = precision.GetCurrentBrightnessPrecision() / 100f;
            var value = CMMath.RoundToDecimals(evt.FloatValue + (modifier * prec));
            evt.FloatValue = Mathf.Max(0f, value);

            return ActionMergeType.EventMainTweak;
        }

        if (controlHeld || selectHeld)

            return null;

        if (evt.IsColorBoostEvent())
            evt.Value = evt.Value == 0 ? 1 : 0;
        else if (evt.IsBpmEvent())
        {
            evt.FloatValue += modifier;
            if (evt.FloatValue < 1)
                evt.FloatValue = 1;

        }
        else
        {
            evt.Value += modifier;
            if (evt.Value < 0)
                evt.Value = 0;

        }

        return ActionMergeType.EventMainTweak;
    }

    public static ActionMergeType? TweakCtrlAlt(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision,
        TrackDefinitionsSO definitions)
    {
        var eventComponents = definitions.GetBasicOrDefault(evt.Type).Components;
        var isLaserSpeed = eventComponents.HasFlag(BasicEventComponent.LightRotation);

        if (isLaserSpeed)
        {
            // Chroma uses unlocked rotation when lockRotation is absent, so never serialize false.
            evt.CustomLockRotation = evt.CustomLockRotation == true
                ? null
                : true;
            return ActionMergeType.LaserLockRotationTweak;
        }
        else if (IsRingRotationEvent(evt, definitions))
        {
            return TweakRingSpeed(evt, modifier, precision);
        }
        else if (IsRingZoomEvent(evt, definitions))
        {
            // SmoothStepRingZoom only applies to The Second's legacy ring.
            if (IsSmoothStepRingZoomEvent(evt, definitions))
            {
                return null;
            }
            return TweakRingSpeed(evt, modifier, precision);
        }
        else if (definitions.GetBasicOrDefault(evt.Type).Kind == BasicEventKind.Lights)
        {
            return TweakLightNodeType(evt, modifier);
        }

        return null;
    }

    public static ActionMergeType? TweakCtrlShift(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision,
        TrackDefinitionsSO definitions,
        List<string> easingValues)
    {
        var isRingRot = IsRingRotationEvent(evt, definitions);
        var isRingZoom = IsRingZoomEvent(evt, definitions);

        if (isRingRot)
        {
            TweakCustomFloat(
                evt,
                modifier,
                evt.CustomProp,
                precision.GetCurrentRingRotationPropagationPrecision(),
                0f,
                false,
                ScrollPrecisionController.DefaultRingPropagation,
                v => evt.CustomProp = v);
            return ActionMergeType.RingPropagationTweak;
        }
        else if (isRingZoom)
        {
            // Negative Chroma ring-zoom steps move toward the player and are explicitly supported, so don't clamp negatives to 0.
            TweakCustomFloat(
                evt,
                modifier,
                evt.CustomStep,
                precision.GetCurrentRingZoomStepPrecision() / 10f,
                null,
                false,
                ScrollPrecisionController.DefaultRingZoomStep,
                v => evt.CustomStep = v);
            return ActionMergeType.RingStepTweak;
        }
        else if (definitions.GetBasicOrDefault(evt.Type).Kind == BasicEventKind.Lights)
        {
            return TweakEasing(evt, modifier, easingValues);
        }

        return null;
    }

    public static ActionMergeType? TweakRingStep(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision)
    {
        TweakCustomFloat(
            evt,
            modifier,
            evt.CustomStep,
            precision.GetCurrentRingRotationStepPrecision(),
            null,
            false,
            ScrollPrecisionController.DefaultRingRotationStep,
            v => evt.CustomStep = v);
        return ActionMergeType.RingStepTweak;
    }

    private static ActionMergeType TweakRingSpeed(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision)
    {
        // Ring rotation and ring zoom share Chroma's speed multiplier and its zoom precision ladder.
        TweakCustomFloat(
            evt,
            modifier,
            evt.CustomSpeed,
            precision.GetCurrentRingZoomStepPrecision(),
            0f,
            false,
            ScrollPrecisionController.DefaultRingSpeed,
            v => evt.CustomSpeed = v);
        return ActionMergeType.RingSpeedTweak;
    }

    private static ActionMergeType? TweakLaserSpeed(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision)
    {
        var currentSpeed = evt.CustomSpeed ?? evt.Value;
        var speed = Mathf.Max(0f, CMMath.RoundToDecimals(currentSpeed + (modifier * precision.GetCurrentLaserSpeedPrecision())));
        var integerSpeed = Mathf.RoundToInt(speed);

        evt.Value = integerSpeed;
        // Keep integer speeds in i so exporting an exact value removes the fractional override.
        evt.CustomSpeed = Mathf.Approximately(speed, integerSpeed)
            ? null
            : speed;
        return ActionMergeType.LaserSpeedTweak;
    }

    private static ActionMergeType? TweakSmoothStepRingZoom(
        BaseEvent evt,
        int modifier,
        ScrollPrecisionController precision)
    {
        var currentStep = evt.CustomStep ?? evt.Value;
        var step = CMMath.RoundToDecimals(currentStep + (modifier * precision.GetCurrentRingZoomStepPrecision()));
        var roundedStep = Mathf.RoundToInt(step);
        // The Second needs a native 0..9 fallback in i when customData.step carries a fractional or extended value.
        var integerStep = Mathf.Clamp(roundedStep, 0, 9);
        var isOemInteger = Mathf.Approximately(step, roundedStep)
            && roundedStep >= 0
            && roundedStep <= 9;

        evt.Value = integerStep;
        evt.CustomStep = isOemInteger
            ? null
            : step;
        return ActionMergeType.RingZoomStepTweak;
    }

    public static ActionMergeType? TweakLightNodeType(BaseEvent evt, int modifier)
    {
        const int nodeTypeCount = 4;
        var colorBase = evt.Value switch
        {
            > 0 and <= 4 => 0,
            > 4 and <= 8 => 4,
            > 8 and <= 12 => 8,
            _ => -1
        };
        if (colorBase < 0)
            return null;

        // Node hover cycles the four light values without touching the RGB/HSV interpolation mode.
        var nodeType = evt.Value - colorBase - 1;
        var nextNodeType = (nodeType + modifier + nodeTypeCount) % nodeTypeCount;
        evt.Value = colorBase + nextNodeType + 1;

        return ActionMergeType.LightLerpTypeTweak;
    }

    public static ActionMergeType? TweakLerpType(BaseEvent evt, int modifier)
    {
        var current = (int)BasicEventColorLerp.FromSerializedName(evt.CustomLerpType);
        var next = (BasicEventColorLerpType)((current + modifier + 3) % 3);
        // Basic Event ribbons serialize RGB as an absent lerpType and HSV as the explicit alternate.
        evt.CustomLerpType = next switch
        {
            BasicEventColorLerpType.LegacyHSV => "HSV",
            BasicEventColorLerpType.TrueHSV => "TrueHSV",
            _ => null
        };

        return ActionMergeType.LightLerpTypeTweak;
    }

    public static ActionMergeType? TweakEasing(BaseEvent evt, int modifier, List<string> easingValues)
    {
        var current = evt.CustomEasing ?? "easeLinear";
        var index = easingValues.IndexOf(current);
        if (index < 0)
            index = 0;

        index = (index + modifier + easingValues.Count) % easingValues.Count;
        var newEasing = easingValues[index];
        evt.CustomEasing = newEasing == "easeLinear"
            ? null
            : newEasing;

        return ActionMergeType.LightEasingTweak;
    }

    private static void TweakCustomFloat(
        BaseEvent evt,
        int modifier,
        float? current,
        float step,
        float? min,
        bool logarithmic,
        float defaultValue,
        Action<float?> setter)
    {
        var value = current ?? defaultValue;
        if (logarithmic)
            step = GetLogarithmicStep(value);

        value += modifier * step;
        if (min.HasValue)
            value = Mathf.Max(min.Value, value);

        setter(CMMath.RoundToDecimals(value));
    }

    private static float GetLogarithmicStep(float value)
    {
        var abs = Mathf.Abs(value);
        if (abs < 0.1f)
            return 0.02f;

        if (abs < 0.3f)
            return 0.04f;

        if (abs < 1.0f)
            return 0.1f;

        if (abs < 2.0f)
            return 0.2f;

        if (abs < 5.0f)
            return 0.5f;

        return 1.0f;
    }

    public static bool MatchesType(BaseEvent hovered, BaseEvent candidate, TrackDefinitionsSO definitions) =>
        GetSelectionType(hovered, definitions) == GetSelectionType(candidate, definitions);

    // Ring capabilities and light kind identify the editing type across lanes. Other events keep their numeric type.
    private static (BasicEventComponent Component, BasicEventKind Kind, int Type) GetSelectionType(
        BaseEvent evt,
        TrackDefinitionsSO definitions)
    {
        if (evt.IsColorBoostEvent() || evt.IsBpmEvent())
            return (BasicEventComponent.None, BasicEventKind.Generic, evt.Type);

        var definition = definitions.GetBasicOrDefault(evt.Type);
        if (IsLaserSpeedEvent(evt, definitions))
            return (BasicEventComponent.LightRotation, BasicEventKind.Generic, 0);

        if (IsRingRotationEvent(evt, definitions))
            return (BasicEventComponent.RingRotation, BasicEventKind.Generic, 0);

        if (IsRingZoomEvent(evt, definitions))
            return (BasicEventComponent.RingZoom, BasicEventKind.Generic, 0);

        return definition.Kind == BasicEventKind.Lights
            ? (BasicEventComponent.None, BasicEventKind.Lights, 0)
            : (BasicEventComponent.None, definition.Kind, evt.Type);
    }

    private static bool IsRingRotationEvent(BaseEvent evt, TrackDefinitionsSO definitions) =>
        definitions.GetBasicOrDefault(evt.Type).Components.HasFlag(BasicEventComponent.RingRotation);

    private static bool IsRingZoomEvent(BaseEvent evt, TrackDefinitionsSO definitions) =>
        definitions.GetBasicOrDefault(evt.Type).Components.HasFlag(BasicEventComponent.RingZoom)
        || IsSmoothStepRingZoomEvent(evt, definitions);

    private static bool IsSmoothStepRingZoomEvent(BaseEvent evt, TrackDefinitionsSO definitions) =>
        definitions.GetBasicOrDefault(evt.Type).Components.HasFlag(BasicEventComponent.SmoothStepRingZoom);

    private static bool IsLaserSpeedEvent(BaseEvent evt, TrackDefinitionsSO definitions) =>
        definitions.GetBasicOrDefault(evt.Type).Components.HasFlag(BasicEventComponent.LightRotation);
}
