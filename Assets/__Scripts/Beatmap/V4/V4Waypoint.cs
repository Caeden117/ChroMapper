using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using SimpleJSON;

namespace Beatmap.V4
{
    public static class V4Waypoint
    {
        public static BaseWaypoint GetFromJson(JSONNode node, IList<V4CommonData.Waypoint> waypointsCommonData)
        {
            var waypoint = new BaseWaypoint();
            
            waypoint.JsonTime = node["b"].AsFloat;

            var index = node["i"].AsInt;
            var waypointData = waypointsCommonData[index];

            waypoint.PosX = waypointData.PosX;
            waypoint.PosY = waypointData.PosY;
            waypoint.OffsetDirection = waypointData.OffsetDirection;
            
            return waypoint;
        }

        public static JSONNode ToJson(BaseWaypoint waypoint, IList<V4CommonData.Waypoint> waypointsCommonData)
        {
            JSONNode node = new JSONObject();
            node["b"] = waypoint.JsonTime;

            var data = V4CommonData.Waypoint.FromBaseWayPoint(waypoint);
            node["i"] = waypointsCommonData.IndexOf(data);
            
            return node;
        }
    }
}
