using System;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public class BaseBpmChange : BaseBpmEvent
    {
        public BaseBpmChange()
        {
        }

        protected BaseBpmChange(BaseBpmChange other)
        {
            SetTimes(other.JsonTime);
            Bpm = other.Bpm;
            BeatsPerBar = other.BeatsPerBar;
            MetronomeOffset = other.MetronomeOffset;
        }

        protected BaseBpmChange(BaseBpmEvent other)
        {
            SetTimes(other.JsonTime);
            Bpm = other.Bpm;
            BeatsPerBar = 4;
            MetronomeOffset = 4;
        }

        public BaseBpmChange(JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Bpm = RetrieveRequiredNode(node, KeyBpm).AsFloat;
            BeatsPerBar = node.HasKey(KeyBeatsPerBar) ? node[KeyBeatsPerBar].AsFloat : 4f;
            MetronomeOffset = node.HasKey(KeyMetronomeOffset) ? node[KeyMetronomeOffset].AsFloat : 4f;
        }

        protected BaseBpmChange(float time, float bpm) : base(time, bpm)
        {
            Bpm = bpm;
            BeatsPerBar = 4;
            MetronomeOffset = 4;
        }

        public float BeatsPerBar { get; set; }
        public float MetronomeOffset { get; set; }

        public string KeyTime => Settings.Instance.MapVersion switch
        {
            2 => V2BpmChange.KeyTime,
            3 => V3BpmChange.KeyTime
        };

        public string KeyBeatsPerBar => Settings.Instance.MapVersion switch
        {
            2 => V2BpmChange.KeyBeatsPerBar,
            3 => V3BpmChange.KeyBeatsPerBar
        };

        public string KeyBpm => Settings.Instance.MapVersion switch
        {
            2 => V2BpmChange.KeyBpm,
            3 => V3BpmChange.KeyBpm
        };

        public string KeyMetronomeOffset => Settings.Instance.MapVersion switch
        {
            2 => V2BpmChange.KeyMetronomeOffset,
            3 => V3BpmChange.KeyMetronomeOffset
        };


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

        public override JSONNode ToJson() => Settings.Instance.MapVersion switch
        {
            2 => V2BpmChange.ToJson(this),
            3 => V3BpmChange.ToJson(this)
        };
    }
}
