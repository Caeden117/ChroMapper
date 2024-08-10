using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3.Customs
{
    public class V3BpmChange
    {
        public static string KeyTime = "b";
        public static string KeyBeatsPerBar = "p";
        public static string KeyBpm = "m";
        public static string KeyMetronomeOffset = "o";

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
