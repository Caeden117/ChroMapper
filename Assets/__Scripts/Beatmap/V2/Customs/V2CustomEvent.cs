using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2CustomEvent
    {
        public static string CustomKeyTrack = "_track";
        public static string CustomKeyColor = "_color";
        public static string KeyTime = "_time";
        public static string KeyType = "_type";
        public static string KeyData = "_data";
        public static string DataKeyDuration = "_duration";
        public static string DataKeyEasing = "_easing";
        public static string DataKeyRepeat = "_repeat";
        public static string DataKeyChildrenTracks = "_childrenTracks";
        public static string DataKeyParentTrack = "_parentTrack";
        public static string DataKeyWorldPositionStays = "_worldPositionStays";

        public static BaseCustomEvent GetFromJson(JSONNode node) => new BaseCustomEvent(node);

        public static JSONNode ToJson(BaseCustomEvent customEvent) => new JSONObject
        {
            [KeyTime] = customEvent.JsonTime, [KeyType] = customEvent.Type, [KeyData] = customEvent.Data
        };
    }
}
