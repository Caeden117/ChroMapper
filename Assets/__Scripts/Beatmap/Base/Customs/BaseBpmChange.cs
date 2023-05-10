﻿using System;
using SimpleJSON;

namespace Beatmap.Base.Customs
{
    public abstract class BaseBpmChange : BaseBpmEvent
    {
        protected BaseBpmChange()
        {
        }

        protected BaseBpmChange(BaseBpmChange other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Bpm = other.Bpm;
            BeatsPerBar = other.BeatsPerBar;
            MetronomeOffset = other.MetronomeOffset;
        }

        protected BaseBpmChange(BaseBpmEvent other)
        {
            SetTimes(other.JsonTime, other.SongBpmTime);
            Bpm = other.Bpm;
            BeatsPerBar = 4;
            MetronomeOffset = 4;
        }

        protected BaseBpmChange(JSONNode node) => InstantiateHelper(ref node);

        protected BaseBpmChange(float time, float bpm) : base(time, bpm)
        {
            Bpm = bpm;
            BeatsPerBar = 4;
            MetronomeOffset = 4;
        }

        protected BaseBpmChange(float jsonTime, float songBpmTime, float bpm) : base(jsonTime, songBpmTime, bpm)
        {
            Bpm = bpm;
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
                [KeyTime] = Math.Round(JsonTime, DecimalPrecision),
                [KeyBpm] = Bpm,
                [KeyBeatsPerBar] = BeatsPerBar,
                [KeyMetronomeOffset] = MetronomeOffset
            };

        private void InstantiateHelper(ref JSONNode node)
        {
            JsonTime = RetrieveRequiredNode(node, KeyTime).AsFloat;
            Bpm = RetrieveRequiredNode(node, KeyBpm).AsFloat;
            BeatsPerBar = node.HasKey(KeyBeatsPerBar) ? node[KeyBeatsPerBar].AsFloat : 4f;
            MetronomeOffset = node.HasKey(KeyMetronomeOffset) ? node[KeyMetronomeOffset].AsFloat : 4f;
        }
    }
}
