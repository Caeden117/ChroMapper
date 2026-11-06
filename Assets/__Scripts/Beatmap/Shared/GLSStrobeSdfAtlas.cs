using System;
using UnityEngine;

namespace Beatmap.Shared
{
    public static class GLSStrobeSdfAtlas
    {
        public const int FieldHeight = 32;
        public const int FieldWidth = 71;
        public const int Padding = 2;
        public const int Columns = 24;
        public const int EasingCount = 35;
        private const int CornerEasingCount = 6;
        public const int TileWidth = FieldWidth + (2 * Padding);
        public const int TileHeight = FieldHeight + (2 * Padding);
        public const float DistanceRange = 16f;
        public const float CoreRadius = 5.90625f;
        public const float CoordinateScale = 4f;

        private static readonly float[] pulseWidths =
        {
            0f, 0.01f, 0.02f, 0.03f, 0.05f, 0.1f, 0.15f, 0.2f, 0.25f, 0.3f,
            0.4f, 0.5f, 0.6f, 0.7f, 0.75f, 0.8f, 0.85f, 0.9f, 0.95f, 0.97f, 0.98f, 0.99f, 1f
        };

        public static int PulseWidthCount => pulseWidths.Length;
        public static int PrimaryFieldCount => EasingCount * PulseWidthCount;
        public static int FieldCount => (EasingCount + CornerEasingCount) * PulseWidthCount;
        public static int Width => Columns * TileWidth;
        public static int Height => ((FieldCount + Columns - 1) / Columns) * TileHeight;

        public static float ReferencePulseWidth(int index)
        {
            return pulseWidths[index];
        }

        public static int CornerFieldIndex(int easingId, int pulseWidthIndex)
        {
            // Bounce and Beat Saber's InOut Back/Elastic variants need a second field to keep their curve
            // sections separate. Store it only for those easings to save atlas space.
            var cornerEasing = easingId switch
            {
                28 => 0,
                29 => 1,
                30 => 2,
                32 => 3,
                33 => 4,
                34 => 5,
                _ => -1
            };
            return cornerEasing < 0
                ? -1
                : PrimaryFieldCount + (cornerEasing * PulseWidthCount) + pulseWidthIndex;
        }

        public static Vector2 FieldOrigin(int field)
        {
            return new Vector2(
                ((field % Columns) * TileWidth) + Padding,
                ((field / Columns) * TileHeight) + Padding);
        }

        public static Vector4 GetFieldParameters(
            int easingId,
            float pulseWidth,
            float endValue,
            out Vector4 cornerParameters)
        {
            var index = Array.BinarySearch(pulseWidths, pulseWidth);
            if (index < 0)
            {
                var upper = ~index;
                var lower = upper - 1;
                var midpoint = pulseWidths[lower] + ((pulseWidths[upper] - pulseWidths[lower]) * 0.5f);
                index = pulseWidth < midpoint ? lower : upper;
                // A zero-width reset line cannot be stretched into a curve, so interior pulse widths need an
                // interior reference.
                index = Mathf.Clamp(index, 1, pulseWidths.Length - 2);
            }

            var origin = FieldOrigin((easingId * pulseWidths.Length) + index);
            var cornerField = CornerFieldIndex(easingId, index);
            if (cornerField < 0)
                cornerParameters = Vector4.one * -1f;
            else
            {
                var rising = FieldOrigin(cornerField);
                var falling = FieldOrigin(CornerFieldIndex(easingId, pulseWidths.Length - 1 - index));
                cornerParameters = new Vector4(rising.x, rising.y, falling.x, falling.y);
            }

            return new Vector4(origin.x, origin.y, pulseWidths[index], endValue);
        }
    }
}
