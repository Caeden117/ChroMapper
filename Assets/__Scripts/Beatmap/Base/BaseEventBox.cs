namespace Beatmap.Base
{
    public abstract class BaseEventBox : BaseItem
    {
        protected BaseEventBox()
        {
        }

        protected BaseEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
        }

        public BaseIndexFilter IndexFilter { get; set; }
        public float BeatDistribution { get; set; }
        public int BeatDistributionType { get; set; }
    }
}
