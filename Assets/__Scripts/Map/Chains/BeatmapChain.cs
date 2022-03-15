using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapChain : BeatmapObject
{
    public const int MinChainCount = 2;
    public const int MaxChainCount = 999;
    public const float MinChainSquish = 0.1f;
    public const float MaxChainSquish = 999;
    // private float b;
    public int C;
    public int X;
    public int Y;
    public int D;
    public float Tb { get => B + Db; set => Db = value - B; } // it always needs to be set after B;
    public float Db;
    public int Tx;
    public int Ty;
    public int Sc;
    public float S = 1.0f;

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
        CustomData = node["_customData"];
    }

    public BeatmapChain(BeatmapChain other)
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
        node["sc"] = Sc;
        node["s"] = S;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false)
    {
        return false;
    }

    public void CopyHelper(BeatmapChain other)
    {
        B = other.B;
        C = other.C;
        X = other.X;
        Y = other.Y;
        D = other.D;
        Tb = other.Tb;
        Tx = other.Tx;
        Ty = other.Ty;
        Sc = other.Sc;
        S = other.S;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);
        if (originalData is BeatmapChain chain)
        {
            CopyHelper(chain);
        }
    }
}
