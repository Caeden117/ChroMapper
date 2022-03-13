using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapObstacleV3 : BeatmapObstacle
{
    public int ValueUnknownBarrier = -1;
    //[FormerlySerializedAs("b")] private float b;
    //[FormerlySerializedAs("x")] private int x;
    [FormerlySerializedAs("y")] private int y;
    //[FormerlySerializedAs("d")] private float d;
    //[FormerlySerializedAs("w")] private int w;
    [FormerlySerializedAs("h")] private int h = 5;

    public float B { get => Time; set => Time = value; }
    public int X { get => LineIndex; set => LineIndex = value; }
    public int Y { get => y; set => y = value; }
    public float D { get => Duration; set => Duration = value; }
    public int W { get => Width; set => Width = value; }
    public int H { get {
            return Type switch
            {
                ValueFullBarrier => 5,
                ValueHighBarrier => 3,
                _ => h,
            };
        }
        set {
            if (value == 3) Type = ValueHighBarrier;
            else if (value == 5) Type = ValueFullBarrier;
            else
            {
                h = value;
                Type = ValueUnknownBarrier;
            }
        } }

    public BeatmapObstacleV3(JSONNode node)
    {
        B = RetrieveRequiredNode(node, "b").AsFloat;
        X = RetrieveRequiredNode(node, "x").AsInt;
        Y = RetrieveRequiredNode(node, "y").AsInt;
        D = RetrieveRequiredNode(node, "d").AsFloat;
        W = RetrieveRequiredNode(node, "w").AsInt;
        H = RetrieveRequiredNode(node, "h").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapObstacleV3(BeatmapObstacle o):
        base(o.Time, o.LineIndex, o.Type, o.Duration, o.Width, o.CustomData)
    {
        Y = 5 - H;
    }
    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(B, DecimalPrecision);
        node["x"] = X;
        node["y"] = Y;
        node["d"] = Math.Round(D, DecimalPrecision); //Get rid of float precision errors
        node["w"] = W;
        node["h"] = H;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapObstacleV3 obs)
        {
            B = obs.B;
            X = obs.X;
            Y = obs.Y;
            D = obs.D;
            W = obs.W;
            H = obs.H;
        }
    }
}
