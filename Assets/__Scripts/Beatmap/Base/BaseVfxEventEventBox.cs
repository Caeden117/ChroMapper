using System.Collections.Generic;
using Beatmap.V3;
using SimpleJSON;

namespace Beatmap.Base
{
    public class BaseVfxEventEventBox : BaseEventBox
    {
        public BaseVfxEventEventBox()
        {
        }

        protected BaseVfxEventEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float vfxDistribution, int vfxDistributionType,
            int vfxAffectFirst, int[] vfxData) : base(indexFilter, beatDistribution,
            beatDistributionType)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            VfxData = vfxData;
        }

        protected BaseVfxEventEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float vfxDistribution, int vfxDistributionType,
            int vfxAffectFirst, int easing, int[] vfxData) : base(indexFilter, beatDistribution,
            beatDistributionType, easing)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            VfxData = vfxData;
        }

        public float VfxDistribution { get; set; }
        public int VfxDistributionType { get; set; }
        public int VfxAffectFirst { get; set; }
        public int[] VfxData { get; set; } = { };

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            3 => V3VfxEventEventBox.ToJson(this)
        };

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
