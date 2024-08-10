using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public static class V3Bookmark
    {
        public static string KeyTime = "b";
        public static string KeyName = "n";
        public static string KeyColor = "c";

        public static BaseBookmark GetFromJson(JSONNode node) => new BaseBookmark(node);
        public static JSONNode ToJson(BaseBookmark bookmark) => new JSONObject
        {
            [KeyTime] = bookmark.JsonTime, [KeyName] = bookmark.Name, [KeyColor] = bookmark.Color
        };
    }
}
