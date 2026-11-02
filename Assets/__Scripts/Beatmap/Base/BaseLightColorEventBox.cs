using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorEventBox : BaseEventBox
    {
        public BaseLightColorEventBox()
        {
            ApplyDefaultAffectFirst();
            BrightnessDistributionType = (int)DistributionType.Wave;
            Events = Array.Empty<BaseLightColorBase>();
        }

        protected BaseLightColorEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float brightnessDistribution,
            int brightnessDistributionType,
            int brightnessAffectFirst,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightColorEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float brightnessDistribution,
            int brightnessDistributionType,
            int brightnessAffectFirst,
            int easing,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType, easing)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightColorEventBox(BaseLightColorEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            BrightnessDistribution = other.BrightnessDistribution;
            BrightnessDistributionType = other.BrightnessDistributionType;
            BrightnessAffectFirst = other.BrightnessAffectFirst;
            CustomData = other.CustomData?.Clone();
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightColorBase>().ToArray();
            ColorDistributions = other.ColorDistributions.ToArray();
            StrobeColorDistributions = other.StrobeColorDistributions.ToArray();
            RefreshColorDistributionCaches();
        }

        public float BrightnessDistribution { get; set; }
        public int BrightnessDistributionType { get; set; }
        public int BrightnessAffectFirst { get; set; }
        public BaseLightColorBase[] Events { get; set; }
        public JSONNode CustomData { get; private set; } = new JSONObject();
        public string[] ColorDistributions { get; set; } = Array.Empty<string>();
        public string[] StrobeColorDistributions { get; set; } = Array.Empty<string>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();
        public IReadOnlyList<GLSColorDistributionInstruction> ParsedStrobeColorDistributions { get; private set; } =
            Array.Empty<GLSColorDistributionInstruction>();

        public void SetCustomData(JSONNode customData)
        {
            CustomData = customData is JSONObject
                ? customData
                : new JSONObject();
            // Read the authored distribution keys directly so loading leaves customData unchanged.
            ColorDistributions = GLSColorDistribution.ReadStrings(CustomData, GLSColorDistribution.ColorDistributionsKey);
            StrobeColorDistributions = GLSColorDistribution.ReadStrings(CustomData, GLSColorDistribution.StrobeColorDistributionsKey);
            RefreshColorDistributionCaches();
        }

        public JSONNode SaveCustom()
        {
            GLSColorDistribution.WriteStrings(CustomData, GLSColorDistribution.ColorDistributionsKey, ColorDistributions);
            GLSColorDistribution.WriteStrings(CustomData, GLSColorDistribution.StrobeColorDistributionsKey, StrobeColorDistributions);
            RefreshColorDistributionCaches();
            return CustomData;
        }

        private void RefreshColorDistributionCaches()
        {
            ParsedColorDistributions = GLSColorDistribution.Parse(ColorDistributions);
            ParsedStrobeColorDistributions = GLSColorDistribution.Parse(StrobeColorDistributions);
        }

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightColorEventBox.ToJson(this),
            };

        public override BaseItem Clone() => new BaseLightColorEventBox(this);

        public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => Events;

        public override void ClearEvents() => Events = Array.Empty<BaseLightColorBase>();

        // Color-lane mutations use the shared occupied-beat replacement invariant before restoring their typed array.
        public override void SetEvents(BaseGLSEvent[] data) =>
            Events = ResolveSameBeatConflicts(data).OfType<BaseLightColorBase>().ToArray();
    }
}
