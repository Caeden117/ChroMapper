using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class GLSEventIconView : MonoBehaviour
    {
        // Place icons just outside the node surface to avoid z-fighting.
        public const float SurfaceOffset = 0.505f;
        public const float StateIconHorizontalPosition = 0.25f;
        public const float StrobeIconHorizontalPosition = StateIconHorizontalPosition - 0.03f;
        public const float EasingIconWidth = 0.3168f * 1.15f;
        public const float CircularEasingIconWidth = 0.22f * 1.15f;
        public const float EasingIconHeight = 0.198f * 1.15f;
        public const float PreviousRotationDirectionIconSize = 0.264f;
        public const float RotationDirectionIconSize = PreviousRotationDirectionIconSize * 1.3f;
        public const float ColorIconScale = 0.7f;

        private const float StrobeIconScale = ColorIconScale * 0.85f;
        private const float DefaultIconSize = 0.22f;
        private const float DefaultIconHeight = 0.29f;
        private const float TextSurfaceOffset = 0.506f;

        public const float HoverLabelHorizontalPosition = 0.9f;
        private GLSColorHoverPool.Labels hoverLabels;
        internal Transform ColorHoverRoot => hoverLabels?.Root;
        private BaseLightColorBase currentColorEvent;
        private bool colorHoverVisible;
        private Transform previewVisualParent;

        [SerializeField] private Sprite[] icons;

        [SerializeField] private SpriteRenderer[] primaryRenderers;
        [SerializeField] private SpriteRenderer[] secondaryRenderers;
        [SerializeField] private SpriteRenderer[] tertiaryRenderers;
        [SerializeField] private GLSStrobeIconRenderer[] strobeRenderers;

        public void SetPreviewVisualParent(Transform parent) => previewVisualParent = parent;

        public void SetIcons(GLSEventIconState state, BaseGLSEvent evt, TextMeshPro[] valueDisplays)
        {
            // Keep the represented event so easing edits refresh labels under a stationary cursor.
            currentColorEvent = evt as BaseLightColorBase;
            // Reusing this node for another event must release its old hover labels.
            if (currentColorEvent == null || currentColorEvent.UsePrevious == 1)
            {
                colorHoverVisible = false;
                ReleaseColorHoverDisplays();
            }

            ApplyLayout(evt, valueDisplays);
            var iconScale = evt is BaseLightColorBase ? ColorIconScale : 1f;
            var strobeIconScale = currentColorEvent != null ? StrobeIconScale : iconScale;
            SetIcon(primaryRenderers, state.Primary, iconScale);
            SetIcon(secondaryRenderers, state.Secondary, strobeIconScale);
            SetIcon(tertiaryRenderers, state.Tertiary, iconScale);
            var showStrobe = currentColorEvent != null && state.Secondary != GLSEventIconType.None;
            if (showStrobe)
            {
                ApplyStrobeIcon(state.Secondary, currentColorEvent.ChromaStrobePulseWidth);
            }

            for (var i = 0; i < strobeRenderers.Length; i++)
            {
                strobeRenderers[i].enabled = showStrobe;
                if (showStrobe)
                {
                    secondaryRenderers[i].enabled = false;
                }
            }

            // A pooled node rebound under a stationary cursor must refresh or retire its existing hover labels.
            if (colorHoverVisible)
            {
                RefreshColorHoverLabels();
            }
        }

        public void SetColorHover(bool visible, TextMeshPro[] valueDisplays)
        {
            visible = visible && currentColorEvent != null && currentColorEvent.UsePrevious == 0;
            if (colorHoverVisible == visible)
            {
                return;
            }

            colorHoverVisible = visible;
            if (visible)
            {
                RentColorHoverDisplays(valueDisplays);
                RefreshColorHoverLabels();
            }
            else
            {
                ReleaseColorHoverDisplays();
            }
        }

        private void RentColorHoverDisplays(TextMeshPro[] valueDisplays)
        {
            hoverLabels = GLSColorHoverPool.Rent(
                valueDisplays,
                previewVisualParent != null
                    ? previewVisualParent
                    : transform);
        }

        private void RefreshColorHoverLabels()
        {
            var valid = colorHoverVisible && currentColorEvent != null && currentColorEvent.UsePrevious == 0;
            if (!valid)
            {
                ReleaseColorHoverDisplays();
                return;
            }

            var isHsv = currentColorEvent.CustomLerpType == BasicEventColorLerpType.TrueHSV;
            var fadeEasing = currentColorEvent.Easing == (int)EaseType.None
                ? (int)EaseType.None
                : currentColorEvent.ChromaColorEasing ?? currentColorEvent.Easing;
            SetHoverPair(hoverLabels.Fade, "Fade Ease", fadeEasing, true, isHsv);
            var strobeEasing = currentColorEvent.StrobeFade == 1
                ? currentColorEvent.ChromaStrobeEasing ?? (int)EaseType.InOutCubic
                : (int)EaseType.None;
            SetHoverPair(
                hoverLabels.Strobe,
                "Strobe Ease",
                strobeEasing,
                GLSEventCommon.IsStrobing(currentColorEvent),
                false,
                currentColorEvent.ChromaStrobePulseWidth);
            SetHoverPair(
                hoverLabels.StrobeColor,
                "Strobe Color Ease",
                currentColorEvent.ChromaStrobeColorEasing ?? (int)EaseType.None,
                currentColorEvent.ChromaStrobeColorEasing.HasValue,
                isHsv);
        }

        private static void SetHoverPair(
            GLSNodeLabel[] pair,
            string title,
            int easing,
            bool active,
            bool hsv,
            float? pulseWidth = null)
        {
            foreach (var label in pair)
            {
                if (active)
                    label.SetHover(title, easing, hsv, pulseWidth);

                label.gameObject.SetActive(active);
            }
        }

        private void ReleaseColorHoverDisplays()
        {
            if (hoverLabels == null)
                return;

            hoverLabels.Release();
            hoverLabels = null;
        }

        private void OnDisable()
        {
            colorHoverVisible = false;
            ReleaseColorHoverDisplays();
        }

        private void ApplyLayout(BaseGLSEvent evt, TextMeshPro[] valueDisplays)
        {
            var primaryX = evt is BaseLightRotationBase
                ? GLSEventCommon.RotationColumnHorizontalOffset
                : evt is BaseLightTranslationBase or BaseFxEventFloat
                    ? 0f
                    : -StateIconHorizontalPosition;
            var secondaryX = evt is BaseLightRotationBase
                ? -GLSEventCommon.RotationColumnHorizontalOffset
                : evt is BaseLightColorBase
                    ? StrobeIconHorizontalPosition
                    : StateIconHorizontalPosition;

            var primaryIconHeight = evt is BaseLightRotationBase or BaseLightTranslationBase or BaseFxEventFloat
                ? GLSEventCommon.TransformEasingIconHeight
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorEasingIconHeight
                    : DefaultIconHeight;
            var secondaryIconHeight = evt is BaseLightRotationBase
                ? GLSEventCommon.RotationDirectionIconHeight
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorStrobeIconHeight
                    : DefaultIconHeight;
            SetFacePositions(primaryRenderers, primaryX, primaryIconHeight);
            SetFacePositions(secondaryRenderers, secondaryX, secondaryIconHeight);
            SetFacePositions(
                tertiaryRenderers,
                -StateIconHorizontalPosition,
                GLSEventCommon.ColorTertiaryIconHeight);

            var textVerticalOffset = evt is BaseLightRotationBase or BaseLightTranslationBase or BaseFxEventFloat
                ? GLSEventCommon.TransformTextVerticalOffset
                : evt is BaseLightColorBase
                    ? GLSEventCommon.ColorFaceVerticalOffset
                    : 0f;
            valueDisplays[0].rectTransform.localPosition = new Vector3(0f, TextSurfaceOffset, textVerticalOffset);
            valueDisplays[1].rectTransform.localPosition = new Vector3(0f, textVerticalOffset, -TextSurfaceOffset);
        }

        private static void SetFacePositions(
            SpriteRenderer[] renderers,
            float horizontalPosition,
            float iconHeight)
        {
            renderers[0].transform.localPosition = new Vector3(
                horizontalPosition,
                SurfaceOffset,
                iconHeight);
            renderers[1].transform.localPosition = new Vector3(
                horizontalPosition,
                iconHeight,
                -SurfaceOffset);
        }

        // The sprite array omits None, so icon enum values map to array indices after subtracting one.
        private void SetIcon(SpriteRenderer[] renderers, GLSEventIconType iconType, float iconScale)
        {
            var enabled = iconType != GLSEventIconType.None;
            var sprite = enabled
                ? icons[(int)iconType - 1]
                : null;
            var scale = iconScale * (IsEasing(iconType)
                ? new Vector3(EasingIconHeight, EasingIconHeight, DefaultIconSize)
                : IsRotationDirection(iconType)
                    ? Vector3.one * RotationDirectionIconSize
                    : Vector3.one * DefaultIconSize);
            for (var i = 0; i < renderers.Length; i++)
            {
                renderers[i].sprite = sprite;
                renderers[i].enabled = enabled;
                renderers[i].transform.localScale = scale;
            }
        }

        private void ApplyStrobeIcon(GLSEventIconType type, float pulseWidth)
        {
            for (var i = 0; i < strobeRenderers.Length; i++)
            {
                strobeRenderers[i].SetIcon(type, pulseWidth);
            }
        }

        private static bool IsEasing(GLSEventIconType iconType)
        {
            var value = (int)iconType;
            return (value >= (int)GLSEventIconType.EaseLinear
                    && value <= (int)GLSEventIconType.EaseBeatSaberInOutBounce)
                || iconType == GLSEventIconType.NoEasingStep;
        }

        private static bool IsRotationDirection(GLSEventIconType iconType)
        {
            return iconType is GLSEventIconType.RotationAutomatic
                or GLSEventIconType.RotationClockwise
                or GLSEventIconType.RotationCounterClockwise;
        }
    }
}
