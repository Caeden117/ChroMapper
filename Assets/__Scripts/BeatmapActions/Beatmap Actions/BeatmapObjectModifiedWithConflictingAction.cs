using System;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;


public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private BaseObject conflictingObject;

    public BeatmapObjectModifiedWithConflictingAction() : base() { }

    public BeatmapObjectModifiedWithConflictingAction(BaseObject edited, BaseObject originalObject,
        BaseObject originalData, BaseObject conflicting, string comment = "No comment.") : base(edited,
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
