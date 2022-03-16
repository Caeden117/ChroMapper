using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapSlider : BeatmapObject
{
    // private float b;
    public int Color;
    public int X;
    public int Y;
    public int Direction;
    public float TailTime { get => Time + DeltaTime; set => DeltaTime = value - Time; }
    public float DeltaTime;
    public int TailX;
    public int TailY;
    public float HeadControlPointLengthMultiplier;
    public float TailControlPointLengthMultiplier;
    public int TailCutDirection;
    public int SliderMidAnchorMode; // haven't figured out its usage
    public override ObjectType BeatmapType { get; set; } = ObjectType.Slider;
    //public float Time { get => base.Time; set => base.Time = value; }

    public BeatmapSlider() { }

    public BeatmapSlider(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        Color = RetrieveRequiredNode(node, "c").AsInt;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        Direction = RetrieveRequiredNode(node, "d").AsInt;
        TailTime = RetrieveRequiredNode(node, "tb").AsFloat;
        TailX = RetrieveRequiredNode(node, "tx").AsInt;
        TailY = RetrieveRequiredNode(node, "ty").AsInt;
        HeadControlPointLengthMultiplier = RetrieveRequiredNode(node, "mu").AsFloat;
        TailControlPointLengthMultiplier = RetrieveRequiredNode(node, "tmu").AsFloat;
        TailCutDirection = RetrieveRequiredNode(node, "tc").AsInt;
        SliderMidAnchorMode = RetrieveRequiredNode(node, "m").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapSlider(BeatmapSlider other)
    {
        CopyHelper(other);
    }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["c"] = Color;
        node["x"] = X;
        node["y"] = Y;
        node["d"] = Direction;
        node["tb"] = TailTime;
        node["tx"] = TailX;
        node["ty"] = TailY;
        node["mu"] = HeadControlPointLengthMultiplier;
        node["tmu"] = TailControlPointLengthMultiplier;
        node["tc"] = TailCutDirection;
        node["m"] = SliderMidAnchorMode;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }
    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false)
    {
        return false;
    }

    public Vector2 GetPosition()
    {
        var start = BeatmapNote.DerivePositionFromIndex(X, Y);
        return start;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);
        if (originalData is BeatmapSlider slider)
        {
            CopyHelper(slider);
        }
    }

    public void CopyHelper(BeatmapSlider other)
    {
        Time = other.Time;
        Color = other.Color;
        X = other.X;
        Y = other.Y;
        Direction = other.Direction;
        TailTime = other.TailTime;
        TailX = other.TailX;
        TailY = other.TailY;
        HeadControlPointLengthMultiplier = other.HeadControlPointLengthMultiplier;
        TailControlPointLengthMultiplier = other.TailControlPointLengthMultiplier;
        TailCutDirection = other.TailCutDirection;
        SliderMidAnchorMode = other.SliderMidAnchorMode;
        CustomData = other.CustomData;
    }
}
