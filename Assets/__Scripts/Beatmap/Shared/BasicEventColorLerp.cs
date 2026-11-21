using UnityEngine;

namespace Beatmap.Shared
{
    // Use the same RGB, HSV, and TrueHSV mode IDs in preview lights and ribbon shaders.
    public enum BasicEventColorLerpType
    {
        RGB = 0,
        LegacyHSV = 1,
        TrueHSV = 2
    }

    // Centralizing classification and CPU interpolation prevents the two HSV semantics from drifting across consumers.
    public static class BasicEventColorLerp
    {
        // Chroma's HSV mode interpolates hue as a number from 0 to 1. TrueHSV takes the shortest path around
        // the hue wheel.
        public static BasicEventColorLerpType FromSerializedName(string lerpType)
        {
            return lerpType switch
            {
                "HSV" => BasicEventColorLerpType.LegacyHSV,
                "TrueHSV" => BasicEventColorLerpType.TrueHSV,
                _ => BasicEventColorLerpType.RGB
            };
        }

        // Preview evaluation is per-frame, so dispatch directly without allocations or repeated serialized-name checks.
        public static Color Interpolate(
            Color start,
            Color end,
            float t,
            BasicEventColorLerpType lerpType)
        {
            return lerpType switch
            {
                BasicEventColorLerpType.LegacyHSV => LerpLegacyHsv(start, end, t),
                BasicEventColorLerpType.TrueHSV => LerpTrueHSV(start, end, t),
                _ => Color.LerpUnclamped(start, end, t)
            };
        }

        public static Color ApplyBrightness(Color color, float brightness)
        {
            color.a *= brightness;
            return color;
        }

        public static Color InterpolateWithBrightness(
            Color start,
            float startBrightness,
            Color end,
            float endBrightness,
            float t,
            BasicEventColorLerpType lerpType) =>
            Interpolate(
                ApplyBrightness(start, startBrightness),
                ApplyBrightness(end, endBrightness),
                t,
                lerpType);

        public static Color InterpolateStrobeFade(
            Color normal,
            Color strobe,
            float t,
            BasicEventColorLerpType lerpType)
        {
            return lerpType switch
            {
                BasicEventColorLerpType.LegacyHSV => LerpLegacyHsv(normal, strobe, t),
                BasicEventColorLerpType.TrueHSV => LerpTrueHSV(normal, strobe, t),
                _ => Color.LerpUnclamped(normal, strobe, t)
            };
        }

        // Legacy Chroma compatibility requires treating normalized hue as an ordinary scalar, including its green detours.
        private static Color LerpLegacyHsv(Color start, Color end, float t)
        {
            Color.RGBToHSV(start, out var startHue, out var startSaturation, out var startValue);
            Color.RGBToHSV(end, out var endHue, out var endSaturation, out var endValue);
            return WithInterpolatedAlpha(
                Color.HSVToRGB(
                    Mathf.LerpUnclamped(startHue, endHue, t),
                    Mathf.LerpUnclamped(startSaturation, endSaturation, t),
                    Mathf.LerpUnclamped(startValue, endValue, t)),
                start,
                end,
                t);
        }

        private static Color LerpTrueHSV(Color start, Color end, float t)
        {
            Color.RGBToHSV(start, out var startHue, out var startSaturation, out var startValue);
            Color.RGBToHSV(end, out var endHue, out var endSaturation, out var endValue);
            var hue = Mathf.LerpAngle(startHue * 360f, endHue * 360f, t);
            return WithInterpolatedAlpha(
                Color.HSVToRGB(
                    Mathf.Repeat(hue, 360f) / 360f,
                    Mathf.LerpUnclamped(startSaturation, endSaturation, t),
                    Mathf.LerpUnclamped(startValue, endValue, t)),
                start,
                end,
                t);
        }

        // Both HSV modes retain the existing independent alpha interpolation used by Basic Event brightness handling.
        private static Color WithInterpolatedAlpha(Color color, Color start, Color end, float t)
        {
            color.a = Mathf.LerpUnclamped(start.a, end.a, t);
            return color;
        }
    }
}
