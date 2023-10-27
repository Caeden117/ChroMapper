using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3LightTranslationBase : BaseLightTranslationBase, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V3LightTranslationBase()
        {
        }

        public V3LightTranslationBase(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            UsePrevious = node["p"].AsInt;
            EaseType = node["e"].AsInt;
            Translation = node["t"].AsFloat;
            CustomData = node["customData"];
        }

        public V3LightTranslationBase(float time, float translation, int easeType, int usePrevious,
            JSONNode customData = null) : base(time, translation, easeType, usePrevious, customData)
        {
        }

        public override Color? CustomColor
        {
            get => null;
            set { }
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = JsonTime;
            node["p"] = UsePrevious;
            node["e"] = EaseType;
            node["t"] = Translation;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3LightTranslationBase(JsonTime, Translation, EaseType,
            UsePrevious, SaveCustom().Clone());
    }
}
