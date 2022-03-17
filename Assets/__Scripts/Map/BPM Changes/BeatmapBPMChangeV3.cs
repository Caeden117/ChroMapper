using System;
using System.Collections;
using System.Collections.Generic;
using SimpleJSON;
using UnityEngine;

/// <summary>
/// <see cref="BeatmapBPMChangeV3"/> currently disabled
/// </summary>
public class BeatmapBPMChangeV3 : BeatmapBPMChange
{
    public BeatmapBPMChangeV3(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "b").AsFloat;
        Bpm = RetrieveRequiredNode(node, "m").AsFloat;
        BeatsPerBar = 4;
        MetronomeOffset = 4;
    }

    public BeatmapBPMChangeV3(BeatmapBPMChange other)
    {
        Apply(other);
    }

    public override JSONNode ConvertToJson()
    {
        if (!Settings.Instance.Load_MapV3) return base.ConvertToJson();
        JSONNode node = new JSONObject();
        node["b"] = Math.Round(Time, DecimalPrecision);
        node["m"] = Bpm;
        if (CustomData != null) node[BeatmapObjectV3CustomDataKey] = CustomData;
        return node;
    }
}
