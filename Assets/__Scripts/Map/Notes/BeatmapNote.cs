using SimpleJSON;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BeatmapNote : BeatmapObject {

    public const int LINE_INDEX_FAR_LEFT = 0;
    public const int LINE_INDEX_MID_LEFT = 1;
    public const int LINE_INDEX_MID_RIGHT = 2;
    public const int LINE_INDEX_FAR_RIGHT = 3;

    public const int LINE_LAYER_BOTTOM = 0;
    public const int LINE_LAYER_MID = 1;
    public const int LINE_LAYER_TOP = 2;

    public const int NOTE_TYPE_A = 0;
    public const int NOTE_TYPE_B = 1;
    //public const int NOTE_TYPE_GHOST = 2;
    public const int NOTE_TYPE_BOMB = 3;

    public const int NOTE_CUT_DIRECTION_UP = 0;
    public const int NOTE_CUT_DIRECTION_DOWN = 1;
    public const int NOTE_CUT_DIRECTION_LEFT = 2;
    public const int NOTE_CUT_DIRECTION_RIGHT = 3;
    public const int NOTE_CUT_DIRECTION_UP_LEFT = 4;
    public const int NOTE_CUT_DIRECTION_UP_RIGHT = 5;
    public const int NOTE_CUT_DIRECTION_DOWN_LEFT = 6;
    public const int NOTE_CUT_DIRECTION_DOWN_RIGHT = 7;

    public const int NOTE_CUT_DIRECTION_ANY = 8;
    public const int NOTE_CUT_DIRECTION_NONE = 9;

    /*
     * MapNote Logic
     */
     
    public BeatmapNote() { }

    public BeatmapNote(JSONNode node) {
        _time = node["_time"].AsFloat;
        _lineIndex = node["_lineIndex"].AsInt;
        _lineLayer = node["_lineLayer"].AsInt;
        _type = node["_type"].AsInt;
        _cutDirection = node["_cutDirection"].AsInt;
        _customData = node["_customData"];
        beatmapType = (_type == NOTE_TYPE_BOMB) ? Type.BOMB : Type.NOTE;
    }

    public BeatmapNote(float time, int lineIndex, int lineLayer, int type, int cutDirection, JSONNode customData = null) {
        _time = time;
        _lineIndex = lineIndex;
        _lineLayer = lineLayer;
        _type = type;
        _cutDirection = cutDirection;
        _customData = customData;
        beatmapType = (type == NOTE_TYPE_BOMB) ? Type.BOMB : Type.NOTE;
    }

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = _time;
        node["_lineIndex"] = _lineIndex;
        node["_lineLayer"] = _lineLayer;
        node["_type"] = _type;
        node["_cutDirection"] = _cutDirection;
        node["_customData"] = _customData;
        return node;
    }

    public override float _time { get; set; }
    public override Type beatmapType { get; set; }
    public override JSONNode _customData { get; set; }
    public int _lineIndex = 0;
    public int _lineLayer = 0;
    public int _type = 0;
    public int _cutDirection = 0;

    public uint id = 0;

}
