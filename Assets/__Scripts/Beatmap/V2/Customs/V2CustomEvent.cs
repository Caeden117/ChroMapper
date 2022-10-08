using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2CustomEvent : ICustomEvent
    {
        public V2CustomEvent()
        {
        }

        public V2CustomEvent(JSONNode node) : base(node)
        {
        }

        public V2CustomEvent(float time, string type, JSONNode node = null) : base(time, type, node)
        {
        }

        public override string CustomKeyColor { get; } = "_color";
        public override string KeyTime { get; } = "_time";
        public override string KeyType { get; } = "_type";
        public override string KeyData { get; } = "_data";

        public override IItem Clone() => new V2CustomEvent(Time, Type, Data.Clone());
    }
}
