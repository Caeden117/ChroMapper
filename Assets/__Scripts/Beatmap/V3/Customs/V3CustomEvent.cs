using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3CustomEvent : BaseCustomEvent, V3Object
    {
        public V3CustomEvent()
        {
        }

        public V3CustomEvent(BaseCustomEvent other) : base(other)
        {
        }

        public V3CustomEvent(JSONNode node) : base(node)
        {
        }

        public V3CustomEvent(float time, string type, JSONNode node = null) : base(time, type, node)
        {
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";
        public override string KeyTime { get; } = "b";
        public override string KeyType { get; } = "t";
        public override string KeyData { get; } = "d";
        public override string DataKeyDuration { get; } = "duration";
        public override string DataKeyEasing { get; } = "easing";
        public override string DataKeyChildrenTracks { get; } = "childrenTracks";
        public override string DataKeyParentTrack { get; } = "parentTrack";

        public override BaseItem Clone() => new V3CustomEvent(JsonTime, Type, Data.Clone());
    }
}
