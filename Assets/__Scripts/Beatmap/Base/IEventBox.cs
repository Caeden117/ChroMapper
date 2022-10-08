namespace Beatmap.Base
{
    public abstract class IEventBox : IItem
    {
        protected IEventBox()
        {
        }

        protected IEventBox(IIndexFilter indexFilter, float beatDistribution, int beatDistributionType)
        {
            IndexFilter = indexFilter;
            BeatDistribution = beatDistribution;
            BeatDistributionType = beatDistributionType;
        }

        public IIndexFilter IndexFilter { get; set; }
        public float BeatDistribution { get; set; }
        public int BeatDistributionType { get; set; }
    }
}
