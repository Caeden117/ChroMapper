using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public class V3Waypoint : BaseWaypoint, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V3Waypoint()
        {
        }

        public V3Waypoint(BaseWaypoint other) : base(other)
        {
        }

        public V3Waypoint(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            PosX = node["x"].AsInt;
            PosY = node["y"].AsInt;
            OffsetDirection = node["d"].AsInt;
            CustomData = node["customData"];
        }

        public V3Waypoint(float time, int posX, int posY, int offsetDirection, JSONNode customData = null) : base(time,
            posX, posY, offsetDirection, customData)
        {
        }

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinates";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override string CustomKeyNoteJumpMovementSpeed { get; } = "noteJumpMovementSpeed";

        public override string CustomKeyNoteJumpStartBeatOffset { get; } = "noteJumpStartBeatOffset";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = JsonTime;
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = OffsetDirection;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3Waypoint(JsonTime, PosX, PosY, OffsetDirection, SaveCustom().Clone());
    }
}
