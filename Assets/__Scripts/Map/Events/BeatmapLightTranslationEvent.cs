using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public class BeatmapLightTranslationEventData : BeatmapLightEventBoxDataBase
{
    // public float AddedBeat { get => Time; set => Time = value; } // b, actually it shouldn't be Time, but for simply comparison just make it as Time
    public int UsePrevious; // p
    public int EaseType; // e
    public float TranslateValue; // t

    public BeatmapLightTranslationEventData(JSONNode node) => LoadFromJson(node);

    public BeatmapLightTranslationEventData(float b, int p, int e, float t)
    {
        AddedBeat = b;
        UsePrevious = p;
        EaseType = e;
        TranslateValue = t;
    }

    public BeatmapLightTranslationEventData(): base()
    {
        EaseType = -1;
    }

    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        UsePrevious = RetrieveRequiredNode(node, "p").AsInt;
        EaseType = RetrieveRequiredNode(node, "e").AsInt;
        TranslateValue = RetrieveRequiredNode(node, "t").AsFloat;
    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["p"] = UsePrevious;
        node["e"] = EaseType;
        node["t"] = TranslateValue;
    }
}

public class BeatmapLightTranslationEventBox : BeatmapLightEventBoxBase<BeatmapLightTranslationEventData>
{
    // public BeatmapLightEventFilter Filter; // f
    // public float Distribution; // w
    // public int DistributionType; // d
    public float TranslationDistribution; // s
    public int TranslationDistributionType; // t
    public int Axis; // a
    public int Flip; // r
    public int TranslationAffectFirst; // b
    // public int DistributionEaseType; // i
    // public List<BeatmapLightTranslationEventData> EventDatas = new List<BeatmapLightTranslationEventData>(); // l

    public BeatmapLightTranslationEventBox(JSONNode node) => LoadFromJson(node);

    public BeatmapLightTranslationEventBox(): base()
    {
        TranslationDistributionType = 1;
    }

    public override string EventDataKey => "l";

    protected override void LoadFromJsonImpl(ref JSONNode node)
    {
        TranslationDistribution = RetrieveRequiredNode(node, "s").AsFloat;
        TranslationDistributionType = RetrieveRequiredNode(node, "t").AsInt;
        Axis = RetrieveRequiredNode(node, "a").AsInt;
        Flip = RetrieveRequiredNode(node, "r").AsInt;
        TranslationAffectFirst = RetrieveRequiredNode(node, "b").AsInt;
    }
    protected override void ConvertToJsonImpl(ref JSONNode node)
    {
        node["s"] = TranslationDistribution;
        node["t"] = TranslationDistributionType;
        node["a"] = Axis;
        node["r"] = Flip;
        node["b"] = TranslationAffectFirst;
    }
}


public class BeatmapLightTranslationEvent: BeatmapLightEventBase<BeatmapLightTranslationEventBox, BeatmapLightTranslationEventData>
{
    // public float Beat { get => Time; set => Time = value; } // b
    // public int Group; // g
    // public List<BeatmapLightTranslationEventBox> EventBoxes = new List<BeatmapLightTranslationEventBox>(); // e

    public BeatmapLightTranslationEvent(JSONNode node): base(node)
    {
    }

    public BeatmapLightTranslationEvent(float b, int g, BeatmapLightTranslationEventBox e)
    {
        Beat = b;
        Group = g;
        EventBoxes.Add(e);
    }

    public BeatmapLightTranslationEvent(): base()
    {
    }

    public override ObjectType BeatmapType { get; set; } = ObjectType.LightTranslationEvent;
}
