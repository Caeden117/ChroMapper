using SimpleJSON;

public class BeatmapBookmark : BeatmapObject
{
    public BeatmapBookmark(JSONNode node)
    {
        _time = node["_time"].AsFloat;
        _name = node["_name"].Value ?? "Invalid Bookmark";
    }

    public BeatmapBookmark(float time, string name)
    {
        _time = time;
        _name = name;
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = _time;
        node["_name"] = _name;
        return node;
    }

    public string _name = "Invalid Bookmark";
    public override Type beatmapType { get; set; } = Type.BPM_CHANGE;
}
