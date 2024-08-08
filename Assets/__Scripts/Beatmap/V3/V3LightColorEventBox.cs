using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3LightColorEventBox : BaseLightColorEventBox, V3Object
    {
        public V3LightColorEventBox()
        {
        }

        public V3LightColorEventBox(JSONNode node)
        {
            IndexFilter = V3IndexFilter.GetFromJson(GetRequiredNode(node, "f"));
            BeatDistribution = node["w"].AsFloat;
            BeatDistributionType = node["d"].AsInt;
            BrightnessDistribution = node["r"].AsFloat;
            BrightnessDistributionType = node["t"].AsInt;
            BrightnessAffectFirst = node["b"].AsInt;
            Easing = node["i"].AsInt;
            Events = RetrieveRequiredNode(node, "e").AsArray.Linq.Select(x => new V3LightColorBase(x)).ToArray();
        }

        public V3LightColorEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            brightnessDistribution, brightnessDistributionType, brightnessAffectFirst, events)
        {
        }

        public V3LightColorEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float brightnessDistribution, int brightnessDistributionType, int brightnessAffectFirst, int easing,
            BaseLightColorBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            brightnessDistribution, brightnessDistributionType, brightnessAffectFirst, easing, events)
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
            node["i"] = Easing;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["e"] = ary;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightColorEventBox((V3IndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                BrightnessDistribution, BrightnessDistributionType, BrightnessAffectFirst,
                (V3LightColorBase[])Events.Clone());
    }
}
