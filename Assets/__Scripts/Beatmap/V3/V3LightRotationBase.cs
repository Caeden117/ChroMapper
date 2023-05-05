using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using UnityEngine;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3LightRotationBase : BaseLightRotationBase, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V3LightRotationBase()
        {
        }

        public V3LightRotationBase(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "b").AsFloat;
            Rotation = RetrieveRequiredNode(node, "r").AsFloat;
            Direction = RetrieveRequiredNode(node, "o").AsInt;
            EaseType = RetrieveRequiredNode(node, "e").AsInt;
            Loop = RetrieveRequiredNode(node, "l").AsInt;
            UsePrevious = RetrieveRequiredNode(node, "p").AsInt;
            CustomData = node["customData"];
        }

        public V3LightRotationBase(float time, float rotation, int direction, int easeType, int loop, int usePrevious,
            JSONNode customData = null) : base(time, rotation, direction, easeType, loop, usePrevious, customData)
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
            node["b"] = Math.Round(JsonTime, DecimalPrecision);
            node["r"] = Rotation;
            node["o"] = Direction;
            node["e"] = EaseType;
            node["l"] = Loop;
            node["p"] = UsePrevious;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3LightRotationBase(JsonTime, Rotation, Direction, EaseType, Loop,
            UsePrevious, SaveCustom().Clone());
    }
}
