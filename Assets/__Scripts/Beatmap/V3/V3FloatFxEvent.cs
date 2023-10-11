using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3FloatFxEvent : FloatFxEventBase
    {
        public V3FloatFxEvent()
        {

        }

        public V3FloatFxEvent(float jsonTime, int usePreviousEventValues, float value, int easing)
        {
            JsonTime = jsonTime;
            UsePreviousEventValue = usePreviousEventValues;
            Value = value;
            Easing = easing;
        }

        public V3FloatFxEvent(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            UsePreviousEventValue = node["p"].AsInt;
            Value = node["v"].AsFloat;
            Easing = node["i"].AsInt;
        }

        public override BaseItem Clone() => new V3FloatFxEvent(JsonTime, UsePreviousEventValue, Value, Easing);
        public override JSONNode ToJson()
        {
            return new JSONObject
            {
                ["b"] = JsonTime,
                ["p"] = UsePreviousEventValue,
                ["v"] = Value,
                ["i"] = Easing
            };
        }
    }
}
