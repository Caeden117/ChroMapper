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
            JsonTime = node["b"].AsFloat;
            Color = node["c"].AsInt;
            Brightness = node["s"].AsFloat;
            TransitionType = node["i"].AsInt;
            Frequency = node["f"].AsInt;
            StrobeBrightness = node["sb"].AsFloat;
            StrobeFade = node["sf"].AsInt;
            CustomData = node["customData"];
        }

        public V3LightColorBase(float time, int color, float brightness, int transitionType, int frequency,
            float strobeBrightness, int strobeFade, JSONNode customData = null) : base(time, color, brightness,
            transitionType, frequency, strobeBrightness, strobeFade, customData)
        {
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = JsonTime;
            node["c"] = Color;
            node["s"] = Brightness;
            node["i"] = TransitionType;
            node["f"] = Frequency;
            node["sb"] = StrobeBrightness;
            node["sf"] = StrobeFade;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() =>
            new V3LightColorBase(JsonTime, Color, Brightness, TransitionType, Frequency, StrobeBrightness, StrobeFade,
                SaveCustom().Clone());
    }
}
