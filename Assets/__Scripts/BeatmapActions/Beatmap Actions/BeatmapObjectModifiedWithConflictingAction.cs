using System;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;
using System.Collections.Generic;


public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    public IEnumerable<BaseObject> ConflictingObjects;

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedWithConflictingAction() : base() { }

    public BeatmapObjectModifiedWithConflictingAction(BaseObject edited, BaseObject originalObject,
        BaseObject originalData, IEnumerable<BaseObject> conflicting, string comment = "No comment.") : base(edited,
        originalObject, originalData, comment) => ConflictingObjects = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);

        foreach (var obj in ConflictingObjects)
            SpawnObject(obj);

        RefreshPools(ConflictingObjects);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);

        foreach (var obj in ConflictingObjects)
            DeleteObject(obj, false);

        RefreshPools(ConflictingObjects);
    }

    public override void Serialize(NetDataWriter writer)
    {
        base.Serialize(writer);
        SerializeBeatmapObjectList(writer, ConflictingObjects);
    }

    public override void Deserialize(NetDataReader reader)
    {
        base.Deserialize(reader);
        ConflictingObjects = DeserializeBeatmapObjectList(reader);
    }
}
