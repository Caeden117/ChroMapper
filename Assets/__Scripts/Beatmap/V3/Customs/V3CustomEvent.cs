using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3CustomEvent : ICustomEvent
    {
        public V3CustomEvent()
        {
        }

        public V3CustomEvent(JSONNode node) : base(node)
        {
        }

        public V3CustomEvent(float time, string type, JSONNode node = null) : base(time, type, node)
        {
        }

        public override string CustomKeyColor { get; } = "c";
        public override string KeyTime { get; } = "b";
        public override string KeyType { get; } = "t";
        public override string KeyData { get; } = "d";

        public override IItem Clone() => new V3CustomEvent(Time, Type, Data.Clone());
    }
}
