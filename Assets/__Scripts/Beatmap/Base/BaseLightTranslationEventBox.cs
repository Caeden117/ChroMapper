using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightTranslationEventBox : BaseEventBox
    {
        public BaseLightTranslationEventBox()
        {
        }

        protected BaseLightTranslationEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float translationDistribution, int translationDistributionType,
            int translationAffectFirst,
            int axis, int flip, BaseLightTranslationBase[] events) : base(indexFilter, beatDistribution,
            beatDistributionType)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            Axis = axis;
            Flip = flip;
            Events = events;
        }

        protected BaseLightTranslationEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float translationDistribution, int translationDistributionType,
            int translationAffectFirst,
            int axis, int flip, int easing, BaseLightTranslationBase[] events) : base(indexFilter, beatDistribution,
            beatDistributionType, easing)
        {
            TranslationDistribution = translationDistribution;
            TranslationDistributionType = translationDistributionType;
            TranslationAffectFirst = translationAffectFirst;
            Axis = axis;
            Flip = flip;
            Events = events;
        }

        public float TranslationDistribution { get; set; }
        public int TranslationDistributionType { get; set; }
        public int TranslationAffectFirst { get; set; }
        public int Axis { get; set; }
        public int Flip { get; set; }
        public BaseLightTranslationBase[] Events { get; set; }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightTranslationEventBox.ToJson(this)
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
