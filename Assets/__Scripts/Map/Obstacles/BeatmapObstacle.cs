using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeatmapObstacle : BeatmapObject {

    //These are uhh, assumptions...
    public const int VALUE_FULL_BARRIER = 0;
    public const int VALUE_HIGH_BARRIER = 1;

    /*
     * Obstacle Logic
     */

    public BeatmapObstacle(JSONNode node) {
        _time = node["_time"].AsFloat;
        _lineIndex = node["_lineIndex"].AsInt;
        _type = node["_type"].AsInt;
        _duration = node["_duration"].AsFloat;
        _width = node["_width"].AsInt;
        _customData = node["_customData"];
    }

    public BeatmapObstacle(float time, int lineIndex, int type, float duration, int width, JSONNode customData = null) {
        _time = time;
        _lineIndex = lineIndex;
        _type = type;
        _duration = duration;
        _width = width;
        _customData = customData;
    }

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = _time;
        node["_lineIndex"] = _lineIndex;
        node["_type"] = _type;
        node["_duration"] = _duration;
        node["_width"] = _width;
        node["_customData"] = _customData;
        return node;
    }

    public override float _time { get; set; }
    public override Type beatmapType { get; set; } = Type.OBSTACLE;
    public override JSONNode _customData { get; set; }
    public int _lineIndex = 0;
    public int _type = 0;
    public float _duration = 0;
    public int _width = 0;
    public uint id = 0;

}