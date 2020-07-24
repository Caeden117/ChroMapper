using SimpleJSON;
using System;

public class BeatmapCustomEvent : BeatmapObject
{
    public BeatmapCustomEvent(JSONNode node)
    {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _type = RetrieveRequiredNode(node, "_type").Value;
        _customData = RetrieveRequiredNode(node, "_data");
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
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_type"] = _type;
        node["_data"] = _customData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other)
    {
        // There exists a possible edge case where an effect might require 2 or more Custom Events
        // of the same type to be triggered at the exact same time (Start animation, spawn from prefab, etc.)
        // However, right now, they don't exist, so here's a way that they can conflict.
        if (other is BeatmapCustomEvent customEvent)
        {
            return _type == customEvent._type;
        }
        return false;
    }

    public override Type beatmapType { get; set; } = Type.CUSTOM_EVENT;
    public string _type;
}
