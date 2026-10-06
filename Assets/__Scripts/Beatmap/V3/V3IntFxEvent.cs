using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3IntFxEvent
    {
        public static BaseFxEventInt GetFromJson(JSONNode node)
        {
            var intFxEventBase = new BaseFxEventInt();
            
            intFxEventBase.JsonTime = node["b"].AsFloat;
            intFxEventBase.UsePrevious = node["p"].AsInt;
            intFxEventBase.Value = node["v"].AsInt;

            return intFxEventBase;
        }

        public static JSONNode ToJson(BaseFxEventInt baseFxEventInt)
        {
            return new JSONObject
            {
                ["b"] = JSONNumber.RoundBeat(baseFxEventInt.JsonTime),
                ["p"] = baseFxEventInt.UsePrevious,
                ["v"] = baseFxEventInt.Value
            };
        }
    }
}
