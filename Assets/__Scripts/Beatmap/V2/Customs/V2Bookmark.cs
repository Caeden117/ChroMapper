using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2Bookmark : BaseBookmark
    {
        public V2Bookmark()
        {
        }

        public V2Bookmark(JSONNode node) : base(node)
        {
        }

        public V2Bookmark(float time, string name) : base(time, name)
        {
        }

        public override string CustomKeyTrack { get; } = "_track";
        public override string CustomKeyColor { get; } = "_color";
        public override string KeyTime { get; } = "_time";
        public override string KeyName { get; } = "_name";
        public override string KeyColor { get; } = "_color";

        public override BaseItem Clone() => new V2Bookmark(ToJson());
    }
}
