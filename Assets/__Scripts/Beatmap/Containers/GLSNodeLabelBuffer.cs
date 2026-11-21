using System;
using System.Collections.Generic;
using System.Globalization;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;

namespace Beatmap.Containers
{
    internal sealed class GLSNodeLabelBuffer
    {
        internal char[] Characters = new char[64];
        internal int Length;
        internal GLSNodeLabelKind Kind;

        internal void WriteEvent(BaseGLSEvent evt)
        {
            Length = 0;
            switch (evt)
            {
                case BaseLightColorBase color:
                    Kind = GLSNodeLabelKind.Color;
                    Append(color.Brightness * 100f);
                    Append('\n');
                    if (color.StrobeBrightness > 0f || GLSEventCommon.IsStrobing(color))
                        Append(color.StrobeBrightness * 100f);

                    Append('\n');
                    if (color.ChromaStrobeInterval is { } interval && interval > 0f)
                    {
                        var format = interval >= 10f
                            ? "0.0"
                            : "0.00";
                        Append(interval, format);
                    }
                    else if (color.Frequency > 0)
                    {
                        Append("1/");
                        Append(color.Frequency);
                    }

                    break;
                case BaseLightRotationBase rotation:
                    Kind = GLSNodeLabelKind.Rotation;
                    if (rotation.UsePrevious != 1 || rotation.Loop != 0)
                        Append(rotation.Loop);

                    Append('\n');
                    // Holding the previous angle can still add positive loops, but negative extensions do not ease.
                    if (rotation.UsePrevious != 1 || rotation.Loop > 0)
                        AppendEasing(rotation.Easing);

                    Append('\n');
                    if (rotation.UsePrevious != 1)
                        Append(rotation.Rotation);

                    break;
                case BaseLightTranslationBase translation:
                    Kind = GLSNodeLabelKind.Transform;
                    Append('\n');
                    AppendEasing(translation.Easing);
                    Append('\n');
                    if (GLSEventTranslationCommand.IsYeet(translation.Translation))
                        Append("YEET");
                    else
                        Append(translation.Translation * 100f);

                    break;
                case BaseFxEventFloat fx:
                    Kind = GLSNodeLabelKind.Transform;
                    Append('\n');
                    AppendEasing(fx.Easing);
                    Append('\n');
                    Append(fx.Value * 100f);
                    break;
            }
        }

        internal void WriteHover(string title, int easing, bool hsv, float? pulseWidth)
        {
            Length = 0;
            Kind = GLSNodeLabelKind.Hover;
            Append(title);
            Append('\n');
            if (hsv)
                Append("HSV ");

            if (pulseWidth.HasValue)
            {
                Append(pulseWidth.Value, "0.##");
                Append(' ');
            }

            AppendEasing(easing);
            if (easing is (int)EaseType.InQuadratic or (int)EaseType.OutQuadratic or (int)EaseType.InOutQuadratic)
                Append(" (Qd)");
        }

        private void AppendEasing(int easing) => Append(Easing.IDToShortName.GetValueOrDefault(easing));

        private void Append(string value)
        {
            if (value == null)
                return;

            EnsureCapacity(Length + value.Length);
            value.CopyTo(0, Characters, Length, value.Length);
            Length += value.Length;
        }

        private void Append(char value)
        {
            EnsureCapacity(Length + 1);
            Characters[Length++] = value;
        }

        private void Append(float value, string format = null)
        {
            // A fixed-point float can need 40 characters. Reserve before formatting so tiny custom intervals fit too.
            EnsureCapacity(Length + 48);
            value.TryFormat(Characters.AsSpan(Length), out var count, format, CultureInfo.InvariantCulture);
            Length += count;
        }

        private void Append(int value)
        {
            EnsureCapacity(Length + 12);
            value.TryFormat(Characters.AsSpan(Length), out var count, default, CultureInfo.InvariantCulture);
            Length += count;
        }

        private void EnsureCapacity(int length)
        {
            if (length > Characters.Length)
                Array.Resize(ref Characters, UnityEngine.Mathf.NextPowerOfTwo(length));
        }

        public override string ToString() => new(Characters, 0, Length);
    }
}
