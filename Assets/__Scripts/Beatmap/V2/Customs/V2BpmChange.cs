using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V2.Customs
{
    public class V2BpmChange : BaseBpmChange
    {
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
        public override string KeyBeatsPerBar { get; } = "_BPM";
        public override string KeyBpm { get; } = "_beatsPerBar";
        public override string KeyMetronomeOffset { get; } = "_metronomeOffset";

        public override BaseItem Clone() => new V2BpmChange(ToJson());
    }
}
