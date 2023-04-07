using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2
{
    public class V2Waypoint : BaseWaypoint, V2Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V2Waypoint()
        {
        }

        public V2Waypoint(BaseWaypoint other) : base(other)
        {
        }

        public V2Waypoint(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, "_time").AsFloat;
            PosX = RetrieveRequiredNode(node, "_lineIndex").AsInt;
            PosY = RetrieveRequiredNode(node, "_lineLayer").AsInt;
            OffsetDirection = RetrieveRequiredNode(node, "_offsetDirection").AsInt;
            CustomData = node["_customData"];
        }

        public V2Waypoint(float time, int posX, int posY, int offsetDirection, JSONNode customData = null) : base(time,
            posX, posY, offsetDirection, customData)
        {
        }

        public override string CustomKeyTrack { get; } = "_track";

        public override string CustomKeyColor { get; } = "_color";

        public override string CustomKeyCoordinate { get; } = "_position";

        public override string CustomKeyWorldRotation { get; } = "_rotation";

        public override string CustomKeyLocalRotation { get; } = "_localRotation";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["_time"] = Math.Round(JsonTime, DecimalPrecision);
            node["_lineIndex"] = PosX;
            node["_lineLayer"] = PosY;
            node["_offsetDirection"] = OffsetDirection;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["_customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V2Waypoint(JsonTime, PosX, PosY, OffsetDirection, SaveCustom().Clone());
    }
}
