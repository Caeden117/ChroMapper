using System;
using System.Collections.Generic;
using System.Linq;
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
            int vfxAffectFirst, IList<FloatFxEventBase> floatFxEvents) : base(indexFilter, beatDistribution,
            beatDistributionType)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            FloatFxEvents = FloatFxEvents.Select(e => (FloatFxEventBase)e.Clone()).ToList();
        }

        protected BaseVfxEventEventBox(BaseIndexFilter indexFilter, float beatDistribution,
            int beatDistributionType, float vfxDistribution, int vfxDistributionType,
            int vfxAffectFirst, int easing, IList<FloatFxEventBase> floatFxEvents) : base(indexFilter, beatDistribution,
            beatDistributionType, easing)
        {
            VfxDistribution = vfxDistribution;
            VfxDistributionType = vfxDistributionType;
            VfxAffectFirst = vfxAffectFirst;
            FloatFxEvents = FloatFxEvents.Select(e => (FloatFxEventBase)e.Clone()).ToList();
        }

        public float VfxDistribution { get; set; }
        public int VfxDistributionType { get; set; }
        public int VfxAffectFirst { get; set; }

        public List<FloatFxEventBase> FloatFxEvents { get; set; } = new();


        public override JSONNode ToJson() => throw new System.NotImplementedException();

        public override BaseItem Clone() => throw new System.NotImplementedException();
    }
}
