using SimpleJSON;
using System;

public class BeatmapChromaNote : BeatmapNote {

    public const int MONOCHROME = NOTE_CUT_DIRECTION_UP;
    public const int BIDIRECTIONAL = NOTE_CUT_DIRECTION_LEFT;
    public const int DUOCHROME = NOTE_CUT_DIRECTION_RIGHT;
    public const int HOT_GARBAGE = NOTE_CUT_DIRECTION_DOWN_RIGHT;
    public const int ALTERNATE = NOTE_CUT_DIRECTION_DOWN;
    public const int DEFLECT = NOTE_CUT_DIRECTION_UP_RIGHT;

    public int BombRotation = 0;

    public BeatmapNote originalNote;

    public BeatmapChromaNote(BeatmapNote note)
    {
        originalNote = note;
        _type = note._type;
        id = note.id;
        _cutDirection = note._cutDirection;
        _lineIndex = note._lineIndex;
        _lineLayer = note._lineLayer;
        _time = note._time;
        _type = note._type;

        //Set custom JSON data here.

    }

    public BeatmapNote ConvertToNote()
    {
        return new BeatmapNote(_time, _lineIndex, _lineLayer, _type, _cutDirection, _customData);
    }

    /*public new JSONNode ConvertToJSON() //Uncomment this when Custom JSON Data is ready.
    {
        return ConvertToNote().ConvertToJSON();
    }*/

    public new JSONNode[] ConvertToJSON()
    {
        JSONNode note = new JSONObject();
        note["_time"] = Math.Round(_time, 3);
        note["_lineIndex"] = _lineIndex;
        note["_lineLayer"] = _lineLayer;
        note["_type"] = _type;
        note["_cutDirection"] = _cutDirection;
        JSONNode bomb = new JSONObject();
        bomb["_time"] = Math.Round(_time, 3);
        note["_lineIndex"] = _lineIndex;
        note["_lineLayer"] = _lineLayer;
        note["_type"] = NOTE_TYPE_BOMB;
        note["_cutDirection"] = BombRotation;
        return new[] { note, bomb };
    }

    public override Type beatmapType { get => Type.CUSTOM_NOTE;
        set => base.beatmapType = value; }
}
