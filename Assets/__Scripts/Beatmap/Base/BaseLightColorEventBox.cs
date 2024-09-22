using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightColorEventBox : BaseEventBox
    {
        public BaseLightColorEventBox()
        {
        }

        protected BaseLightColorEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            Events = events;
        }

        protected BaseLightColorEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst, int easing,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType, easing)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            Events = events;
        }

        public float BrightnessDistribution { get; set; }
        public int BrightnessDistributionType { get; set; }
        public int BrightnessAffectFirst { get; set; }
        public BaseLightColorBase[] Events { get; set; }

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightColorEventBox.ToJson(this),
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
