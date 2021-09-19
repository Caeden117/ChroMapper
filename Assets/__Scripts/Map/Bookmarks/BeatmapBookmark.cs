﻿using SimpleJSON;
using System;
using UnityEngine;


public class BeatmapBookmark : BeatmapObject
{
    public BeatmapBookmark(JSONNode node)
    {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _name = RetrieveRequiredNode(node, "_name");
        if (node.HasKey("_color")) _color = RetrieveRequiredNode(node, "_color");
        else _color = Color.HSVToRGB((float)rand.NextDouble(), 0.75f, 1);
    }


    public BeatmapBookmark(float time, string name)
    {
        _time = time;
        _name = name;
        _color = Color.HSVToRGB((float)rand.NextDouble(), 0.75f, 1);
    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_name"] = _name;
        node["_color"] = _color;
        return node;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion) => true;
    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapBookmark bm)
        {
            _name = bm._name;
        }
    }

    public string _name = "Invalid Bookmark";
    public Color _color;
    public override Type beatmapType { get; set; } = Type.BPM_CHANGE;

    private System.Random rand = new System.Random();
}
