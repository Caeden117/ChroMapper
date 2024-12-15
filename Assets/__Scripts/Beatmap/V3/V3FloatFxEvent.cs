using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public static class V3FloatFxEvent
    {
        public static FloatFxEventBase GetFromJson(JSONNode node)
        {
            var floatFxEventBase = new FloatFxEventBase();
            
            floatFxEventBase.JsonTime = node["b"].AsFloat;
            floatFxEventBase.UsePreviousEventValue = node["p"].AsInt;
            floatFxEventBase.Value = node["v"].AsFloat;
            floatFxEventBase.Easing = node["i"].AsInt;

            return floatFxEventBase;
        }

        public static JSONNode ToJson(FloatFxEventBase floatFxEventBase)
        {
            return new JSONObject
            {
                ["b"] = floatFxEventBase.JsonTime,
                ["p"] = floatFxEventBase.UsePreviousEventValue,
                ["v"] = floatFxEventBase.Value,
                ["i"] = floatFxEventBase.Easing
            };
        }
    }
}
