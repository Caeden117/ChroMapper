using Beatmap.Base;
using SimpleJSON;

namespace Beatmap.V3
{
    public class V3IntFxEvent : IntFxEventBase
    {
        public V3IntFxEvent()
        {

        }

        public V3IntFxEvent(float jsonTime, int usePreviousEventValues, int value)
        {
            JsonTime = jsonTime;
            UsePreviousEventValue = usePreviousEventValues;
            Value = value;
        }

        public V3IntFxEvent(JSONNode node)
        {
            JsonTime = node["b"].AsFloat;
            UsePreviousEventValue = node["p"].AsInt;
            Value = node["v"].AsInt;
        }

        public override BaseItem Clone() => new V3IntFxEvent(JsonTime, UsePreviousEventValue, Value);
        public override JSONNode ToJson()
        {
            return new JSONObject
            {
                ["b"] = JsonTime,
                ["p"] = UsePreviousEventValue,
                ["v"] = Value
            };
        }
    }
}
