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
    public const float MinChainSquish = 0.1f;
    public const float MaxChainSquish = 999;
    public static Vector3 ChainScale = new Vector3(1.5f, 0.8f, 1.5f);
    // private float b;
    public int Color;
    public int X;
    public int Y;
    public int Direction;
    public float TailTime { get => Time + DeltaTime; set => DeltaTime = value - Time; } // it always needs to be set after Time;
    public float DeltaTime;
    public int TailX;
    public int TailY;
    public int SliceCount;
    public float SquishAmount = 1.0f;

    public override ObjectType BeatmapType { get; set; } = ObjectType.Chain;
    //public float Time { get => base.Time; set => base.Time = value;  }

    public BeatmapChain() { }

    public BeatmapChain(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        Color = RetrieveRequiredNode(node, "c").AsInt;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        Direction = RetrieveRequiredNode(node, "d").AsInt;
        TailTime = RetrieveRequiredNode(node, "tb").AsFloat;
        TailX = RetrieveRequiredNode(node, "tx").AsInt;
        TailY = RetrieveRequiredNode(node, "ty").AsInt;
        SliceCount = RetrieveRequiredNode(node, "sc").AsInt;
        SquishAmount = RetrieveRequiredNode(node, "s").AsFloat;
        CustomData = node[BeatmapObjectV3CustomDataKey];
    }

    public BeatmapChain(BeatmapChain other)
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
        node["sc"] = SliceCount;
        node["s"] = SquishAmount;
        if (CustomData != null) node[BeatmapObjectV3CustomDataKey] = CustomData;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion = false)
    {
        return false;
    }

    public void CopyHelper(BeatmapChain other)
    {
        Time = other.Time;
        Color = other.Color;
        X = other.X;
        Y = other.Y;
        Direction = other.Direction;
        TailTime = other.TailTime;
        TailX = other.TailX;
        TailY = other.TailY;
        SliceCount = other.SliceCount;
        SquishAmount = other.SquishAmount;
        CustomData = other.CustomData;
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
