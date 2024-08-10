using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public static class V2Bookmark
    {
        public const string KeyTime = "_time";
        public const string KeyName = "_name";
        public const string KeyColor = "_color";

        public static BaseBookmark GetFromJson(JSONNode node) => new BaseBookmark(node);
        public static JSONNode ToJson(BaseBookmark bookmark) => new JSONObject
        {
            [KeyTime] = bookmark.JsonTime, [KeyName] = bookmark.Name, [KeyColor] = bookmark.Color
        };
    }
}
