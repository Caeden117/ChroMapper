using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V2.Customs
{
    public class V2BpmChange
    {
        public static string KeyTime = "_time";
        public static string KeyBeatsPerBar = "_beatsPerBar";
        public static string KeyBpm = "_BPM";
        public static string KeyMetronomeOffset = "_metronomeOffset";

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
