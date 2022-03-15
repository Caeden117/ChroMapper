using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapBombNote : BeatmapNote
{
    //private float b;
    //private int x;
    //private int y;

    public float B { get => Time; set => Time = value; }
    public int X { get => LineIndex; set => LineIndex = value; }
    public int Y { get => LineLayer; set => LineLayer = value; }


    public BeatmapBombNote() { Type = NoteTypeBomb; }

    public BeatmapBombNote(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        CustomData = node["_customData"];
        Type = NoteTypeBomb;
    }

    public BeatmapBombNote(BeatmapNote note) :
        base(note.Time, note.LineIndex, note.LineLayer, note.Type, note.CutDirection, note.CustomData)
    {
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["x"] = X;
        node["y"] = Y;
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
        }
    }
}
