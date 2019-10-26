using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapCustomEvent : BeatmapObject
{
    public BeatmapCustomEvent(JSONNode node)
    {
        _time = node["_time"].AsFloat;
        _type = node["_type"].Value;
        _data = node["_data"];
    }

    public BeatmapCustomEvent(float time, string type, JSONNode data)
    {
        _time = time;
        _type = type;
        _data = data;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = _time;
        node["_type"] = _type;
        node["_data"] = _data;
        return node;
    }

    public override float _time { get; set; }
    public override Type beatmapType { get; set; } = Type.CUSTOM_EVENT;
    public override JSONNode _customData { get; set; } //Unused as its essentially the same as _data.
    public string _type;
    public JSONNode _data;
}
