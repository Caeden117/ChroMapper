using System;
using Beatmap.Base;
using Beatmap.Base.Customs;
using SimpleJSON;
using LiteNetLib.Utils;

namespace Beatmap.V3.Customs
{
    public class V3BpmChange
    {
        public const string KeyTime = "b";
        public const string KeyBeatsPerBar = "p";
        public const string KeyBpm = "m";
        public const string KeyMetronomeOffset = "o";

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
