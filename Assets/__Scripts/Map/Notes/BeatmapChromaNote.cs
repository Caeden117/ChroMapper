public class BeatmapChromaNote : BeatmapNote
{
    public const int Monochrome = NoteCutDirectionUp;
    public const int Bidirectional = NoteCutDirectionLeft;
    public const int Duochrome = NoteCutDirectionRight;
    public const int HotGarbage = NoteCutDirectionDownRight;
    public const int Alternate = NoteCutDirectionDown;
    public const int Deflect = NoteCutDirectionUpRight;

    public int BombRotation = 0;

    public BeatmapNote OriginalNote;

    public BeatmapChromaNote(BeatmapNote note)
    {
        OriginalNote = note;
        Type = note.Type;
        ID = note.ID;
        CutDirection = note.CutDirection;
        LineIndex = note.LineIndex;
        LineLayer = note.LineLayer;
        Time = note.Time;
        Type = note.Type;

        //Set custom JSON data here.
    }

    public override ObjectType BeatmapType
    {
        get => ObjectType.CustomNote;
        set => base.BeatmapType = value;
    }

    public BeatmapNote ConvertToNote() =>
        new BeatmapNote(Time, LineIndex, LineLayer, Type, CutDirection, CustomData);

    /*public new JSONNode ConvertToJSON() //Uncomment this when Custom JSON Data is ready.
    {
        return ConvertToNote().ConvertToJSON();
    }*/
}
