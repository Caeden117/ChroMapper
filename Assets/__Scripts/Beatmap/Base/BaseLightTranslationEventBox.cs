using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Enums;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationEventBox : BaseEventBox
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
            beatDistributionType)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            Axis = axis;
            Flip = flip;
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
            easing)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            Axis = axis;
            Flip = flip;
            // Group-level load finalization removes conflicts after parent beat and lane ownership are available for diagnostics.
            Events = events;
        }

        protected BaseLightTranslationEventBox(BaseLightTranslationEventBox other) : base(
            other.IndexFilter.Clone() as BaseIndexFilter,
            other.BeatDistribution,
            other.BeatDistributionType,
            other.Easing)
        {
            TranslationDistribution = other.TranslationDistribution;
            TranslationDistributionType = other.TranslationDistributionType;
            TranslationAffectFirst = other.TranslationAffectFirst;
            Axis = other.Axis;
            Flip = other.Flip;
            IsAutomaticAxisLane = other.IsAutomaticAxisLane;
            Events = other.Events.Select(x => x.Clone()).Cast<BaseLightTranslationBase>().ToArray();
        }

        public float TranslationDistribution { get; set; }
        public int TranslationDistributionType { get; set; }
        public int TranslationAffectFirst { get; set; }
        public int Axis { get; set; }
        public int Flip { get; set; }
        public BaseLightTranslationBase[] Events { get; set; }

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

        public override Axis GetAxis() => (Axis)Axis;
    }
}
