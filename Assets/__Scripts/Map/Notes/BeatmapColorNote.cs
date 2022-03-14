
using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapColorNote : BeatmapNote
{
    // private float b;
    // private int x;
    // private int y;
    private int a = 0; // usually 0, only could be 45 at omni-direct note
    // private int c;
    // private int d;

    public float B { get => Time; set => Time = value; }
    public int X { get => LineIndex; set => LineIndex = value; }
    public int Y { get => LineLayer; set => LineLayer = value; }
    public int A { get => a; set => a = value; }
    public int C { get => Type; set => Type = value; }
    public int D { get => CutDirection; set => CutDirection = value; }

    public BeatmapColorNote() { }

    public BeatmapColorNote(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        A = RetrieveRequiredNode(node, "a").AsInt;
        C = RetrieveRequiredNode(node, "c").AsInt;
        D = RetrieveRequiredNode(node, "d").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapColorNote(BeatmapNote note):
        base(note.Time, note.LineIndex, note.LineLayer, note.Type, note.CutDirection, note.CustomData)
    {

    }

    public BeatmapColorNote(BeatmapChain chain)
    {
        B = chain.B;
        X = chain.X;
        Y = chain.Y;
        A = 0;
        C = chain.C;
        D = chain.D;
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["x"] = X;
        node["y"] = Y;
        node["a"] = A;
        node["c"] = C;
        node["d"] = D;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapColorNote note)
        {
            B = note.B;
            X = note.X;
            Y = note.Y;
            A = note.A;
            C = note.C;
            D = note.D;
        }
    }
}
