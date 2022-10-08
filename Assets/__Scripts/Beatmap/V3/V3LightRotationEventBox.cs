using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3LightRotationEventBox : BaseLightRotationEventBox
    {
        public V3LightRotationEventBox()
        {
        }

        public V3LightRotationEventBox(JSONNode node)
        {
            IndexFilter = new V3IndexFilter(RetrieveRequiredNode(node, "f"));
            BeatDistribution = RetrieveRequiredNode(node, "w").AsFloat;
            BeatDistributionType = RetrieveRequiredNode(node, "d").AsInt;
            RotationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
            RotationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
            RotationAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
            Axis = RetrieveRequiredNode(node, "a").AsInt;
            Flip = RetrieveRequiredNode(node, "r").AsInt;
            Events = RetrieveRequiredNode(node, "l").AsArray.Linq.Select(x => new V3LightRotationBase(x)).ToArray();
        }

        public V3LightRotationEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float rotationDistribution, int rotationDistributionType, int rotationAffectFirst, int axis, int flip,
            BaseLightRotationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            rotationDistribution, rotationDistributionType, rotationAffectFirst, axis, flip, events)
        {
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["f"] = IndexFilter.ToJson();
            node["w"] = BeatDistribution;
            node["d"] = BeatDistributionType;
            node["s"] = RotationDistribution;
            node["t"] = RotationDistributionType;
            node["b"] = RotationAffectFirst;
            node["a"] = Axis;
            node["r"] = Flip;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["l"] = ary;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightRotationEventBox((V3IndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                RotationDistribution, RotationDistributionType, RotationAffectFirst, Axis, Flip, (V3LightRotationBase[])Events.Clone());
    }
}
