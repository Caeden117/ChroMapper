using SimpleJSON;
using System;

public class BeatmapCustomEvent : BeatmapObject
{
    public BeatmapCustomEvent(JSONNode node)
    {
        _time = node["_time"].AsFloat;
        _type = node["_type"].Value;
        _customData = node["_data"];
    }

    public BeatmapCustomEvent(float time, string type, JSONNode data)
    {
        _time = time;
        _type = type;
        _customData = data;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, 3);
        node["_type"] = _type;
        node["_data"] = _customData;
        return node;
    }

    public override Type beatmapType { get; set; } = Type.CUSTOM_EVENT;
    public string _type;
}
