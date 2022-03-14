using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapSlider : BeatmapObject
{
    //[FormerlySerializedAs("b")] private float b;
    [FormerlySerializedAs("c")] public int C;
    [FormerlySerializedAs("x")] public int X;
    [FormerlySerializedAs("y")] public int Y;
    [FormerlySerializedAs("d")] public int D;
    [FormerlySerializedAs("tb")] public float Tb;
    [FormerlySerializedAs("tx")] public int Tx;
    [FormerlySerializedAs("ty")] public int Ty;
    [FormerlySerializedAs("mu")] public float Mu;
    [FormerlySerializedAs("tmu")] public float Tmu;
    [FormerlySerializedAs("tc")] public int Tc;
    [FormerlySerializedAs("m")] public int M; // haven't figured out its meaning
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
