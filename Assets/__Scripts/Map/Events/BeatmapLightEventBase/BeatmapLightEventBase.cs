using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public abstract class BeatmapLightEventBase<TEb, TEbd> : BeatmapObject
    where TEb: BeatmapLightEventBoxBase<TEbd>, new()
    where TEbd: BeatmapLightEventBoxDataBase, new()
{
    public float Beat { get => Time; set => Time = value; } // b
    public int Group; // g
    public List<TEb> EventBoxes = new List<TEb>(); // e


    public BeatmapLightEventBase(JSONNode node)
    {
        LoadFromJson(node);
    }


    public BeatmapLightEventBase<TEb, TEbd> LoadFromJson(JSONNode node)
    {
        Beat = RetrieveRequiredNode(node, "b").AsFloat;
        Group = RetrieveRequiredNode(node, "g").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "e"))
        {
            EventBoxes.Add(new TEb().LoadFromJson(n) as TEb);
        }
        return this;
    }

    public BeatmapLightEventBase()
    {
        var filter = new BeatmapLightEventFilter();
        var eb = new TEb();
        var ebd = new TEbd();
        eb.Filter = filter;
        eb.EventDatas.Add(ebd);
        EventBoxes.Add(eb);
    }

    public static List<BeatmapLightEventBase<TEb, TEbd>> SplitEventBoxes(BeatmapLightEventBase<TEb, TEbd> e)
    {
        var ret = new List<BeatmapLightEventBase<TEb, TEbd>>();
        for (int i = 0; i < e.EventBoxes.Count; ++i)
        {
            var copy = GenerateCopy(e);
            copy.EventBoxes = new List<TEb> { copy.EventBoxes[i] };
            ret.Add(copy);
        }
        return ret;
    }


    public override JSONNode ConvertToJson()
    {
        var node = new JSONObject();
        node["b"] = Math.Round(Beat, DecimalPrecision);
        node["g"] = Group;
        var eventBoxes = new JSONArray();
        foreach (var eb in EventBoxes) eventBoxes.Add(eb.ConvertToJson());

        node["e"] = eventBoxes;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) { return false; }

    public Vector2 GetPosition(PlatformDescriptorV3 platformDescriptor)
    {
        return new Vector2(platformDescriptor.GroupIdToLaneIndex(Group) + 0.5f, 0.5f);
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);
        if (originalData is BeatmapLightEventBase<TEb, TEbd> obj)
        {
            Beat = obj.Beat;
            Group = obj.Group;
            EventBoxes = obj.EventBoxes;
        }
    }
}
