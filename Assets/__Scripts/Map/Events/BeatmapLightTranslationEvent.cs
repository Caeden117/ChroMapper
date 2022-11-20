using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightTranslationEventData : BeatmapObject
{
    public float AddedBeat { get => Time; set => Time = value; } // b, actually it shouldn't be Time, but for simply comparison just make it as Time
    public int UsePrevious; // p
    public int EaseType; // e
    public float TranslateValue; // t

    public BeatmapLightTranslationEventData(JSONNode node)
    {
        AddedBeat = RetrieveRequiredNode(node, "b").AsFloat;
        UsePrevious = RetrieveRequiredNode(node, "p").AsInt;
        EaseType = RetrieveRequiredNode(node, "e").AsInt;
        TranslateValue = RetrieveRequiredNode(node, "t").AsFloat;
    }

    public BeatmapLightTranslationEventData(float b, int p, int e, float t)
    {
        AddedBeat = b;
        UsePrevious = p;
        EaseType = e;
        TranslateValue = t;
    }

    public BeatmapLightTranslationEventData()
    {
    }

    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(AddedBeat, DecimalPrecision);
        node["p"] = UsePrevious;
        node["e"] = EaseType;
        node["t"] = TranslateValue;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

public class BeatmapLightTranslationEventBox : BeatmapObject
{
    public BeatmapLightEventFilter Filter; // f
    public float Distribution; // w
    public int DistributionType; // d
    public float TranslationDistribution; // s
    public int TranslationDistributionType; // t
    public int Axis; // a
    public int Flip; // r
    public int TranslationAffectFirst; // b
    public int DistributionEaseType; // i
    public List<BeatmapLightTranslationEventData> EventDatas = new List<BeatmapLightTranslationEventData>(); // l

    public BeatmapLightTranslationEventBox(JSONNode node)
    {
        Filter = new BeatmapLightEventFilter(RetrieveRequiredNode(node, "f"));
        Distribution = RetrieveRequiredNode(node, "w").AsFloat;
        DistributionType = RetrieveRequiredNode(node, "d").AsInt;
        TranslationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
        TranslationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        Axis = RetrieveRequiredNode(node, "a").AsInt;
        Flip = RetrieveRequiredNode(node, "r").AsInt;
        TranslationAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
        DistributionEaseType = RetrieveRequiredNode(node, "i").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "l"))
        {
            EventDatas.Add(new BeatmapLightTranslationEventData(n));
        }
    }

    public BeatmapLightTranslationEventBox()
    {
        DistributionType = 1;
        TranslationDistributionType = 1;
    }

    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["f"] = Filter.ConvertToJson();
        node["w"] = Distribution;
        node["d"] = DistributionType;
        node["s"] = TranslationDistribution;
        node["t"] = TranslationDistributionType;
        node["a"] = Axis;
        node["r"] = Flip;
        node["b"] = TranslationAffectFirst;
        node["i"] = DistributionEaseType;
        var eventDatas = new JSONArray();
        foreach (var ed in EventDatas) eventDatas.Add(ed.ConvertToJson());

        node["l"] = eventDatas;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}


public class BeatmapLightTranslationEvent: BeatmapObject
{
    public float Beat { get => Time; set => Time = value; } // b
    public int Group; // g
    public List<BeatmapLightTranslationEventBox> EventBoxes = new List<BeatmapLightTranslationEventBox>(); // e

    public BeatmapLightTranslationEvent(JSONNode node)
    {
        Beat = RetrieveRequiredNode(node, "b").AsFloat;
        Group = RetrieveRequiredNode(node, "g").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "e"))
        {
            EventBoxes.Add(new BeatmapLightTranslationEventBox(n));
        }
    }

    public BeatmapLightTranslationEvent(float b, int g, BeatmapLightTranslationEventBox e)
    {
        Beat = b;
        Group = g;
        EventBoxes.Add(e);
    }

    public BeatmapLightTranslationEvent()
    {
        var filter = new BeatmapLightEventFilter();
        var eb = new BeatmapLightTranslationEventBox();
        var ebd = new BeatmapLightTranslationEventData();
        eb.Filter = filter;
        eb.EventDatas.Add(ebd);
        EventBoxes.Add(eb);
    }

    public static List<BeatmapLightTranslationEvent> SplitEventBoxes(BeatmapLightTranslationEvent e)
    {
        var ret = new List<BeatmapLightTranslationEvent>();
        foreach (var eb in e.EventBoxes)
        {
            ret.Add(new BeatmapLightTranslationEvent(e.Time, e.Group, eb));
        }
        return ret;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightTranslationEvent;

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
        if (originalData is BeatmapLightTranslationEvent obj)
        {
            Beat = obj.Beat;
            Group = obj.Group;
            EventBoxes = obj.EventBoxes;
        }
    }
}
