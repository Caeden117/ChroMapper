using System;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class BeatmapNote : BeatmapObject, IBeatmapObjectBounds
{
    public const int LineIndexFarLeft = 0;
    public const int LineIndexMidLeft = 1;
    public const int LineIndexMidRight = 2;
    public const int LineIndexFarRight = 3;

    public const int LineLayerBottom = 0;
    public const int LineLayerMid = 1;
    public const int LineLayerTop = 2;

    public const int NoteTypeA = 0;

    public const int NoteTypeB = 1;

    //public const int NOTE_TYPE_GHOST = 2;
    public const int NoteTypeBomb = 3;

    public const int NoteCutDirectionUp = 0;
    public const int NoteCutDirectionDown = 1;
    public const int NoteCutDirectionLeft = 2;
    public const int NoteCutDirectionRight = 3;
    public const int NoteCutDirectionUpLeft = 4;
    public const int NoteCutDirectionUpRight = 5;
    public const int NoteCutDirectionDownLeft = 6;
    public const int NoteCutDirectionDownRight = 7;

    public const int NoteCutDirectionAny = 8;
    public const int NoteCutDirectionNone = 9;
    [FormerlySerializedAs("_lineIndex")] public int LineIndex;
    [FormerlySerializedAs("_lineLayer")] public int LineLayer;
    [FormerlySerializedAs("_type")] public int Type;
    [FormerlySerializedAs("_cutDirection")] public int CutDirection;

    [FormerlySerializedAs("id")] public uint ID;

    /*
     * MapNote Logic
     */

    public BeatmapNote() { }

    public BeatmapNote(JSONNode node)
    {
        Time = RetrieveRequiredNode(node, "_time").AsFloat;
        LineIndex = RetrieveRequiredNode(node, "_lineIndex").AsInt;
        LineLayer = RetrieveRequiredNode(node, "_lineLayer").AsInt;
        Type = RetrieveRequiredNode(node, "_type").AsInt;
        CutDirection = RetrieveRequiredNode(node, "_cutDirection").AsInt;
        CustomData = node["_customData"];
    }

    public BeatmapNote(float time, int lineIndex, int lineLayer, int type, int cutDirection, JSONNode customData = null)
    {
        Time = time;
        LineIndex = lineIndex;
        LineLayer = lineLayer;
        Type = type;
        CutDirection = cutDirection;
        CustomData = customData;
    }

    public bool IsMainDirection => CutDirection == NoteCutDirectionUp || CutDirection == NoteCutDirectionDown ||
                                   CutDirection == NoteCutDirectionLeft ||
                                   CutDirection == NoteCutDirectionRight;

    public override ObjectType BeatmapType { get; set; } = ObjectType.Note;

    public Vector2 GetCenter() => GetPosition() + new Vector2(0f, 0.5f);

    public override JSONNode ConvertToJson()
    {
        JSONNode node = new JSONObject();
        node["_time"] = Math.Round(Time, DecimalPrecision);
        node["_lineIndex"] = LineIndex;
        node["_lineLayer"] = LineLayer;
        node["_type"] = Type;
        node["_cutDirection"] = CutDirection;
        if (CustomData != null) node["_customData"] = CustomData;
        return node;
    }

    public Vector2 GetPosition()
    {
        if (CustomData?.HasKey("_position") ?? false)
            return CustomData["_position"].ReadVector2() + new Vector2(0.5f, 0);
        return DerivePositionFromIndex(LineIndex, LineLayer);
    }

    public static Vector2 DerivePositionFromIndex(int LineIndex, int LineLayer)
    {
        var position = LineIndex - 1.5f;
        float layer = LineLayer;

        if (LineIndex >= 1000)
            position = (LineIndex / 1000f) - 2.5f;
        else if (LineIndex <= -1000) position = (LineIndex / 1000f) - 0.5f;

        if (LineLayer >= 1000 || LineLayer <= -1000) layer = (LineLayer / 1000f) - 1f;

        return new Vector2(position, layer);
    }

    public Vector3 GetScale()
    {
        if (CustomData?.HasKey("_scale") ?? false) return CustomData["_scale"].ReadVector3();
        return Vector3.one;
    }

    protected override bool IsConflictingWithObjectAtSameTime(BeatmapObject other, bool deletion)
    {
        if (other is BeatmapNote note)
            // Only down to 1/4 spacing
            return Vector2.Distance(note.GetPosition(), GetPosition()) < 0.1;
        return false;
    }

    public override void Apply(BeatmapObject originalData)
    {
        base.Apply(originalData);

        if (originalData is BeatmapNote note)
        {
            Type = note.Type;
            CutDirection = note.CutDirection;
            LineIndex = note.LineIndex;
            LineLayer = note.LineLayer;
        }
    }
}
