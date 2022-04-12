
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
    private int angleOffset = 0;
    // private int c;
    // private int d;

    //public float Time { get => base.Time; set => base.Time = value; }
    // public int LineIndex { get => base.LineIndex; set => base.LineIndex = value; }
    //public int LineLayer { get => base.LineLayer; set => base.LineLayer = value; }
    public int AngleOffset { get; set; }
    public int Color { get => Type; set => Type = value; }
    //public int CutDirection { get => base.CutDirection; set => base.CutDirection = value; }

    public BeatmapColorNote() { }

    public BeatmapColorNote(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        LineIndex = RetrieveRequiredNode(node, "x").AsInt;
        LineLayer = RetrieveRequiredNode(node, "y").AsInt;
        AngleOffset = RetrieveRequiredNode(node, "a").AsInt;
        Color = RetrieveRequiredNode(node, "c").AsInt;
        CutDirection = RetrieveRequiredNode(node, "d").AsInt;
        CustomData = node[BeatmapObjectV3CustomDataKey];
    }

    public BeatmapColorNote(BeatmapNote note) :
        base(note.Time, note.LineIndex, note.LineLayer, note.Type, note.CutDirection, note.CustomData)
    {
        if (note is BeatmapColorNote colorNote) AngleOffset = colorNote.angleOffset;
    }

    public BeatmapColorNote(float time, int lineIndex, int lineLayer, int type, int cutDirection, int angleOffset,
            JSONNode customData = null) :
        base(time, lineIndex, lineLayer, type, cutDirection, customData)
    {
        AngleOffset = angleOffset;
    }

    public BeatmapColorNote(BeatmapChain chain)
    {
        Time = chain.Time;
        LineIndex = chain.X;
        LineLayer = chain.Y;
        Color = chain.Color;
        CutDirection = chain.Direction;
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["x"] = LineIndex;
        node["y"] = LineLayer;
        node["a"] = AngleOffset;
        node["c"] = Color;
        node["d"] = CutDirection;
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
            AngleOffset = note.AngleOffset;
            Color = note.Color;
            CutDirection = note.CutDirection;
        }
    }
}
