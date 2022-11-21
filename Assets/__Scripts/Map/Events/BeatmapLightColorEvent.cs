using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightColorEventData: BeatmapLightEventBoxDataBase
{
    // public float AddedBeat { get => Time; set => Time = value; } // b, actually it shouldn't be Time, but for simply comparison just make it as Time
    public int TransitionType; // i
    public int Color; // c
    public float Brightness; // s
    public int FlickerFrequency; // f

    public BeatmapLightColorEventData(JSONNode node) => LoadFromJson(node);

    public BeatmapLightColorEventData(float b, int i, int c, float s, int f)
    {
        AddedBeat = b;
        TransitionType = i;
        Color = c;
        Brightness = s;
        FlickerFrequency = f;
    }

    public BeatmapLightColorEventData(): base()
    {
        Brightness = 1;
    }
    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        TransitionType = RetrieveRequiredNode(node, "i").AsInt;
        Color = RetrieveRequiredNode(node, "c").AsInt;
        Brightness = RetrieveRequiredNode(node, "s").AsFloat;
        FlickerFrequency = RetrieveRequiredNode(node, "f").AsInt;
    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["i"] = TransitionType;
        node["c"] = Color;
        node["s"] = Brightness;
        node["f"] = FlickerFrequency;
    }
}

public class BeatmapLightColorEventBox: BeatmapLightEventBoxBase<BeatmapLightColorEventData>
{
    // public BeatmapLightEventFilter Filter; // f
    // public float Distribution; // w
    // public int DistributionType; // d
    public float BrightnessDistribution; // r
    public int BrightnessDistributionType; // t
    public int BrightnessAffectFirst; // b
    // public List<BeatmapLightColorEventData> EventDatas = new List<BeatmapLightColorEventData>(); // e
    public override string EventDataKey => "e";

    public BeatmapLightColorEventBox(JSONNode node) => LoadFromJson(node);

    public BeatmapLightColorEventBox(): base()
    {
        BrightnessDistributionType = 1;
    }


    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        BrightnessDistribution = RetrieveRequiredNode(node, "r").AsFloat;
        BrightnessDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        BrightnessAffectFirst = RetrieveRequiredNode(node, "b").AsInt;

    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["r"] = BrightnessDistribution;
        node["t"] = BrightnessDistributionType;
        node["b"] = BrightnessAffectFirst;
    }
}

public class BeatmapLightColorEvent : BeatmapLightEventBase<BeatmapLightColorEventBox, BeatmapLightColorEventData>
{
    public BeatmapLightColorEvent(): base()
    {
    }

    public BeatmapLightColorEvent(JSONNode node) : base(node)
    {
    }

    // public float Beat { get => Time; set => Time = value; } // b
    // public int Group; // g
    // public List<BeatmapLightColorEventBox> EventBoxes = new List<BeatmapLightColorEventBox>(); // e

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightColorEvent;
}
