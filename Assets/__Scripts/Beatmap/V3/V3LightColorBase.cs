using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3LightColorBase : BaseLightColorBase, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        private BaseLightColorBase lightColorBaseImplementation;

        public V3LightColorBase()
        {
        }

        public V3LightColorBase(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "b").AsFloat;
            Color = RetrieveRequiredNode(node, "c").AsInt;
            Brightness = RetrieveRequiredNode(node, "s").AsFloat;
            TransitionType = RetrieveRequiredNode(node, "i").AsInt;
            Frequency = RetrieveRequiredNode(node, "f").AsInt;
            CustomData = node["customData"];
        }

        public V3LightColorBase(float time, int color, float brightness, int transitionType, int frequency,
            JSONNode customData = null) : base(time, color, brightness, transitionType, frequency, customData)
        {
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(JsonTime, DecimalPrecision);
            node["c"] = Color;
            node["s"] = Brightness;
            node["i"] = TransitionType;
            node["f"] = Frequency;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightColorBase(JsonTime, Color, Brightness, TransitionType, Frequency, SaveCustom().Clone());
    }
}
