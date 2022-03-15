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
    public int C;
    public int X;
    public int Y;
    public int D;
    public float Tb { get => B + Db; set => Db = value - B; }
    public float Db;
    public int Tx;
    public int Ty;
    public float Mu;
    public float Tmu;
    public int Tc;
    public int M; // haven't figured out its meaning
    public override ObjectType BeatmapType { get; set; } = ObjectType.Slider;
    public float B { get => Time; set => Time = value; }

    public BeatmapSlider() { }

    public BeatmapSlider(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        C = RetrieveRequiredNode(node, "c").AsInt;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        D = RetrieveRequiredNode(node, "d").AsInt;
        Tb = RetrieveRequiredNode(node, "tb").AsFloat;
        Tx = RetrieveRequiredNode(node, "tx").AsInt;
        Ty = RetrieveRequiredNode(node, "ty").AsInt;
        Mu = RetrieveRequiredNode(node, "mu").AsFloat;
        Tmu = RetrieveRequiredNode(node, "tmu").AsFloat;
        Tc = RetrieveRequiredNode(node, "tc").AsInt;
        M = RetrieveRequiredNode(node, "m").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapSlider(BeatmapSlider other)
    {
        CopyHelper(other);
    }

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["c"] = C;
        node["x"] = X;
        node["y"] = Y;
        node["d"] = D;
        node["tb"] = Tb;
        node["tx"] = Tx;
        node["ty"] = Ty;
        node["mu"] = Mu;
        node["tmu"] = Tmu;
        node["tc"] = Tc;
        node["m"] = M;
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
        B = other.B;
        C = other.C;
        X = other.X;
        Y = other.Y;
        D = other.D;
        Tb = other.Tb;
        Tx = other.Tx;
        Ty = other.Ty;
        Mu = other.Mu;
        Tmu = other.Tmu;
        Tc = other.Tc;
        M = other.M;
        CustomData = other.CustomData;
    }
}
