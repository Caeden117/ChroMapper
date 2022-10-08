using System;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public abstract class BaseBpmChange : BaseBpmEvent
    {
        protected BaseBpmChange()
        {
        }

        protected BaseBpmChange(JSONNode node) => InstantiateHelper(ref node);

        protected BaseBpmChange(float time, float bpm) : base(time, bpm)
        {
            Bpm = bpm;
            Time = time;
            BeatsPerBar = 4;
            MetronomeOffset = 4;
        }

        public float BeatsPerBar { get; set; }
        public float MetronomeOffset { get; set; }

        public abstract string KeyTime { get; }
        public abstract string KeyBeatsPerBar { get; }
        public abstract string KeyBpm { get; }
        public abstract string KeyMetronomeOffset { get; }

        protected override bool IsConflictingWithObjectAtSameTime(BaseObject other, bool deletion = false) => true;

        public override void Apply(BaseObject originalData)
        {
            base.Apply(originalData);

            if (originalData is BaseBpmChange bpm)
            {
                Bpm = bpm.Bpm;
                BeatsPerBar = bpm.BeatsPerBar;
                MetronomeOffset = bpm.MetronomeOffset;
            }
        }

        public override JSONNode ToJson() =>
            new JSONObject
            {
                [KeyTime] = Math.Round(Time, DecimalPrecision),
                [KeyBpm] = Bpm,
                [KeyBeatsPerBar] = BeatsPerBar,
                [KeyMetronomeOffset] = MetronomeOffset
            };

        protected void InstantiateHelper(ref JSONNode node)
        {
            Time = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Bpm = RetrieveRequiredNode(node, KeyBpm).AsFloat;
            BeatsPerBar = node[KeyBeatsPerBar]?.AsFloat ?? 4;
            MetronomeOffset = node[KeyMetronomeOffset]?.AsFloat ?? 4;
        }
    }
}
