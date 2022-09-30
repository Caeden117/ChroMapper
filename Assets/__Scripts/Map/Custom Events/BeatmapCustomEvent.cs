using System;
using LiteNetLib.Utils;
using SimpleJSON;

public class BeatmapCustomEvent : BeatmapObject
{
    public string Type;

    public BeatmapCustomEvent() { }

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
    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(Time);
        writer.Put(Type);
        writer.Put(CustomData?.ToString() ?? "null");
    }

    public override void Deserialize(NetDataReader reader)
    {
        Time = reader.GetFloat();
        Type = reader.GetString();
        CustomData = JSON.Parse(reader.GetString());
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
