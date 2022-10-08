namespace Beatmap.Base
{
    public abstract class ILightRotationEventBox : IEventBox
    {
        protected ILightRotationEventBox()
        {
        }

        protected ILightRotationEventBox(IIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float rotationDistribution, int rotationDistributionType, int rotationAffectFirst,
            int axis, int flip, ILightRotationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType)
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
        public ILightRotationBase[] Events { get; set; }
    }
}
