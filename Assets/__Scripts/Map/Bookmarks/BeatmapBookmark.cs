using SimpleJSON;
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;


public class BeatmapBookmark : BeatmapObject
{
    public BeatmapBookmark(JSONNode node)
    {
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _name = RetrieveRequiredNode(node, "_name");
        if (node.HasKey("_color")) _color = ColorFromJSONArray(RetrieveRequiredNode(node, "_color").AsArray);
        else _color = GetRandomColor();
    }

    private Color GetRandomColor()
    {
        Color color = UnityEngine.Random.ColorHSV(0, 1, 0.75f, 0.75f, 1, 1);
        return color;
    }

    private Color ColorFromJSONArray(JSONArray color)
    {
        return new Color(color[0], color[1], color[2]);
    }   

    private JSONArray ColorToJSONArray(Color color)
    {
        JSONArray colorArray = new JSONArray();
        colorArray[0] = color.r;
        colorArray[1] = color.g;
        colorArray[2] = color.b;
        return colorArray;
    }

    public BeatmapBookmark(float time, string name)
    {
        _time = time;
        _name = name;
        _color = GetRandomColor();
        //_color = color;
    }

    public void NewColor()
    {
        PersistentUI.Instance.ShowColorInputBox("Mapper", "bookmark.dialog", HandleNewBookmarkColor, "bookmark.dialog.default");
    }

    private void HandleNewBookmarkColor(object res)
    {
        if (res == null || res.GetType() != typeof(Color)) return;
        _color = (Color)res;

    }

    public override JSONNode ConvertToJSON()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_name"] = _name;
        node["_color"] = ColorToJSONArray(_color);
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
}
