using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public abstract class BeatmapLightEventBoxBase<TEbd>: BeatmapObject
    where TEbd: BeatmapLightEventBoxDataBase, new()
{
    public BeatmapLightEventFilter Filter; // f
    public float Distribution; // w
    public int DistributionType; // d
    public int DistributionEaseType; // i
    public List<TEbd> EventDatas = new List<TEbd>(); // e or l
    public abstract string EventDataKey { get; }

    public BeatmapLightEventBoxBase()
    {
        DistributionType = 1;
    }

    public BeatmapLightEventBoxBase<TEbd> LoadFromJson(JSONNode node)
    {
        Filter = new BeatmapLightEventFilter(RetrieveRequiredNode(node, "f"));
        Distribution = RetrieveRequiredNode(node, "w").AsFloat;
        DistributionType = RetrieveRequiredNode(node, "d").AsInt;
        LoadFromJsonImpl(ref node);
        DistributionEaseType = node.HasKey("i") ? node["i"].AsInt : 0;
        foreach (var n in RetrieveRequiredNode(node, EventDataKey))
        {
            EventDatas.Add(new TEbd().LoadFromJson(n) as TEbd);
        }
        return this;
    }

    protected abstract void LoadFromJsonImpl(ref JSONNode node);

    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["f"] = Filter.ConvertToJson();
        node["w"] = Distribution;
        node["d"] = DistributionType;
        ConvertToJsonImpl(ref node);
        node["i"] = DistributionEaseType;
        var eventDatas = new JSONArray();
        foreach (var ed in EventDatas) eventDatas.Add(ed.ConvertToJson());

        node[EventDataKey] = eventDatas;
        return node;
    }

    protected abstract void ConvertToJsonImpl(ref JSONNode node);

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

