using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3LightTranslationEventBox : BaseLightTranslationEventBox, V3Object
    {
        public V3LightTranslationEventBox()
        {
        }

        public V3LightTranslationEventBox(JSONNode node)
        {
            IndexFilter = new V3IndexFilter(RetrieveRequiredNode(node, "f"));
            BeatDistribution = RetrieveRequiredNode(node, "w").AsFloat;
            BeatDistributionType = RetrieveRequiredNode(node, "d").AsInt;
            TranslationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
            TranslationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
            TranslationAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
            Axis = RetrieveRequiredNode(node, "a").AsInt;
            Flip = RetrieveRequiredNode(node, "r").AsInt;
            Easing = node["i"]?.AsInt ?? 0;
            Events = RetrieveRequiredNode(node, "l").AsArray.Linq.Select(x => new V3LightTranslationBase(x)).ToArray();
        }

        public V3LightTranslationEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float translationDistribution, int translationDistributionType, int translationAffectFirst, int axis,
            int flip,
            BaseLightTranslationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            translationDistribution, translationDistributionType, translationAffectFirst, axis, flip, events)
        {
        }

        public V3LightTranslationEventBox(BaseIndexFilter indexFilter, float beatDistribution, int beatDistributionType,
            float translationDistribution, int translationDistributionType, int translationAffectFirst, int axis,
            int flip, int easing,
            BaseLightTranslationBase[] events) : base(indexFilter, beatDistribution, beatDistributionType,
            translationDistribution, translationDistributionType, translationAffectFirst, axis, flip, easing, events)
        {
        }

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["f"] = IndexFilter.ToJson();
            node["w"] = BeatDistribution;
            node["d"] = BeatDistributionType;
            node["s"] = TranslationDistribution;
            node["t"] = TranslationDistributionType;
            node["b"] = TranslationAffectFirst;
            node["a"] = Axis;
            node["r"] = Flip;
            var ary = new JSONArray();
            foreach (var k in Events) ary.Add(k.ToJson());
            node["l"] = ary;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightTranslationEventBox((V3IndexFilter)IndexFilter.Clone(), BeatDistribution, BeatDistributionType,
                TranslationDistribution, TranslationDistributionType, TranslationAffectFirst, Axis, Flip,
                (V3LightTranslationBase[])Events.Clone());
    }
}
