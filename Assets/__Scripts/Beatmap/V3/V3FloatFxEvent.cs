using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3FloatFxEvent
    {
        public static BaseFxEventFloat GetFromJson(JSONNode node)
        {
            var floatFxEventBase = new BaseFxEventFloat();

            floatFxEventBase.JsonTime = floatFxEventBase.RelativeJsonTime = node["b"].AsFloat;
            floatFxEventBase.UsePrevious = node["p"].AsInt;
            floatFxEventBase.Value = node["v"].AsFloat;
            floatFxEventBase.Easing = node["i"].AsInt;

            return floatFxEventBase;
        }

        public static JSONNode ToJson(BaseFxEventFloat baseFxEventFloat)
        {
            var node = new JSONObject
            {
                ["b"] = JSONNumber.RoundBeat(baseFxEventFloat.RelativeJsonTime),
                ["p"] = baseFxEventFloat.UsePrevious,
                ["v"] = baseFxEventFloat.Value
            };
            // Extensions hold the previous value, so only normal nodes export easing.
            if (baseFxEventFloat.UsePrevious == 0)
                node["i"] = baseFxEventFloat.Easing;
            return node;
        }
    }
}
