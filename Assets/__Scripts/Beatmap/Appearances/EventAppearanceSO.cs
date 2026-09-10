using System;
using System.Globalization;
using System.Text;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/Event Appearance SO", fileName = "EventAppearanceSO")]
    public class EventAppearanceSO : ScriptableObject
    {
        // Keep all Basic and GLS node geometry on the same final/preview scale contract.
        public const float FinalNodeScale = 0.75f;
        public const float PreviewNodeScale = 0.6f;

        // Keep final and preview node bottoms aligned even though their rendered heights differ.
        public static float GetGroundedNodeCenterY(bool final)
        {
            var scale = final ? FinalNodeScale : PreviewNodeScale;
            return BeatmapConstant.EventNodeGroundedCenterY - ((FinalNodeScale - scale) / 2f);
        }

        [Space(5)]
        [Header("Default Colors")]
        public Color RedColor;
        public Color BlueColor;
        public Color WhiteColor = new(0.7264151f, 0.7264151f, 0.7264151f);
        public Color RedBoostColor;
        public Color BlueBoostColor;
        public Color WhiteBoostColor = new(0.7264151f, 0.7264151f, 0.7264151f);
        public Color OffColor;

        [Header("Other Event Colors")]
        public Color RingEventsColor;
        /// <summary>
        /// Used for clockwise ring rotations and positive step ring zoom, and Y GLS rotation / translations.
        /// </summary>
        public Color RingEventsClockwiseColor = new(0.75f, 0.75f, 0.75f);
        /// <summary>
        /// Used for counter-clockwise ring rotations and positive step ring zoom, and Y GLS rotation / translations.
        /// </summary>
        public Color RingEventsCounterClockwiseColor = new(0.35f, 0.35f, 0.35f);

        [Tooltip("Example: Ring rotate/Ring zoom/Light speed change events")]
        public Color OtherColor;

        public void SetAppearance(
            EventContainer e,
            bool final = true,
            bool boost = false,
            BaseEvent transitionTarget = null)
        {
            var color = Color.white;
            var trackDef = e.TracksDefinition.GetBasicOrDefault(e.EventData.Type);
            e.UpdateAlpha(final ? 1.0f : 0.6f, false);
            e.UpdateScale(final ? FinalNodeScale : PreviewNodeScale);
            e.UpdateOffset(e.AlternateShader ? -0.5f : 0f);
            // Component metadata distinguishes laser-speed IntValue tracks from unrelated integer events.
            var isLaserSpeed = trackDef.Components.HasFlag(BasicEventComponent.LightRotation);
            if (trackDef.Kind == BasicEventKind.IntValue)
                e.UpdateTextDisplay(true, GetIntValueText(e.EventData, isLaserSpeed));
            else
                e.UpdateTextDisplay(false);

            // Ring behavior comes from environment component metadata because event-type numbers can be repurposed or mixed.
            var isRingRotation = trackDef.Components.HasFlag(BasicEventComponent.RingRotation);
            // SmoothStepRingZoom only applies to The Second's legacy ring right now.
            var isSmoothStepRingZoom = trackDef.Components.HasFlag(BasicEventComponent.SmoothStepRingZoom);
            var isRingZoom = trackDef.Components.HasFlag(BasicEventComponent.RingZoom) || isSmoothStepRingZoom;
            var isRingEvent = isRingRotation || isRingZoom;
            if (isRingEvent)
            {
                e.UseBlockModel = true;
                Color ringColor;
                // Ring nodes retain their dedicated transform colors even when their simulated operations overlap.
                if (isRingZoom && e.EventData.CustomStep < 0)
                {
                    // Negative ring zoom (towards the player) uses the shared dark transform color.
                    ringColor = RingEventsCounterClockwiseColor;
                }
                else if (isRingRotation)
                {
                    // Basic Event ring rotation direction is explicit custom data, so use it to distinguish CW/CCW/random.
                    ringColor = e.EventData.CustomDirection switch
                    {
                        1 => RingEventsClockwiseColor,
                        0 => RingEventsCounterClockwiseColor,
                        _ => RingEventsColor,
                    };
                }
                else
                {
                    ringColor = RingEventsColor;
                }

                e.ChangeColorA(ringColor, false);
                e.ChangeColorB(ringColor, false);
                e.ChangeFadeSize(0.75f, false);
                e.UpdateGradientRendering();
                var ringText = GetNonLightText(e.EventData, trackDef, isSmoothStepRingZoom);
                e.UpdateTextDisplay(ringText.Length > 0, ringText);
                e.UpdateMaterials();
                return;
            }

            if (trackDef.Kind != BasicEventKind.Lights)
            {
                e.UseBlockModel = true;
                // DenseNormalLanesForwardUnloadAndBackwardScrubReloadEveryNodeAndRibbon exposed that non-light nodes
                // inherited _FadeSize from a prior pooled owner. Initialize the one per-container property here; Color
                // Boost retains its intentionally narrower override below without any pool scan or cache rebuild.
                e.ChangeFadeSize(0.75f, false);
                if (e.EventData.Type == (int)EventTypeValue.ColorBoostEventType)
                {
                    if (e.EventData.Value == 1)
                    {
                        e.ChangeColorA(RedBoostColor, false);
                        e.ChangeColorB(BlueBoostColor, false);
                    }
                    else
                    {
                        e.ChangeColorA(RedColor, false);
                        e.ChangeColorB(BlueColor, false);
                    }

                    e.ChangeFadeSize(0.25f, false);
                }
                else if (trackDef.Kind == BasicEventKind.None)
                {
                    e.ChangeColorA(RingEventsColor, false);
                    e.ChangeColorB(RingEventsColor, false);
                }
                else if (isLaserSpeed)
                {
                    // Laser rotation direction uses the same light/dark/random visual language as ring rotation.
                    var laserColor = e.EventData.CustomDirection switch
                    {
                        1 => RingEventsClockwiseColor,
                        0 => RingEventsCounterClockwiseColor,
                        _ => RingEventsColor,
                    };
                    e.ChangeColorA(laserColor, false);
                    e.ChangeColorB(laserColor, false);
                }
                else
                {
                    e.ChangeColorA(OtherColor, false);
                    e.ChangeColorB(OtherColor, false);
                }

                // Heck lockRotation only suppresses the reset performed by its attached laser-speed event, so it has
                // no transition endpoint and must not create a ribbon or an interaction collider toward the next event.
                e.UpdateGradientRendering();

                if (trackDef.Kind != BasicEventKind.IntValue)
                {
                    var text = GetNonLightText(e.EventData, trackDef);
                    e.UpdateTextDisplay(text.Length > 0, text);
                }
                e.UpdateMaterials();
                return;
            }

            if (e.EventData.Value >= ColourManager.RgbintOffset)
            {
                color = ColourManager.ColourFromInt(e.EventData.Value);
                e.UpdateAlpha(final ? 0.9f : 0.6f, false);
            }
            else if (e.EventData.IsOff)
                color = OffColor;
            else if (e.EventData.IsBlue)
                color = boost ? BlueBoostColor : BlueColor;
            else if (e.EventData.IsRed)
                color = boost ? RedBoostColor : RedColor;
            else if (e.EventData.IsWhite) color = boost ? WhiteBoostColor : WhiteColor;

            if (Settings.Instance.EmulateChromaLite
                && e.EventData.CustomColor != null
                && !e.EventData.IsOff
                && !e.EventData.IsWhite) // White overrides Chroma
            {
                color = e.EventData.CustomColor.Value;
            }

            var ribbonStartColor = color;

            // Display floatValue only where used
            if (trackDef.Kind == BasicEventKind.Lights
                && e.EventData.Value != 0)
            {
                if (Settings.Instance.DisplayFloatValueText)
                {
                    if (!Mathf.Approximately(e.EventData.FloatValue, 1f))
                    {
                        var text = e.EventData.IsTransition
                            ? $"T{Mathf.RoundToInt(e.EventData.FloatValue * 100)}"
                            : $"{Mathf.RoundToInt(e.EventData.FloatValue * 100)}";
                        e.UpdateTextDisplay(true, text);
                    }
                    else if (e.EventData.IsTransition)
                        e.UpdateTextDisplay(true, "T");
                    else
                        e.UpdateTextDisplay(false);
                }

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(OffColor, color, 0.25f);
                color = Color.Lerp(clampedOffColor, color, e.EventData.FloatValue);
            }

            e.UseBlockModel = false;
            e.ChangeColorA(color, false);
            e.ChangeColorB(OffColor, false);
            switch (e.EventData.Value)
            {
                case (int)LightValue.Off:
                    e.ChangeColorB(OffColor, false);
                    e.ChangeColorA(OffColor, false);
                    break;
                case (int)LightValue.BlueOn:
                case (int)LightValue.RedOn:
                case (int)LightValue.WhiteOn:
                    e.ChangeColorB(color, false);
                    break;
                case (int)LightValue.BlueFlash:
                case (int)LightValue.RedFlash:
                case (int)LightValue.WhiteFlash:
                    e.ChangeColorA(OffColor, false);
                    e.ChangeColorB(color, false);
                    break;
                case (int)LightValue.BlueFade:
                case (int)LightValue.RedFade:
                case (int)LightValue.WhiteFade:
                    break;
                case (int)LightValue.BlueTransition:
                case (int)LightValue.RedTransition:
                case (int)LightValue.WhiteTransition:
                    e.ChangeColorB(color, false);
                    break;
            }

            e.ChangeFadeSize(0.75f, false);

            // At this point, next Event must be a light event.
            Color? nextColor = null;
            Color? ribbonEndColor = null;
            // Surface serialized Basic Event easing even without a following transition.
            var easing = e.EventData.CustomEasing ?? "easeLinear";
            // Fall back to the serialized easing suffix so unknown custom easing labels stay inspectable.
            var easingLabel = e.EventData.CustomEasing != null ? GetShortEasingName(easing) : null;
            // Classify serialized lerpType once for node labels and ribbon shader dispatch.
            var colorLerpType = BasicEventColorLerp.FromSerializedName(e.EventData.CustomLerpType);
            // PasteUndo_OnIntoOnTransition previews have no grid owner, so only finalized callers override EventData.Next.
            var nextEvent = transitionTarget ?? e.EventData.Next;
            if (!e.EventData.IsFade && !e.EventData.IsFlash && nextEvent != null && nextEvent.IsTransition)
            {
                if (nextEvent.IsBlue)
                    nextColor = boost ? BlueBoostColor : BlueColor;
                else if (nextEvent.IsRed)
                    nextColor = boost ? RedBoostColor : RedColor;
                else if (nextEvent.IsWhite) nextColor = boost ? WhiteBoostColor : WhiteColor;

                if (Settings.Instance.EmulateChromaLite
                    && nextEvent.CustomColor != null
                    && !nextEvent.IsWhite)
                {
                    nextColor = nextEvent.CustomColor.Value;
                }

                ribbonEndColor = nextColor;

                // for clarity sake, we don't want this to be the same as off color
                var clampedOffColor = Color.Lerp(OffColor, nextColor.Value, 0.25f);
                nextColor = Color.Lerp(clampedOffColor, nextColor.Value, nextEvent.FloatValue);
                // Basic Event interpolation metadata stays on this source node while the next node marks the transition target.
            }

            if (e.EventData.CustomLightGradient != null)
            {
                easing = e.EventData.CustomLightGradient.EasingType;
                easingLabel = easing == "easeLinear" ? null : GetShortEasingName(easing);
                // Chroma legacy gradients are RGB-only and must not surface ordinary transition lerpType metadata.
                colorLerpType = BasicEventColorLerpType.RGB;
            }

            // Distinguish compatibility HSV from conventional angular HSV without changing serialized names.
            var lerpTypeLabel = colorLerpType switch
            {
                BasicEventColorLerpType.LegacyHSV => "LHSV",
                BasicEventColorLerpType.TrueHSV => "HSV",
                _ => null
            };

            var lightText = GetLightText(e.EventData, GetLightValueText(e.EventData), easingLabel, lerpTypeLabel);
            e.UpdateTextDisplay(lightText.Length > 0, lightText);

            if (Settings.Instance.VisualizeChromaGradients)
            {
                // LightIdTransitionRibbonStopsAtAllLightsNonTransitionInterrupt keeps color and length on one endpoint.
                e.UpdateGradientRendering(
                    ribbonStartColor,
                    ribbonEndColor,
                    easing,
                    colorLerpType,
                    e.EventData.FloatValue,
                    nextEvent?.FloatValue ?? 1f,
                    transitionTarget: nextEvent);
            }

            e.UpdateMaterials();
        }

        public void SetAppearance(
            RotationEventContainer e,
            bool final = true)
        {
            e.UpdateScale(final ? FinalNodeScale : PreviewNodeScale);
            e.UpdateTextDisplay(true, $"{e.EventData.Rotation}°");
        }

        private static string GetIntValueText(BaseEvent data, bool isLaserSpeed)
        {
            var speed = data.CustomSpeed ?? data.Value;
            // Basic Event laser speed displays at most one decimal place.
            var text = new StringBuilder(speed.ToString("0.#", CultureInfo.InvariantCulture));

            if (data.CustomDirection.HasValue)
                text.Append(" ").Append(DirectionText(data.CustomDirection.Value));

            if (isLaserSpeed && data.Value == 0 && data.CustomSpeed.HasValue
                && !Mathf.Approximately(data.CustomSpeed.Value, 0f))
                text.AppendLine().Append("NR");

            // Show the serialized laser rotation lock as a dedicated line beneath the speed/direction value.
            if (isLaserSpeed && data.CustomLockRotation == true)
                text.AppendLine().Append("L");

            return text.ToString();
        }

        private static string GetLightText(BaseEvent data, string existingText, string easingLabel, string lerpTypeLabel = null)
        {
            var result = existingText;
            if (!string.IsNullOrEmpty(easingLabel))
            {
                result = string.IsNullOrEmpty(result) ? easingLabel : result + "\n" + easingLabel;
            }
            if (!string.IsNullOrEmpty(lerpTypeLabel))
            {
                result = string.IsNullOrEmpty(result) ? lerpTypeLabel : result + "\n" + lerpTypeLabel;
            }
            return result;
        }

        private static string GetShortEasingName(string easing) =>
            Easing.InternalNameToShortName.TryGetValue(easing, out var shortName)
                ? shortName
                : easing.StartsWith("ease", StringComparison.Ordinal) ? easing[4..] : easing;

        private static string GetLightValueText(BaseEvent data)
        {
            if (!Settings.Instance.DisplayFloatValueText || data.Value == 0) return string.Empty;
            if (!Mathf.Approximately(data.FloatValue, 1f))
                return data.IsTransition
                    ? $"T{Mathf.RoundToInt(data.FloatValue * 100)}"
                    : $"{Mathf.RoundToInt(data.FloatValue * 100)}";
            return data.IsTransition ? "T" : string.Empty;
        }

        private static string GetNonLightText(
            BaseEvent data,
            TrackDefinitionBasic trackDef,
            bool isSmoothStepRingZoom = false)
        {
            // Prefer rotation text for mixed ring tracks because it includes the shared ring parameters.
            if (trackDef.Components.HasFlag(BasicEventComponent.RingRotation)) return GetRingRotationText(data);
            if (trackDef.Components.HasFlag(BasicEventComponent.RingZoom) || isSmoothStepRingZoom)
                return GetRingZoomText(data, isSmoothStepRingZoom);
            return string.Empty;
        }

        private static string GetRingRotationText(BaseEvent data)
        {
            var lines = new StringBuilder();
            if (data.CustomRingRotation.HasValue)
            {
                var rotationLine = FormatFloat(data.CustomRingRotation.Value);
                if (data.CustomDirection.HasValue)
                {
                    rotationLine += " " + DirectionText(data.CustomDirection.Value);
                }
                lines.AppendLine(rotationLine);
            }
            if (data.CustomStep.HasValue) lines.AppendLine($"Z{FormatFloat(data.CustomStep.Value)}");
            // Propagation always retains thousandths because small differences materially alter repeated assignments.
            if (data.CustomProp.HasValue) lines.AppendLine($"P{FormatFloat(data.CustomProp.Value, "0.###")}");
            // BasicEventAppearanceTest's low/high-propagation speed regressions require
            // speed precision to depend only on speed magnitude, never propagation.
            if (data.CustomSpeed.HasValue)
            {
                var speed = FormatRingSpeed(data.CustomSpeed.Value);
                lines.AppendLine($"S{speed}");
            }
            return lines.ToString().TrimEnd('\r', '\n');
        }

        private static string GetRingZoomText(BaseEvent data, bool isSmoothStepRingZoom)
        {
            // SmoothStepRingZoom only applies to The Second's ring and uses i as its integer fallback.
            if (isSmoothStepRingZoom)
                return $"Z{FormatFloat(data.CustomStep ?? data.Value, "0.###")}";

            var lines = new StringBuilder();
            // Ring zoom step retains thousandths so the node label reflects the dedicated fine precision ladder.
            if (data.CustomStep.HasValue) lines.AppendLine($"Z{FormatFloat(data.CustomStep.Value, "0.###")}");
            // RingZoomSpeedBelowOneDisplaysThreeDecimals requires zoom and rotation to
            // share the same magnitude-based ring-speed precision rule.
            if (data.CustomSpeed.HasValue) lines.Append($"S{FormatRingSpeed(data.CustomSpeed.Value)}");
            return lines.ToString().TrimEnd('\r', '\n');
        }

        private static string DirectionText(int direction) => direction == 1 ? "CW" : "CCW";

        // Ring speed labels use thousandths below one and hundredths otherwise, avoiding
        // separate rotation/zoom formatters that can drift back out of sync.
        private static string FormatRingSpeed(float value) =>
            FormatFloat(value, Mathf.Abs(value) < 1f ? "0.###" : "0.##");

        private static string FormatFloat(float value)
        {
            var magnitude = Mathf.Abs(value);
            var format = magnitude > 100f ? "0.##" : magnitude > 10f ? "0.#" : "0.##";
            return FormatFloat(value, format);
        }

        private static string FormatFloat(float value, string format) =>
            value.ToString(format, CultureInfo.InvariantCulture);
    }
}
