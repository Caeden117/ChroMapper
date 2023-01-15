using System;
using System.Linq;
using LiteNetLib.Utils;

public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private BeatmapObject conflictingObject;

    public BeatmapObjectModifiedWithConflictingAction() : base() { }

    public BeatmapObjectModifiedWithConflictingAction(BeatmapObject edited, BeatmapObject originalObject,
        BeatmapObject originalData, BeatmapObject conflicting, string comment = "No comment.") : base(edited,
        originalObject, originalData, comment) => conflictingObject = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);
        if (conflictingObject != null)
        {
            SpawnObject(conflictingObject);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);
        if (conflictingObject != null)
        {
            DeleteObject(conflictingObject);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {    
        base.Serialize(writer);
        writer.PutBeatmapObject(conflictingObject);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        conflictingObject = reader.GetBeatmapObject();
        Data = Data.Append(conflictingObject);
    }
}
