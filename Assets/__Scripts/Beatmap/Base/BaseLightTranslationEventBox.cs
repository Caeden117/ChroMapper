using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationEventBox : BaseLightTransformEventBox
    {
        public BaseLightTranslationEventBox()
        {
            TranslationDistributionType = (int)DistributionType.Wave;
            Events = Array.Empty<BaseLightTranslationBase>();
        }

        protected BaseLightTranslationEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float translationDistribution,
            int translationDistributionType,
            int translationAffectFirst,
            int axis,
            int flip,
            BaseLightTranslationBase[] events) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType,
            axis,
            flip)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightTranslationEventBox(
            BaseIndexFilter indexFilter,
            float beatDistribution,
            int beatDistributionType,
            float translationDistribution,
            int translationDistributionType,
            int translationAffectFirst,
            int axis,
            int flip,
            int easing,
            BaseLightTranslationBase[] events) : base(
            indexFilter,
            beatDistribution,
            beatDistributionType,
            easing,
            axis,
            flip)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightTranslationEventBox(BaseLightTranslationEventBox other) : base(other)
        {
            TranslationDistribution = other.TranslationDistribution;
            TranslationDistributionType = other.TranslationDistributionType;
            TranslationAffectFirst = other.TranslationAffectFirst;
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightTranslationBase>().ToArray();
        }

        public float TranslationDistribution { get; set; }
        public int TranslationDistributionType { get; set; }
        public int TranslationAffectFirst { get; set; }
        public BaseLightTranslationBase[] Events { get; set; }

        // Map to shared transform interface
        public override float ValueDistribution
        {
            get => TranslationDistribution;
            set => TranslationDistribution = value;
        }

        public override int ValueDistributionType
        {
            get => TranslationDistributionType;
            set => TranslationDistributionType = value;
        }

        public override int AffectFirst
        {
            get => TranslationAffectFirst;
            set => TranslationAffectFirst = value;
        }

        public override float ValueDistributionDisplayScale => 100f;

        public override bool AcceptsEvent(BaseGLSEvent evt) => evt is BaseLightTranslationBase;

        public override JSONNode ToJson() =>
            Settings.Instance.MapVersion switch
            {
                3 or 4 => V3LightTranslationEventBox.ToJson(this)
            };

        public override BaseItem Clone() => new BaseLightTranslationEventBox(this);

        public override IReadOnlyList<BaseGLSEvent> ReadOnlyEvents => Events;

        public override void ClearEvents() => Events = Array.Empty<BaseLightTranslationBase>();

        // Translation-axis mutations use the shared occupied-beat replacement invariant before restoring their typed array.
        public override void SetEvents(BaseGLSEvent[] data) =>
            Events = ResolveSameBeatConflicts(data).OfType<BaseLightTranslationBase>().ToArray();

    }
}
