using System;
using System.Linq;
using Beatmap.Base;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2
{
    public static class V2Waypoint
    {
        public static BaseWaypoint GetFromJson(JSONNode node)
        {
            var waypoint = new BaseWaypoint();
            
            waypoint.JsonTime = BaseItem.GetRequiredNode(node, "_time").AsFloat;
            waypoint.PosX = BaseItem.GetRequiredNode(node, "_lineIndex").AsInt;
            waypoint.PosY = BaseItem.GetRequiredNode(node, "_lineLayer").AsInt;
            waypoint.OffsetDirection = BaseItem.GetRequiredNode(node, "_offsetDirection").AsInt;
            waypoint.CustomData = node["_customData"];

            return waypoint;
        }

        public static JSONNode ToJson(BaseWaypoint waypoint)
        {
            JSONNode node = new JSONObject();
            node["_time"] = waypoint.JsonTime;
            node["_lineIndex"] = waypoint.PosX;
            node["_lineLayer"] = waypoint.PosY;
            node["_offsetDirection"] = waypoint.OffsetDirection;
            waypoint.CustomData = waypoint.SaveCustom();
            if (!waypoint.CustomData.Children.Any()) return node;
            node["_customData"] = waypoint.CustomData;
            return node;
        }
    }
}
