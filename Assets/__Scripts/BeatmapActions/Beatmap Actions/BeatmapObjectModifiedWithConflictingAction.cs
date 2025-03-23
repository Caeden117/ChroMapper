using System;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;
using System.Collections.Generic;


public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private IEnumerable<BaseObject> conflictingObjects;

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedWithConflictingAction() : base() { }

    public BeatmapObjectModifiedWithConflictingAction(BaseObject edited, BaseObject originalObject,
        BaseObject originalData, IEnumerable<BaseObject> conflicting, string comment = "No comment.") : base(edited,
        originalObject, originalData, comment) => conflictingObjects = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);

        foreach (var obj in conflictingObjects)
            SpawnObject(obj);

        RefreshPools(conflictingObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);

        foreach (var obj in conflictingObjects)
            DeleteObject(obj, false);

        RefreshPools(conflictingObjects);
    }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        SerializeBeatmapObjectList(writer, conflictingObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        conflictingObjects = DeserializeBeatmapObjectList(reader);
    }
}
