using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2.Customs
{
    public class V2CustomEvent : BaseCustomEvent, V2Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();

        public V2CustomEvent()
        {
        }

        public V2CustomEvent(BaseCustomEvent other) : base(other)
        {
        }

        public V2CustomEvent(JSONNode node) : base(node)
        {
        }

        public V2CustomEvent(float time, string type, JSONNode node = null) : base(time, type, node)
        {
        }

        public override string CustomKeyTrack { get; } = "_track";
        public override string CustomKeyColor { get; } = "_color";
        public override string KeyTime { get; } = "_time";
        public override string KeyType { get; } = "_type";
        public override string KeyData { get; } = "_data";

        public override BaseItem Clone() => new V2CustomEvent(Time, Type, Data.Clone());
    }
}
