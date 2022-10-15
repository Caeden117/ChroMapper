using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3Waypoint : BaseWaypoint
    {
        public V3Waypoint()
        {
        }

        public V3Waypoint(BaseWaypoint other) : base(other)
        {
        }

        public V3Waypoint(JSONNode node)
        {
            Time = RetrieveRequiredNode(node, "b").AsFloat;
            PosX = RetrieveRequiredNode(node, "x").AsInt;
            PosY = RetrieveRequiredNode(node, "y").AsInt;
            OffsetDirection = RetrieveRequiredNode(node, "d").AsInt;
            CustomData = node["customData"];
        }

        public V3Waypoint(float time, int posX, int posY, int offsetDirection, JSONNode customData = null) : base(time,
            posX, posY, offsetDirection, customData)
        {
        }

        public override string CustomKeyTrack { get; } = "track";

        public override string CustomKeyColor { get; } = "color";

        public override string CustomKeyCoordinate { get; } = "coordinate";

        public override string CustomKeyWorldRotation { get; } = "worldRotation";

        public override string CustomKeyLocalRotation { get; } = "localRotation";

        public override JSONNode ToJson()
        {
            JSONNode node = new JSONObject();
            node["b"] = Math.Round(Time, DecimalPrecision);
            node["x"] = PosX;
            node["y"] = PosY;
            node["d"] = OffsetDirection;
            CustomData = SaveCustom();
            if (!CustomData.Children.Any()) return node;
            node["customData"] = CustomData;
            return node;
        }

        public override BaseItem Clone() => new V3Waypoint(Time, PosX, PosY, OffsetDirection, CustomData?.Clone());
    }
}
