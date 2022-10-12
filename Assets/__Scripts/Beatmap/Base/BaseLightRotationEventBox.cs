namespace Beatmap.Base
{
    public abstract class BaseLightRotationEventBox : BaseEventBox
    {
        protected BaseLightRotationEventBox()
        {
        }

        protected BaseLightRotationEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float rotationDistribution, int rotationDistributionType, int rotationAffectFirst,
            int axis, int flip, BaseLightRotationBase[] events) : base(indexFilter, beatDistribution,
            beatDistributionType)
        {
            RotationDistribution = rotationDistribution;
            RotationDistributionType = rotationDistributionType;
            RotationAffectFirst = rotationAffectFirst;
            Axis = axis;
            Flip = flip;
            Events = events;
        }

        public float RotationDistribution { get; set; }
        public int RotationDistributionType { get; set; }
        public int RotationAffectFirst { get; set; }
        public int Axis { get; set; }
        public int Flip { get; set; }
        public BaseLightRotationBase[] Events { get; set; }
    }
}
