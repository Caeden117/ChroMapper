using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3VfxEventEventBox : BaseVfxEventEventBox, V3Object
    {
        public V3VfxEventEventBox()
        {
        }

        public V3VfxEventEventBox(JSONNode node)
        {
            IndexFilter = V3IndexFilter.GetFromJson(RetrieveRequiredNode(node, "f"));
            BeatDistribution = node["w"].AsFloat;
            BeatDistributionType = node["d"].AsInt;
            VfxDistribution = node["s"].AsFloat;
            VfxDistributionType = node["t"].AsInt;
            VfxAffectFirst = node["b"].AsInt;
            Easing = node["i"].AsInt;

            if (node.HasKey("l"))
            {
                VfxData = node["l"].AsArray.Linq.Select(x => x.Value.AsInt).ToArray();
            }
        }

        public V3VfxEventEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float vfxDistribution, int vfxDistributionType, int vfxAffectFirst, int[] vfxData) : base(indexFilter,
            beatDistribution, beatDistributionType, vfxDistribution, vfxDistributionType, vfxAffectFirst, vfxData)
        {
        }

        public V3VfxEventEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float vfxDistribution, int vfxDistributionType, int vfxAffectFirst, int easing,
            int[] vfxData) : base(indexFilter, beatDistribution, beatDistributionType,
            vfxDistribution, vfxDistributionType, vfxAffectFirst, easing, vfxData)
        {
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["f"] = IndexFilter.ToJson();
            node["w"] = BeatDistribution;
            node["d"] = BeatDistributionType;
            node["s"] = VfxDistribution;
            node["t"] = VfxDistributionType;
            node["b"] = VfxAffectFirst;
            node["i"] = Easing;

            node["l"] = new JSONArray();
            foreach (var data in VfxData) node["l"].Add(data);

            return node;
        }

        public override BaseItem Clone()
        {
            return new V3VfxEventEventBox((BaseIndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                VfxDistribution, VfxDistributionType, VfxAffectFirst, VfxData.Clone() as int[]);
        }

    }
}
