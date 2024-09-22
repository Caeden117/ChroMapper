using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3
{
    public static class V3Waypoint
    {
        public static BaseWaypoint GetFromJson(JSONNode node)
        {
            var waypoint = new BaseWaypoint();
            
            waypoint.JsonTime = node["b"].AsFloat;
            waypoint.PosX = node["x"].AsInt;
            waypoint.PosY = node["y"].AsInt;
            waypoint.OffsetDirection = node["d"].AsInt;
            waypoint.CustomData = node["customData"];

            return waypoint;
        }

        public static  JSONNode ToJson(BaseWaypoint waypoint)
        {
            JSONNode node = new JSONObject();
            node["b"] = waypoint.JsonTime;
            node["x"] = waypoint.PosX;
            node["y"] = waypoint.PosY;
            node["d"] = waypoint.OffsetDirection;
            waypoint.CustomData = waypoint.SaveCustom();
            if (!waypoint.CustomData.Children.Any()) return node;
            node["customData"] = waypoint.CustomData;
            return node;
        }
    }
}
