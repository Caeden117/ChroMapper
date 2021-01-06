using SimpleJSON;
using System;
using UnityEngine;

[Serializable]
public class BeatmapNote : BeatmapObject, IBeatmapObjectBounds
{

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
        _time = RetrieveRequiredNode(node, "_time").AsFloat;
        _lineIndex = RetrieveRequiredNode(node, "_lineIndex").AsInt;
        _lineLayer = RetrieveRequiredNode(node, "_lineLayer").AsInt;
        _type = RetrieveRequiredNode(node, "_type").AsInt;
        _cutDirection = RetrieveRequiredNode(node, "_cutDirection").AsInt;
        _customData = node["_customData"];
    }

    public BeatmapNote(float time, int lineIndex, int lineLayer, int type, int cutDirection, JSONNode customData = null) {
        _time = time;
        _lineIndex = lineIndex;
        _lineLayer = lineLayer;
        _type = type;
        _cutDirection = cutDirection;
        _customData = customData;
    }

    public override JSONNode ConvertToJSON() {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(_time, decimalPrecision);
        node["_lineIndex"] = _lineIndex;
        node["_lineLayer"] = _lineLayer;
        node["_type"] = _type;
        node["_cutDirection"] = _cutDirection;
        if (_customData != null) node["_customData"] = _customData;
        return node;
    }

    public Vector2 GetPosition()
    {
        if (_customData?.HasKey("_position") ?? false)
        {
            return _customData["_position"].ReadVector2() + new Vector2(0.5f, 0);
        }

        float position = _lineIndex - 1.5f;
        float layer = _lineLayer;

        if (_lineIndex >= 1000)
        {
            position = (_lineIndex / 1000f) - 2.5f;
        }
        else if (_lineIndex <= -1000)
        {
            position = (_lineIndex / 1000f) - 0.5f;
        }

        if (_lineLayer >= 1000 || _lineLayer <= -1000)
        {
            layer = (_lineLayer / 1000f) - 1f;
        }

        return new Vector2(position, layer);
    }
    public Vector3 GetScale()
    {
        if (_customData?.HasKey("_scale") ?? false)
        {
            return _customData["_scale"].ReadVector3();
        }
        return Vector3.one;
    }

    public Vector2 GetCenter()
    {
        return GetPosition() + new Vector2(0f, 0.5f);
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (other is BeatmapNote note)
        {
            // Only down to 1/4 spacing
            return Vector2.Distance(note.GetPosition(), GetPosition()) < 0.1;
        }
        return false;
    }

    public bool IsMainDirection => _cutDirection == NOTE_CUT_DIRECTION_UP || _cutDirection == NOTE_CUT_DIRECTION_DOWN ||
        _cutDirection == NOTE_CUT_DIRECTION_LEFT || _cutDirection == NOTE_CUT_DIRECTION_RIGHT;

    public override Type beatmapType { get; set; } = Type.NOTE;
    public int _lineIndex = 0;
    public int _lineLayer = 0;
    public int _type = 0;
    public int _cutDirection = 0;

    public uint id = 0;

}
