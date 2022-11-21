using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

public abstract class BeatmapLightEventBoxDataBase : BeatmapObject
{
    public float AddedBeat { get => Time; set => Time = value; } // b, actually it shouldn't be Time, but for simply comparison just make it as Time

    public BeatmapLightEventBoxDataBase LoadFromJson(JSONNode node)
    {
        AddedBeat = RetrieveRequiredNode(node, "b").AsFloat;
        LoadFromJsonImpl(ref node);
        return this;
    }

    protected abstract void LoadFromJsonImpl(ref JSONNode node);


    public override ObjectType BeatmapType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(AddedBeat, DecimalPrecision);
        ConvertToJsonImpl(ref node);
        return node;
    }

    protected abstract void ConvertToJsonImpl(ref JSONNode node);

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false) => throw new System.NotImplementedException();
}
