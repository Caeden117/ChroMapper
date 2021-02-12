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

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (deletion)
        {
            return _type == (other as BeatmapCustomEvent)._type;
        }
        else
        {
            return false;
        }
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapCustomEvent ev)
        {
            _type = ev._type;
        }
    }

    public override Type beatmapType { get; set; } = Type.CUSTOM_EVENT;
    public string _type;
}
