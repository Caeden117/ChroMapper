using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightRotationEventData : BeatmapObject
{
    public float AddedBeat { get => Time; set => Time = value; } // b
    public int Transition; // p
    public int EaseType; // e
    public int AdditionalLoop; // l
    public float RotationValue; // r
    public int RotationDirection; // o

    public BeatmapLightRotationEventData(JSONNode node)
    {
        AddedBeat = RetrieveRequiredNode(node, "b").AsFloat;
        Transition = RetrieveRequiredNode(node, "p").AsInt;
        EaseType = RetrieveRequiredNode(node, "e").AsInt;
        AdditionalLoop = RetrieveRequiredNode(node, "l").AsInt;
        RotationValue = RetrieveRequiredNode(node, "r").AsFloat;
        RotationDirection = RetrieveRequiredNode(node, "o").AsInt;
    }

    public BeatmapLightRotationEventData(float b, int p, int e, int l, float r, int o)
    {
        AddedBeat = b;
        Transition = p;
        EaseType = e;
        AdditionalLoop = l;
        RotationValue = r;
        RotationDirection = o;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightColorEvent;

    public override JSONNode ConvertToJson()
    {
        var node = new JSONObject();
        node["b"] = Math.Round(AddedBeat, DecimalPrecision);
        node["p"] = Transition;
        node["e"] = EaseType;
        node["l"] = AdditionalLoop;
        node["r"] = RotationValue;
        node["o"] = RotationDirection;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

public class BeatmapLightRotationEventBox : BeatmapObject
{
    public BeatmapLightEventFilter Filter; // f
    public float Distribution; // w
    public int DistributionType; // d
    public float RotationDistribution; // s
    public int RotationDistributionType; // t
    public int RotationAffectFirst; // b
    public int Axis; // a
    public int ReverseRotation; // r
    public List<BeatmapLightRotationEventData> EventDatas = new List<BeatmapLightRotationEventData>(); // l

    public BeatmapLightRotationEventBox(JSONNode node)
    {
        Filter = new BeatmapLightEventFilter(RetrieveRequiredNode(node, "f"));
        Distribution = RetrieveRequiredNode(node, "w").AsFloat;
        DistributionType = RetrieveRequiredNode(node, "d").AsInt;
        RotationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
        RotationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        Axis = RetrieveRequiredNode(node, "a").AsInt;
        ReverseRotation = RetrieveRequiredNode(node, "r").AsInt;
        foreach (var n in RetrieveRequiredNode(node, "l"))
        {
            EventDatas.Add(new BeatmapLightRotationEventData(n));
        }
    }
    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        var node = new JSONObject();
        node["f"] = Filter.ConvertToJson();
        node["w"] = Distribution;
        node["d"] = DistributionType;
        node["s"] = RotationDistribution;
        node["t"] = RotationDistributionType;
        node["b"] = RotationAffectFirst;
        node["a"] = Axis;
        node["r"] = ReverseRotation;
        var eventDatas = new JSONArray();
        foreach (var ed in EventDatas) eventDatas.Add(ed.ConvertToJson());

        node["l"] = eventDatas;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}

public class BeatmapLightRotationEvent : BeatmapObject
{
    public float Beat { get => Time; set => Time = value; } // b
    public int Group; // g
    public List<BeatmapLightRotationEventBox> EventBoxes = new List<BeatmapLightRotationEventBox>(); // e

    public BeatmapLightRotationEvent(JSONNode node)
    {
        Beat = RetrieveRequiredNode(node, "b");
        Group = RetrieveRequiredNode(node, "g");
        foreach (var n in RetrieveRequiredNode(node, "e"))
        {
            EventBoxes.Add(new BeatmapLightRotationEventBox(n));
        }
    }

    public BeatmapLightRotationEvent(float b, int g, BeatmapLightRotationEventBox e)
    {
        Beat = b;
        Group = g;
        EventBoxes.Add(e);
    }

    public static List<BeatmapLightRotationEvent> SplitEventBoxes(BeatmapLightRotationEvent e)
    {
        var ret = new List<BeatmapLightRotationEvent>();
        foreach (var eb in e.EventBoxes)
        {
            ret.Add(new BeatmapLightRotationEvent(e.Time, e.Group, eb));
        }
        return ret;
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightRotationEvent;

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
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();

    public Vector2 GetPosition()
    {
        return new Vector2(Group + 0.5f, 0.5f);
    }
}
