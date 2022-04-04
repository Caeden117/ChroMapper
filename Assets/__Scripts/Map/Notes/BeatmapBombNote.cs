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

    //public float Time { get => base.Time; set => base.Time = value; }
    //public int LineIndex { get => base.LineIndex; set => base.LineIndex = value; }
    //public int LineLayer { get => base.LineLayer; set => base.LineLayer = value; }


    public BeatmapBombNote() { Type = NoteTypeBomb; }

    public BeatmapBombNote(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        LineIndex = RetrieveRequiredNode(node, "x").AsInt;
        LineLayer = RetrieveRequiredNode(node, "y").AsInt;
        CustomData = node[BeatmapObjectV3CustomDataKey];
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
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["x"] = LineIndex;
        node["y"] = LineLayer;
        if (CustomData != null) node[BeatmapObjectV3CustomDataKey] = CustomData;
        return node;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);
        if (originalData is BeatmapColorNote note)
        {
            Time = note.Time;
            LineIndex = note.LineIndex;
            LineLayer = note.LineLayer;
        }
    }
}
