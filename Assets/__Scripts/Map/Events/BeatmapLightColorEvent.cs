using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightColorEventData: BeatmapObject
{
    public float AddedBeat { get => Time; set => Time = value; } // b, actually it shouldn't be Time, but for simply comparison just make it as Time
    public int TransitionType; // i
    public int Color; // c
    public float Brightness; // s
    public int FlickerFrequency; // f

    public BeatmapLightColorEventData(JSONNode node)
    {
        AddedBeat = RetrieveRequiredNode(node, "b").AsFloat;
        TransitionType = RetrieveRequiredNode(node, "i").AsInt;
        Color = RetrieveRequiredNode(node, "c").AsInt;
        Brightness = RetrieveRequiredNode(node, "s").AsFloat;
        FlickerFrequency = RetrieveRequiredNode(node, "f").AsInt;
    }

    public BeatmapLightColorEventData(float b, int i, int c, float s, int f)
    {
        AddedBeat = b;
        TransitionType = i;
        Color = c;
        Brightness = s;
        FlickerFrequency = f;
    }

    public BeatmapLightColorEventData()
    {
        Brightness = 1;
    }

    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(AddedBeat, DecimalPrecision);
        node["i"] = TransitionType;
        node["c"] = Color;
        node["s"] = Brightness;
        node["f"] = FlickerFrequency;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

public class BeatmapLightColorEventBox: BeatmapObject
{
    public BeatmapLightEventFilter Filter; // f
    public float Distribution; // w
    public int DistributionType; // d
    public float BrightnessDistribution; // r
    public int BrightnessDistributionType; // t
    public int BrightnessAffectFirst; // b
    public List<BeatmapLightColorEventData> EventDatas = new List<BeatmapLightColorEventData>(); // e

    public BeatmapLightColorEventBox(JSONNode node)
    {
        Filter = new BeatmapLightEventFilter(RetrieveRequiredNode(node, "f"));
        Distribution = RetrieveRequiredNode(node, "w").AsFloat;
        DistributionType = RetrieveRequiredNode(node, "d").AsInt;
        BrightnessDistribution = RetrieveRequiredNode(node, "r").AsFloat;
        BrightnessDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        BrightnessAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "e"))
        {
            EventDatas.Add(new BeatmapLightColorEventData(n));
        }
    }

    public BeatmapLightColorEventBox() 
    {
        DistributionType = 1;
        BrightnessDistributionType = 1;
    }

    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["f"] = Filter.ConvertToJson();
        node["w"] = Distribution;
        node["d"] = DistributionType;
        node["r"] = BrightnessDistribution;
        node["t"] = BrightnessDistributionType;
        node["b"] = BrightnessAffectFirst;
        var eventDatas = new JSONArray();
        foreach (var ed in EventDatas) eventDatas.Add(ed.ConvertToJson());

        node["e"] = eventDatas;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

public class BeatmapLightColorEvent : BeatmapObject
{
    public float Beat { get => Time; set => Time = value; } // b
    public int Group; // g
    public List<BeatmapLightColorEventBox> EventBoxes = new List<BeatmapLightColorEventBox>(); // e

    public BeatmapLightColorEvent(JSONNode node)
    {
        Beat = RetrieveRequiredNode(node, "b").AsFloat;
        Group = RetrieveRequiredNode(node, "g").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "e"))
        {
            EventBoxes.Add(new BeatmapLightColorEventBox(n));
        }
    }

    public BeatmapLightColorEvent(float b, int g, BeatmapLightColorEventBox e)
    {
        Beat = b;
        Group = g;
        EventBoxes.Add(e);
    }

    public BeatmapLightColorEvent()
    {
        var filter = new BeatmapLightEventFilter();
        var eb = new BeatmapLightColorEventBox();
        var ebd = new BeatmapLightColorEventData();
        eb.Filter = filter;
        eb.EventDatas.Add(ebd);
        EventBoxes.Add(eb);
    }

    public static List<BeatmapLightColorEvent> SplitEventBoxes(BeatmapLightColorEvent e)
    {
        var ret = new List<BeatmapLightColorEvent>();
        foreach (var eb in e.EventBoxes)
        {
            ret.Add(new BeatmapLightColorEvent(e.Time, e.Group, eb));
        }
        return ret;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightColorEvent;

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

    public Vector2 GetPosition()
    {
        return new Vector2(Group + 0.5f, 0.5f);
    }

    public override void Apply(BeatmapObject originalData)
    {
        var obj = originalData as BeatmapLightColorEvent;
        Beat = obj.Beat;
        Group = obj.Group;
        EventBoxes = obj.EventBoxes;
    }
}
