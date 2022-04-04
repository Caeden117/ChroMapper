using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapObstacleV3 : BeatmapObstacle
{
    public const int ValueUnknownBarrier = -1;
    //private float b;
    //private int x;
    private int lineLayer;
    // private float d;
    // private int w;
    private int height = 5;

    //public float Time { get => base.Time; set => base.Time = value; }
    //public int LineIndex { get => base.LineIndex; set => base.LineIndex = value; }
    public int LineLayer { get => lineLayer; set => lineLayer = value; }
    //public float Duration { get => base.Duration; set => base.Duration = value; }
    //public int Width { get => base.Width; set => base.Width = value; }
    public int Height { get {
            return Type switch
            {
                ValueFullBarrier => 5,
                ValueHighBarrier => 3,
                _ => height,
            };
        }
        set {
            if (value == 3) Type = ValueHighBarrier;
            else if (value == 5) Type = ValueFullBarrier;
            else
            {
                height = value;
                Type = ValueUnknownBarrier;
            }
        } }

    public BeatmapObstacleV3(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        LineIndex = RetrieveRequiredNode(node, "x").AsInt;
        LineLayer = RetrieveRequiredNode(node, "y").AsInt;
        Duration = RetrieveRequiredNode(node, "d").AsFloat;
        Width = RetrieveRequiredNode(node, "w").AsInt;
        Height = RetrieveRequiredNode(node, "h").AsInt;
        CustomData = node[BeatmapObjectV3CustomDataKey];
    }

    public BeatmapObstacleV3(BeatmapObstacle o):
        base(o.Time, o.LineIndex, o.Type, o.Duration, o.Width, o.CustomData)
    {
        LineLayer = 5 - Height;
    }
    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["x"] = LineIndex;
        node["y"] = LineLayer;
        node["d"] = Math.Round(Duration, DecimalPrecision); //Get rid of float precision errors
        node["w"] = Width;
        node["h"] = Height;
        if (CustomData != null) node[BeatmapObjectV3CustomDataKey] = CustomData;
        return node;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapObstacleV3 obs)
        {
            Time = obs.Time;
            LineIndex = obs.LineIndex;
            LineLayer = obs.LineLayer;
            Duration = obs.Duration;
            Width = obs.Width;
            Height = obs.Height;
        }
    }
}
