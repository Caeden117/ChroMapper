using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3LightRotationEventBox : BaseLightRotationEventBox, V3Object
    {
        public V3LightRotationEventBox()
        {
        }

        public V3LightRotationEventBox(JSONNode node)
        {
            IndexFilter = new V3IndexFilter(RetrieveRequiredNode(node, "f"));
            BeatDistribution = node["w"].AsFloat;
            BeatDistributionType = node["d"].AsInt;
            RotationDistribution = node["s"].AsFloat;
            RotationDistributionType = node["t"].AsInt;
            RotationAffectFirst = node["b"].AsInt;
            Axis = node["a"].AsInt;
            Flip = node["r"].AsInt;
            Easing = node["i"].AsInt;
            Events = RetrieveRequiredNode(node, "l").AsArray.Linq.Select(x => new V3LightRotationBase(x)).ToArray();
        }

        public V3LightRotationEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float rotationDistribution, int rotationDistributionType, int rotationAffectFirst, int axis, int flip,
            BaseLightRotationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            rotationDistribution, rotationDistributionType, rotationAffectFirst, axis, flip, events)
        {
        }

        public V3LightRotationEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float rotationDistribution, int rotationDistributionType, int rotationAffectFirst, int axis, int flip,
            int easing,
            BaseLightRotationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            rotationDistribution, rotationDistributionType, rotationAffectFirst, axis, flip, easing, events)
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
            node["i"] = Easing;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["l"] = ary;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightRotationEventBox((V3IndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                RotationDistribution, RotationDistributionType, RotationAffectFirst, Axis, Flip,
                (V3LightRotationBase[])Events.Clone());
    }
}
