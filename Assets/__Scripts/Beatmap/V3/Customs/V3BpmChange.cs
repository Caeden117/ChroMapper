using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;

namespace Beatmap.V3.Customs
{
    public class V3BpmChange : IBpmChange
    {
        public V3BpmChange()
        {
        }

        public V3BpmChange(JSONNode node) : base(node)
        {
        }

        public V3BpmChange(float time, float bpm) : base(time, bpm)
        {
        }

        public override string CustomKeyColor { get; } = "color";
        public override string KeyTime { get; } = "b";
        public override string KeyBeatsPerBar { get; } = "p";
        public override string KeyBpm { get; } = "m";
        public override string KeyMetronomeOffset { get; } = "o";

        public override IItem Clone() => new V3BpmChange(ToJson().Clone());
    }
}
