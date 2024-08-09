using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseLightRotationEventBox : BaseEventBox
    {
        public BaseLightRotationEventBox()
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

        protected BaseLightRotationEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float rotationDistribution, int rotationDistributionType, int rotationAffectFirst,
            int axis, int flip, int easing, BaseLightRotationBase[] events) : base(indexFilter, beatDistribution,
            beatDistributionType, easing)
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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3LightRotationEventBox.ToJson(this)
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
