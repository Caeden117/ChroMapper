using UnityEngine;

namespace Beatmap.Shared
{
    // One stable numeric contract keeps preview and ribbon shader dispatch aligned for RGB, legacy HSV, and trueHSV.
    public enum BasicEventColorLerpType
    {
        RGB = 0,
        LegacyHSV = 1,
        TrueHSV = 2
    }

    // Centralizing classification and CPU interpolation prevents the two HSV semantics from drifting across consumers.
    public static class BasicEventColorLerp
    {
        // Chroma's existing HSV name means normalized 0..1 hue lerping; trueHSV opts into conventional angular interpolation.
        public static BasicEventColorLerpType FromSerializedName(string lerpType)
        {
            return lerpType switch
            {
                "HSV" => BasicEventColorLerpType.LegacyHSV,
                "trueHSV" => BasicEventColorLerpType.TrueHSV,
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

        // trueHSV preserves ChroMapper's former behavior by wrapping hue across the seam along the shortest angular path.
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
