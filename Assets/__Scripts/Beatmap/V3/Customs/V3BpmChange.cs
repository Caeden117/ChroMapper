using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3.Customs
{
    public class V3BpmChange : BaseBpmChange, V3Object
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V3BpmChange()
        {
        }

        public V3BpmChange(BaseBpmChange other) : base(other)
        {
        }

        public V3BpmChange(BaseBpmEvent other) : base(other)
        {
        }

        public V3BpmChange(JSONNode node) : base(node)
        {
        }

        public V3BpmChange(float time, float bpm) : base(time, bpm)
        {
        }

        public override string CustomKeyTrack { get; } = "track";
        public override string CustomKeyColor { get; } = "color";
        public override string KeyTime { get; } = "b";
        public override string KeyBeatsPerBar { get; } = "p";
        public override string KeyBpm { get; } = "m";
        public override string KeyMetronomeOffset { get; } = "o";

        public override BaseItem Clone() => new V3BpmChange(ToJson().Clone());
    }
}
