using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3LightColorEventBox : ILightColorEventBox
    {
        public V3LightColorEventBox()
        {
        }

        public V3LightColorEventBox(JSONNode node)
        {
            IndexFilter = new V3IndexFilter(RetrieveRequiredNode(node, "f"));
            BeatDistribution = RetrieveRequiredNode(node, "w").AsFloat;
            BeatDistributionType = RetrieveRequiredNode(node, "d").AsInt;
            BrightnessDistribution = RetrieveRequiredNode(node, "r").AsFloat;
            BrightnessDistributionType = RetrieveRequiredNode(node, "t").AsInt;
            BrightnessAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
            Events = RetrieveRequiredNode(node, "e").AsArray.Linq.Select(x => new V3LightColorBase(x)).ToArray();
        }

        public V3LightColorEventBox(IIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst,
            ILightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            brightnessDistribution, brightnessDistributionType, brightnessAffectFirst, events)
        {
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["f"] = IndexFilter.ToJson();
            node["w"] = BeatDistribution;
            node["d"] = BeatDistributionType;
            node["r"] = BrightnessDistribution;
            node["t"] = BrightnessDistributionType;
            node["b"] = BrightnessAffectFirst;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["e"] = ary;
            return node;
        }

        public override IItem Clone() =>
            new V3LightColorEventBox((V3IndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                BrightnessDistribution, BrightnessDistributionType, BrightnessAffectFirst, (V3LightColorBase[])Events.Clone());
    }
}
