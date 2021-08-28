using System;
using SimpleJSON;

public class BeatmapCustomEvent : BeatmapObject
{
    public string Type;

    public BeatmapCustomEvent(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "_time").AsFloat;
        Type = RetrieveRequiredNode(node, "_type").Value;
        CustomData = RetrieveRequiredNode(node, "_data");
    }

    public BeatmapCustomEvent(float time, string type, JSONNode data)
    {
        Time = time;
        Type = type;
        CustomData = data;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.CustomEvent;

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(Time, DecimalPrecision);
        node["_type"] = Type;
        node["_data"] = CustomData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (deletion)
            return Type == (other as BeatmapCustomEvent).Type;
        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapCustomEvent ev) Type = ev.Type;
    }
}
