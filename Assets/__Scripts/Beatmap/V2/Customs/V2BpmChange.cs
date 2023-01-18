using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2.Customs
{
    public class V2BpmChange : BaseBpmChange
    {
        public override void Serialize(NetDataWriter writer) => throw new NotImplementedException();
        public override void Deserialize(NetDataReader reader) => throw new NotImplementedException();
        public V2BpmChange()
        {
        }

        public V2BpmChange(BaseBpmChange other) : base(other)
        {
        }

        public V2BpmChange(BaseBpmEvent other) : base(other)
        {
        }

        public V2BpmChange(JSONNode node) : base(node)
        {
        }

        public V2BpmChange(float time, float bpm) : base(time, bpm)
        {
        }

        public override string CustomKeyTrack { get; } = "_track";
        public override string CustomKeyColor { get; } = "_color";
        public override string KeyTime { get; } = "_time";
        public override string KeyBeatsPerBar { get; } = "_beatsPerBar";
        public override string KeyBpm { get; } = "_BPM";
        public override string KeyMetronomeOffset { get; } = "_metronomeOffset";

        public override BaseItem Clone() => new V2BpmChange(ToJson());
    }
}
