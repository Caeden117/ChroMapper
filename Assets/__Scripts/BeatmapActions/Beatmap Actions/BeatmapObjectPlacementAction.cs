using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BeatmapObjectPlacementAction : BeatmapAction
{
    internal BeatmapObjectContainerCollection collection;
    internal BeatmapObjectContainer removedConflictObject;

    public BeatmapObjectPlacementAction(BeatmapObjectContainer note, BeatmapObjectContainerCollection collection,
        BeatmapObjectContainer conflictingObject = null) : base(note) {
        this.collection = collection;
        removedConflictObject = conflictingObject;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(container);
        removedConflictObject = collection.SpawnObject(BeatmapObject.GenerateCopy(removedConflictObject.objectData), out _);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        container = collection.SpawnObject(BeatmapObject.GenerateCopy(data), out _);
        collection.DeleteObject(removedConflictObject);
    }
}
