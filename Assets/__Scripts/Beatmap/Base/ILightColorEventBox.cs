namespace Beatmap.Base
{
    public abstract class ILightColorEventBox : IEventBox
    {
        protected ILightColorEventBox()
        {
        }

        protected ILightColorEventBox(IIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst,
            ILightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType)
        {
            BrightnessDistribution = brightnessDistribution;
            BrightnessDistributionType = brightnessDistributionType;
            BrightnessAffectFirst = brightnessAffectFirst;
            Events = events;
        }

        public float BrightnessDistribution { get; set; }
        public int BrightnessDistributionType { get; set; }
        public int BrightnessAffectFirst { get; set; }
        public ILightColorBase[] Events { get; set; }
    }
}
