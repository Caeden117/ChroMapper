using System;
using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

namespace Beatmap.Appearances
{
    [CreateAssetMenu(menuName = "Beatmap/Appearance/GLS Event Appearance SO", fileName = "GLSEventAppearanceSO")]
    public class GLSEventAppearanceSO : ScriptableObject
    {
        [SerializeField] private EventAppearanceSO eventAppearance;

        private static readonly int colorId = Shader.PropertyToID("_Color");
        private static readonly int strobeColorId = Shader.PropertyToID("_StrobeColor");
        private static readonly int strobeColorEnabledId = Shader.PropertyToID("_StrobeColorEnabled");
        public void SetAppearance(
            GLSEventContainer container,
            bool final = true,
            bool boost = false)
        {
            // Reuse the Basic Event scale contract so inner GLS nodes share its grounded-height calculation.
            container.transform.localScale = Vector3.one * (final
                ? EventAppearanceSO.FinalNodeScale
                : EventAppearanceSO.PreviewNodeScale);
            container.SetIcons(GLSEventIconResolver.Resolve(container.EventData));
            container.MpbController.Mpb.SetFloat(strobeColorEnabledId, 0f);
            switch (container.EventData)
            {
                case BaseLightColorBase colorEvt:
                    if (colorEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        var color = GLSEventCommon.GetColor(colorEvt, boost, eventAppearance);
                        var strobeColor = GLSEventCommon.GetStrobeColor(colorEvt, boost, eventAppearance);
                        container.MpbController.Mpb.SetColor(colorId, color);
                        container.MpbController.Mpb.SetColor(strobeColorId, strobeColor);
                        // Keep an unset strobe dark color from rendering a band on a non-strobing brightness node.
                        var strobeBandEnabled = GLSEventCommon.IsStrobing(colorEvt) && color != strobeColor;
                        container.MpbController.Mpb.SetFloat(
                            strobeColorEnabledId,
                            strobeBandEnabled
                                ? 1f
                                : 0f);
                        container.SetText(colorEvt);
                        container.SetText(true);
                    }

                    break;
                case BaseLightRotationBase rotationEvt:
                    if (rotationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        // Rotation extensions can still ease through extra loops while holding the previous
                        // angle. Show their loop count and easing so these spins are visible in the editor.
                        container.SetText(rotationEvt);
                        container.SetText(rotationEvt.Loop != 0);
                    }
                    else
                    {
                        // Encode the event box axis with the same neutral/CW/CCW grays used by Basic Events.
                        container.MpbController.Mpb.SetColor(colorId, GLSEventCommon.GetAxisColor(rotationEvt, eventAppearance));
                        container.SetText(rotationEvt);
                        container.SetText(true);
                    }

                    break;
                case BaseLightTranslationBase translationEvt:
                    if (translationEvt == null || translationEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        // Encode the event box axis with the same neutral/CW/CCW grays used by Basic Events.
                        container.MpbController.Mpb.SetColor(colorId, GLSEventCommon.GetAxisColor(translationEvt, eventAppearance));
                        container.SetText(translationEvt);
                        container.SetText(true);
                    }

                    break;
                case BaseFxEventFloat fxEvt:
                    if (fxEvt == null || fxEvt.UsePrevious == 1)
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.OffColor);
                        container.SetText(false);
                    }
                    else
                    {
                        container.MpbController.Mpb.SetColor(colorId, eventAppearance.RingEventsColor);
                        container.SetText(fxEvt);
                        container.SetText(true);
                    }

                    break;
                default:
                    container.MpbController.Mpb.SetColor(colorId, Color.gray);
                    container.SetText(false);
                    break;
            }

            container.MpbController.ApplyChanges();
        }

        // Keep inner GLS color-node ribbons synchronized with the selected group's global color-event timeline.
        public void UpdateTransitionRibbon(
            GLSEventContainer container,
            Func<float, bool> isBoostAt,
            ColorBoostEventIndex boostEvents = null)
        {
            if (container.EventData is BaseLightColorBase colorEvent)
            {
                GLSEventCommon.UpdateColorTransitionRibbon(
                    container.LightGradientController,
                    colorEvent,
                    eventAppearance,
                    isBoostAt,
                    container.GlsLightCount,
                    boostEvents: boostEvents);
                GLSEventCommon.UpdateIncomingColorTransitionRibbon(
                    container.IncomingLightGradientController,
                    colorEvent,
                    eventAppearance,
                    isBoostAt,
                    container.GlsLightCount,
                    boostEvents: boostEvents);
            }
            else
            {
                container.LightGradientController.SetVisible(false);
                container.IncomingLightGradientController.SetVisible(false);
            }
        }
    }
}
