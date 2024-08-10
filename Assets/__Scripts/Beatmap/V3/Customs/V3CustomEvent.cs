using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3CustomEvent
    {
        public static string CustomKeyTrack = "track";
        public static string CustomKeyColor = "color";
        public static string KeyTime = "b";
        public static string KeyType = "t";
        public static string KeyData = "d";
        public static string DataKeyDuration = "duration";
        public static string DataKeyEasing = "easing";
        public static string DataKeyRepeat = "repeat";
        public static string DataKeyChildrenTracks = "childrenTracks";
        public static string DataKeyParentTrack = "parentTrack";
        public static string DataKeyWorldPositionStays = "worldPositionStays";
        
        public static BaseCustomEvent GetFromJson(JSONNode node) => new BaseCustomEvent(node);

        public static JSONNode ToJson(BaseCustomEvent customEvent) => new JSONObject
        {
            [KeyTime] = customEvent.JsonTime, [KeyType] = customEvent.Type, [KeyData] = customEvent.Data
        };
    }
}
