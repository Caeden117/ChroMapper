using SimpleJSON;
using System;

public class BeatmapBookmark : BeatmapObject
{
    public BeatmapBookmark(JSONNode node)
    {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _name = RetrieveRequiredNode(node, "_name");
    }

    public BeatmapBookmark(float time, string name)
    {
        _time = time;
        _name = name;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_name"] = _name;
        return node;
    }

    public string _name = "Invalid Bookmark";
    public override Type beatmapType { get; set; } = Type.BPM_CHANGE;
}
