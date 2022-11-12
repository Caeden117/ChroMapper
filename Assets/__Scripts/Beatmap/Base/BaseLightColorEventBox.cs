namespace Beatmap.Base
{
    public abstract class BaseLightColorEventBox : BaseEventBox
    {
        protected BaseLightColorEventBox()
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
    }
}
