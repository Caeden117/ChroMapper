using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3CustomEvent
    {
        public const string CustomKeyTrack = "track";
        public const string CustomKeyColor = "color";
        public const string KeyTime = "b";
        public const string KeyType = "t";
        public const string KeyData = "d";
        public const string DataKeyDuration = "duration";
        public const string DataKeyEasing = "easing";
        public const string DataKeyRepeat = "repeat";
        public const string DataKeyChildrenTracks = "childrenTracks";
        public const string DataKeyParentTrack = "parentTrack";
        public const string DataKeyWorldPositionStays = "worldPositionStays";
        
        public static BaseCustomEvent GetFromJson(JSONNode node) => new BaseCustomEvent(node);

        public static JSONNode ToJson(BaseCustomEvent customEvent) => new JSONObject
        {
            [KeyTime] = customEvent.JsonTime, [KeyType] = customEvent.Type, [KeyData] = customEvent.Data
        };
    }
}
