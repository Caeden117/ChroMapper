public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private BeatmapObject conflictingObject = null;

    public BeatmapObjectModifiedWithConflictingAction(BeatmapObject edited, BeatmapObject originalObject, BeatmapObject originalData, BeatmapObject conflicting, string comment = "No comment.") : base(edited, originalObject, originalData, comment)
    {
        conflictingObject = conflicting;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.beatmapType).SpawnObject(conflictingObject, false, true);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.beatmapType).DeleteObject(conflictingObject, false, true);
        }
    }
}
