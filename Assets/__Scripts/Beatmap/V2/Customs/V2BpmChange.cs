using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2.Customs
{
    public class V2BpmChange
    {
        public const string KeyTime = "_time";
        public const string KeyBeatsPerBar = "_beatsPerBar";
        public const string KeyBpm = "_BPM";
        public const string KeyMetronomeOffset = "_metronomeOffset";

        public static BaseBpmChange GetFromJson(JSONNode node) => new BaseBpmChange(node);

        public static JSONNode ToJson(BaseBpmChange bpmChange) => new JSONObject
        {
            [KeyTime] = bpmChange.JsonTime,
            [KeyBpm] = bpmChange.Bpm,
            [KeyBeatsPerBar] = bpmChange.BeatsPerBar,
            [KeyMetronomeOffset] = bpmChange.MetronomeOffset
        };
    }
}
