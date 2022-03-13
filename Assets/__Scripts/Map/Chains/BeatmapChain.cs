using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapChain : BeatmapObject
{
    public const int MinChainCount = 3;
    public const int MaxChainCount = 999;
    //[FormerlySerializedAs("b")] private float b;
    [FormerlySerializedAs("c")] public int C;
    [FormerlySerializedAs("x")] public int X;
    [FormerlySerializedAs("y")] public int Y;
    [FormerlySerializedAs("d")] public int D;
    [FormerlySerializedAs("tb")] public float Tb;
    [FormerlySerializedAs("tx")] public int Tx;
    [FormerlySerializedAs("ty")] public int Ty;
    [FormerlySerializedAs("sc")] public int Sc;
    [FormerlySerializedAs("s")] public float S;

    public override ObjectType BeatmapType { get; set; } = ObjectType.Chain;
    public float B { get => Time; set => Time = value; }

    public BeatmapChain() { }

    public BeatmapChain(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        C = RetrieveRequiredNode(node, "c").AsInt;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        D = RetrieveRequiredNode(node, "d").AsInt;
        Tb = RetrieveRequiredNode(node, "tb").AsFloat;
        Tx = RetrieveRequiredNode(node, "tx").AsInt;
        Ty = RetrieveRequiredNode(node, "ty").AsInt;
        Sc = RetrieveRequiredNode(node, "sc").AsInt;
        S = RetrieveRequiredNode(node, "s").AsFloat;
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
        node["sc"] = Sc;
        node["s"] = S;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false)
    {
        return false;
    }
}
