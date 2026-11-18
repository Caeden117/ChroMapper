using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Beatmap.Enums;
using Beatmap.Shared;
using Beatmap.V3;
using SimpleJSON;
using UnityEngine;

namespace Beatmap.Base
{
    /// <summary>
    /// GLS light color node base
    /// </summary>
    public class BaseLightColorBase : BaseGLSEvent
    {
        public BaseLightColorBase()
        {
        }

        // Used for Node Editor
        public BaseLightColorBase(JSONNode node) : this(V3LightColorBase.GetFromJson(node))
        {
        }

        protected BaseLightColorBase(
            float time,
            int color,
            float brightness,
            int easing,
            int usePrevious,
            int frequency,
            float strobeBrightness,
            int strobeFade,
            JSONNode customData = null) : base(time, customData)
        {
            Color = color;
            Brightness = brightness;
            Easing = easing;
            UsePrevious = usePrevious;
            Frequency = frequency;
            StrobeBrightness = strobeBrightness;
            StrobeFade = strobeFade;
        }

        protected BaseLightColorBase(BaseLightColorBase other) : base(other)
        {
            Color = other.Color;
            Brightness = other.Brightness;
            Frequency = other.Frequency;
            StrobeBrightness = other.StrobeBrightness;
            StrobeFade = other.StrobeFade;
            StrobeColor = other.StrobeColor;
            ChromaStrobeInterval = other.ChromaStrobeInterval;
            ChromaStrobePulseWidth = other.ChromaStrobePulseWidth;
            ChromaColorEasing = other.ChromaColorEasing;
            ChromaStrobeColorEasing = other.ChromaStrobeColorEasing;
            ChromaStrobeEasing = other.ChromaStrobeEasing;
            CustomLerpType = other.CustomLerpType;
            ColorDistributions = other.ColorDistributions.ToArray();
            StrobeColorDistributions = other.StrobeColorDistributions.ToArray();
        }

        public int Color { get; set; }
        public float Brightness { get; set; }
        public int Frequency { get; set; }
        public float StrobeBrightness { get; set; }
        public int StrobeFade { get; set; }
        public Color? StrobeColor { get; set; }
        public float? ChromaStrobeInterval { get; set; }
        private float chromaStrobePulseWidth = 0.5f;
        public float ChromaStrobePulseWidth
        {
            get => chromaStrobePulseWidth;
            set => chromaStrobePulseWidth = Mathf.Clamp01(value);
        }
        public int? ChromaColorEasing { get; set; }
        public int? ChromaStrobeColorEasing { get; set; }
        public int? ChromaStrobeEasing { get; set; }
        public string[] ColorDistributions { get; set; } = Array.Empty<string>();
        public string[] StrobeColorDistributions { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedStrobeColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();

        public virtual BasicEventColorLerpType CustomLerpType { get; set; }

        public override string CustomKeyColor { get; } = "color";

        public virtual string CustomKeyLerpType => V3LightColorBase.CustomKeyEasingType;

        public string CustomKeyStrobeColor => "strobeColor";
        public string CustomKeyStrobeInterval => "strobeInterval";
        public string CustomKeyStrobePulseWidth => "strobePulseWidth";
        public string CustomKeyColorEasing => "colorEasing";
        public string CustomKeyStrobeColorEasing => "strobeColorEasing";
        public string CustomKeyStrobeEasing => "strobeEasing";
        public string CustomKeyColorDistributions => GLSColorDistribution.ColorDistributionsKey;
        public string CustomKeyStrobeColorDistributions => GLSColorDistribution.StrobeColorDistributionsKey;

        public override bool IsChroma() =>
            CustomData != null && (CustomData.HasKey(CustomKeyColor) || CustomData.HasKey(CustomKeyStrobeColor)
                || CustomData.HasKey(CustomKeyColorEasing) || CustomData.HasKey(CustomKeyStrobeColorEasing)
                || CustomData.HasKey(CustomKeyStrobeEasing) || CustomData.HasKey(CustomKeyStrobePulseWidth)
                || CustomData.HasKey(CustomKeyLerpType)
                || CustomData.HasKey(CustomKeyColorDistributions) || CustomData.HasKey(CustomKeyStrobeColorDistributions));

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is not BaseLightColorBase other)
                return;

            Color = other.Color;
            Brightness = other.Brightness;
            Frequency = other.Frequency;
            StrobeBrightness = other.StrobeBrightness;
            StrobeFade = other.StrobeFade;
            StrobeColor = other.StrobeColor;
            ChromaStrobeInterval = other.ChromaStrobeInterval;
            ChromaStrobePulseWidth = other.ChromaStrobePulseWidth;
            ChromaColorEasing = other.ChromaColorEasing;
            ChromaStrobeColorEasing = other.ChromaStrobeColorEasing;
            ChromaStrobeEasing = other.ChromaStrobeEasing;
            CustomLerpType = other.CustomLerpType;
            ColorDistributions = other.ColorDistributions.ToArray();
            StrobeColorDistributions = other.StrobeColorDistributions.ToArray();
            RefreshColorDistributionCaches();
        }

        protected override void ParseCustom()
        {
            base.ParseCustom();
            CustomLerpType = Easing != (int)EaseType.None
                && (CustomData?.HasKey(CustomKeyLerpType) ?? false)
                && CustomData?[CustomKeyLerpType].Value == "HSV"
                ? BasicEventColorLerpType.TrueHSV
                : BasicEventColorLerpType.RGB;
            StrobeColor = (CustomData?.HasKey(CustomKeyStrobeColor) ?? false)
                ? CustomData?[CustomKeyStrobeColor].ReadColor()
                : null;
            ChromaStrobeInterval = (CustomData?.HasKey(CustomKeyStrobeInterval) ?? false)
                ? CustomData?[CustomKeyStrobeInterval].AsFloat
                : null;
            // V3ColorNodeInstantStripsColorEasing: an Instant node has no interval for colorEasing to drive.
            ChromaColorEasing = Easing != (int)EaseType.None
                && TryGetCustomEasingId(CustomKeyColorEasing, out var colorEasing)
                ? colorEasing
                : null;
            ChromaStrobeColorEasing = TryGetCustomEasingId(CustomKeyStrobeColorEasing, out var strobeColorEasing)
                ? strobeColorEasing
                : null;
            ChromaStrobeEasing = TryGetCustomEasingId(CustomKeyStrobeEasing, out var strobeEasing)
                    && strobeEasing != (int)EaseType.InOutCubic
                ? strobeEasing
                : null;
            var pulseWidth = CustomData?[CustomKeyStrobePulseWidth];
            ChromaStrobePulseWidth = pulseWidth != null && pulseWidth.IsNumber && !float.IsNaN(pulseWidth.AsFloat)
                ? pulseWidth.AsFloat
                : 0.5f;
            ColorDistributions = GLSColorDistribution.ReadStrings(CustomData, CustomKeyColorDistributions);
            StrobeColorDistributions = GLSColorDistribution.ReadStrings(CustomData, CustomKeyStrobeColorDistributions);
            RefreshColorDistributionCaches();
        }

        private bool TryGetCustomEasingId(string key, out int easing)
        {
            easing = 0;
            var node = CustomData?[key];
            var valid = node != null
                && node.IsNumber
                && node.AsDouble == node.AsInt
                && (V3LightColorBase.RequiresCustomEasing(node.AsInt)
                    || node.AsInt == (int)EaseType.Linear);
            if (valid)
            {
                easing = node.AsInt;
            }

            return valid;
        }

        protected internal override JSONNode SaveCustom()
        {
            var node = base.SaveCustom();
            if (CustomLerpType == BasicEventColorLerpType.TrueHSV && Easing != (int)EaseType.None)
                node[CustomKeyLerpType] = "HSV";
            else
                node.Remove(CustomKeyLerpType);
            if (StrobeColor != null)
                // Keep opaque strobe colors compact; Chroma treats an omitted alpha as fully opaque.
                node[CustomKeyStrobeColor] = new JSONArray().WriteColor(StrobeColor.Value, StrobeColor.Value.a != 1f);
            else
                node.Remove(CustomKeyStrobeColor);
            if (ChromaStrobeInterval.HasValue)
                node[CustomKeyStrobeInterval] = ChromaStrobeInterval.Value;
            else
                node.Remove(CustomKeyStrobeInterval);
            if (ChromaColorEasing.HasValue && Easing != (int)EaseType.None)
                node[CustomKeyColorEasing] = ChromaColorEasing.Value;
            else
                node.Remove(CustomKeyColorEasing);
            if (ChromaStrobeColorEasing.HasValue)
                node[CustomKeyStrobeColorEasing] = ChromaStrobeColorEasing.Value;
            else
                node.Remove(CustomKeyStrobeColorEasing);
            if (ChromaStrobeEasing.HasValue && StrobeFade == 1)
                node[CustomKeyStrobeEasing] = ChromaStrobeEasing.Value;
            else
                node.Remove(CustomKeyStrobeEasing);
            if (ChromaStrobePulseWidth != 0.5f)
                node[CustomKeyStrobePulseWidth] = ChromaStrobePulseWidth;
            else
                node.Remove(CustomKeyStrobePulseWidth);
            GLSColorDistribution.WriteStrings(node, CustomKeyColorDistributions, ColorDistributions);
            GLSColorDistribution.WriteStrings(node, CustomKeyStrobeColorDistributions, StrobeColorDistributions);
            RefreshColorDistributionCaches();
            return node;
        }

        // UI mutations update the raw arrays first, so refresh both parsed lists at the same ownership boundary as JSON writes.
        private void RefreshColorDistributionCaches()
        {
            ParsedColorDistributions = GLSColorDistribution.Parse(ColorDistributions);
            ParsedStrobeColorDistributions = GLSColorDistribution.Parse(StrobeColorDistributions);
        }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false)
        {
            if (other is BaseLightColorBase lcb) return BoxIndex == lcb.BoxIndex;
            return false;
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightColorBase.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightColorBase(this);
    }

    [Flags]
    public enum GLSColorDistributionTargets
    {
        None = 0,
        Hue = 1 << 0,
        Saturation = 1 << 1,
        Value = 1 << 2,
        Red = 1 << 3,
        Green = 1 << 4,
        Blue = 1 << 5,
        Brightness = 1 << 6
    }

    public sealed class GLSColorDistributionInstruction
    {
        public GLSColorDistributionInstruction(
            GLSColorDistributionTargets targets,
            float offset,
            Func<float, float> easing,
            bool usesAffectedLightProgress)
        {
            Targets = targets;
            Offset = offset;
            Easing = easing;
            UsesAffectedLightProgress = usesAffectedLightProgress;
        }

        public GLSColorDistributionTargets Targets { get; }
        public float Offset { get; }
        public Func<float, float> Easing { get; }
        public bool UsesAffectedLightProgress { get; }
    }

    public static class GLSColorDistribution
    {
        public const string ColorDistributionsKey = "colorDistributions";
        public const string StrobeColorDistributionsKey = "strobeColorDistributions";

        public static IReadOnlyList<GLSColorDistributionInstruction> Parse(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                return Array.Empty<GLSColorDistributionInstruction>();
            }

            var result = new List<GLSColorDistributionInstruction>(values.Count);
            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                var parts = values[valueIndex]?.Split(',');
                if (parts == null
                    || parts.Length < 3
                    || !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var offset)
                    || float.IsNaN(offset)
                    || float.IsInfinity(offset)
                    || !Easing.TryGetByNameOrShortName(parts[2], out var easing))
                {
                    continue;
                }

                var targets = ParseTargets(parts[0]);
                if (targets != GLSColorDistributionTargets.None)
                {
                    var usesAffectedLightProgress = parts.Length >= 4
                        && string.Equals(parts[3].Trim(), "l", StringComparison.OrdinalIgnoreCase);
                    result.Add(new GLSColorDistributionInstruction(
                        targets,
                        offset,
                        easing,
                        usesAffectedLightProgress));
                }
            }

            return result;
        }

        public static string[] ReadStrings(JSONNode customData, string key)
        {
            if (customData == null || !customData.HasKey(key) || customData[key] is not JSONArray values)
            {
                return Array.Empty<string>();
            }

            var result = new List<string>(values.Count);
            foreach (var value in values)
            {
                if (value.Value is JSONString)
                {
                    result.Add(value.Value.Value);
                }
            }

            return result.ToArray();
        }

        public static void WriteStrings(JSONNode customData, string key, IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
            {
                customData.Remove(key);
                return;
            }

            var array = new JSONArray();
            for (var valueIndex = 0; valueIndex < values.Count; valueIndex++)
            {
                array.Add(values[valueIndex]);
            }

            customData[key] = array;
        }

        public static string[] FromEditorText(string value) =>
            (value ?? string.Empty)
            .Split(new[] { ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(entry => entry.Trim())
            .Where(entry => entry.Length > 0)
            .ToArray();

        public static string ToEditorText(IReadOnlyList<string> values) => values == null
            ? string.Empty
            : string.Join("; ", values);

        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyNormal(color, box, evt, distributionProgress, distributionProgress);

        public static Color ApplyNormal(
            Color color,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress) =>
            Apply(color, box?.ParsedColorDistributions, evt.ParsedColorDistributions, affectedChunkProgress, affectedLightProgress);

        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float distributionProgress) =>
            ApplyStrobe(mainColor, box, evt, distributionProgress, distributionProgress);

        // An explicit strobeColor uses only strobe color distributions. Without one, strobe distributions
        // start from the unshifted main color. Only strobes with neither override inherit the fully
        // distributed main color. Clamp saturation after each channel's complete box/event sequence.
        public static Color ApplyStrobe(
            Color mainColor,
            BaseLightColorEventBox box,
            BaseLightColorBase evt,
            float affectedChunkProgress,
            float affectedLightProgress)
        {
            var ownsStrobeChannel = evt.StrobeColor.HasValue
                || box?.ParsedStrobeColorDistributions.Count > 0
                || evt.ParsedStrobeColorDistributions.Count > 0;
            return ownsStrobeChannel
                ? Apply(
                    evt.StrobeColor ?? mainColor,
                    box?.ParsedStrobeColorDistributions,
                    evt.ParsedStrobeColorDistributions,
                    affectedChunkProgress,
                    affectedLightProgress)
                : ApplyNormal(mainColor, box, evt, affectedChunkProgress, affectedLightProgress);
        }

        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorDistributionInstruction> boxInstructions,
            IReadOnlyList<GLSColorDistributionInstruction> eventInstructions,
            float distributionProgress) =>
            Apply(color, boxInstructions, eventInstructions, distributionProgress, distributionProgress);

        public static Color Apply(
            Color color,
            IReadOnlyList<GLSColorDistributionInstruction> boxInstructions,
            IReadOnlyList<GLSColorDistributionInstruction> eventInstructions,
            float affectedChunkProgress,
            float affectedLightProgress)
        {
            var evaluator = new ColorDistributionEvaluator(color);
            evaluator.Apply(boxInstructions, affectedChunkProgress, affectedLightProgress);
            evaluator.Apply(eventInstructions, affectedChunkProgress, affectedLightProgress);
            return evaluator.Result;
        }

        private struct ColorDistributionEvaluator
        {
            private Color color;
            private float hue;
            private float saturation;
            private float value;
            private bool hsvPending;

            public ColorDistributionEvaluator(Color color)
            {
                this.color = color;
                hue = 0f;
                saturation = 0f;
                value = 0f;
                hsvPending = false;
            }

            public Color Result
            {
                get
                {
                    MaterializeHsv();
                    return color;
                }
            }

            public void Apply(
                IReadOnlyList<GLSColorDistributionInstruction> instructions,
                float affectedChunkProgress,
                float affectedLightProgress)
            {
                if (instructions == null)
                {
                    return;
                }

                for (var instructionIndex = 0; instructionIndex < instructions.Count; instructionIndex++)
                {
                    var instruction = instructions[instructionIndex];
                    var distributionProgress = instruction.UsesAffectedLightProgress
                        ? affectedLightProgress
                        : affectedChunkProgress;
                    var offset = instruction.Offset * instruction.Easing(distributionProgress);
                    var targets = instruction.Targets;
                    if ((targets & (GLSColorDistributionTargets.Hue | GLSColorDistributionTargets.Saturation | GLSColorDistributionTargets.Value)) != 0)
                    {
                        EnsureHsv();
                        if ((targets & GLSColorDistributionTargets.Hue) != 0)
                        {
                            hue += offset;
                        }
                        if ((targets & GLSColorDistributionTargets.Saturation) != 0)
                        {
                            saturation += offset;
                        }
                        if ((targets & GLSColorDistributionTargets.Value) != 0)
                        {
                            value += offset;
                        }
                    }

                    if ((targets & (GLSColorDistributionTargets.Red | GLSColorDistributionTargets.Green | GLSColorDistributionTargets.Blue)) != 0)
                    {
                        // Apply pending HSV offsets before an RGB instruction reads or changes the color
                        // channels.
                        MaterializeHsv();
                        if ((targets & GLSColorDistributionTargets.Red) != 0)
                        {
                            color.r += offset;
                        }
                        if ((targets & GLSColorDistributionTargets.Green) != 0)
                        {
                            color.g += offset;
                        }
                        if ((targets & GLSColorDistributionTargets.Blue) != 0)
                        {
                            color.b += offset;
                        }
                    }

                    if ((targets & GLSColorDistributionTargets.Brightness) != 0)
                    {
                        color.a += offset;
                    }
                }
            }

            private void EnsureHsv()
            {
                if (hsvPending)
                {
                    return;
                }

                Color.RGBToHSV(color, out hue, out saturation, out value);
                hsvPending = true;
            }

            private void MaterializeHsv()
            {
                if (!hsvPending)
                {
                    return;
                }

                var alpha = color.a;
                // Wrap hue and clamp saturation, but preserve HDR value above 1.
                color = Color.HSVToRGB(Mathf.Repeat(hue, 1f), Mathf.Clamp01(saturation), value, true);
                color.a = alpha;
                hsvPending = false;
            }
        }

        private static GLSColorDistributionTargets ParseTargets(string value)
        {
            var targets = GLSColorDistributionTargets.None;
            var model = ColorModel.None;
            foreach (var target in value ?? string.Empty)
            {
                switch (char.ToLowerInvariant(target))
                {
                    case 'h' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Hue;
                        break;
                    case 's' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Saturation;
                        break;
                    case 'v' when model != ColorModel.Rgb:
                        model = ColorModel.Hsv;
                        targets |= GLSColorDistributionTargets.Value;
                        break;
                    case 'r' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Red;
                        break;
                    case 'g' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Green;
                        break;
                    case 'b' when model != ColorModel.Hsv:
                        model = ColorModel.Rgb;
                        targets |= GLSColorDistributionTargets.Blue;
                        break;
                    case 'f':
                        targets |= GLSColorDistributionTargets.Brightness;
                        break;
                }
            }

            return targets;
        }

        private enum ColorModel
        {
            None,
            Hsv,
            Rgb
        }
    }
}
