using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3.Customs
{
    public class V3CustomEvent : BaseCustomEvent
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
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

        public override BaseItem Clone() => new V3CustomEvent(Time, Type, Data.Clone());
    }
}
