using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightRotationEventData : BeatmapLightEventBoxDataBase
{
    // public float AddedBeat { get => Time; set => Time = value; } // b
    public int Transition; // p
    public int EaseType; // e
    public int AdditionalLoop; // l
    public float RotationValue; // r
    public int RotationDirection; // o

    public BeatmapLightRotationEventData(JSONNode node) => LoadFromJson(node);

    public BeatmapLightRotationEventData(float b, int p, int e, int l, float r, int o)
    {
        AddedBeat = b;
        Transition = p;
        EaseType = e;
        AdditionalLoop = l;
        RotationValue = r;
        RotationDirection = o;
    }

    public BeatmapLightRotationEventData(): base()
    {
        EaseType = -1;
    }

    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        Transition = RetrieveRequiredNode(node, "p").AsInt;
        EaseType = RetrieveRequiredNode(node, "e").AsInt;
        AdditionalLoop = RetrieveRequiredNode(node, "l").AsInt;
        RotationValue = RetrieveRequiredNode(node, "r").AsFloat;
        RotationDirection = RetrieveRequiredNode(node, "o").AsInt;
    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["p"] = Transition;
        node["e"] = EaseType;
        node["l"] = AdditionalLoop;
        node["r"] = RotationValue;
        node["o"] = RotationDirection;
    }
}

public class BeatmapLightRotationEventBox : BeatmapLightEventBoxBase<BeatmapLightRotationEventData>
{
    // public BeatmapLightEventFilter Filter; // f
    // public float Distribution; // w
    // public int DistributionType; // d
    public float RotationDistribution; // s
    public int RotationDistributionType; // t
    public int RotationAffectFirst; // b
    public int Axis; // a
    public int ReverseRotation; // r
    // public List<BeatmapLightRotationEventData> EventDatas = new List<BeatmapLightRotationEventData>(); // l

    public override string EventDataKey => "l";
    public BeatmapLightRotationEventBox(JSONNode node) => LoadFromJson(node);

    public BeatmapLightRotationEventBox(): base()
    {
        RotationDistributionType = 1;
    }

    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        RotationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
        RotationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        RotationAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
        Axis = RetrieveRequiredNode(node, "a").AsInt;
        ReverseRotation = RetrieveRequiredNode(node, "r").AsInt;
    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["s"] = RotationDistribution;
        node["t"] = RotationDistributionType;
        node["b"] = RotationAffectFirst;
        node["a"] = Axis;
        node["r"] = ReverseRotation;
    }
}

public class BeatmapLightRotationEvent : BeatmapLightEventBase<BeatmapLightRotationEventBox, BeatmapLightRotationEventData>
{
    // public float Beat { get => Time; set => Time = value; } // b
    // public int Group; // g
    // public List<BeatmapLightRotationEventBox> EventBoxes = new List<BeatmapLightRotationEventBox>(); // e

    public BeatmapLightRotationEvent(JSONNode node) : base(node)
    {
    }


    public BeatmapLightRotationEvent(): base()
    {
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightRotationEvent;
}
