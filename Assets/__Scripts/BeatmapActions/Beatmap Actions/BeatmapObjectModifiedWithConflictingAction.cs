public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private BeatmapObject conflictingObject = null;

    public BeatmapObjectModifiedWithConflictingAction(BeatmapObject edited, BeatmapObject original, BeatmapObject conflicting, string comment = "No comment.") : base(edited, original, comment)
    {
        conflictingObject = BeatmapObject.GenerateCopy(conflicting);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.beatmapType).SpawnObject(conflictingObject, false, true);
            conflictingObject = BeatmapObject.GenerateCopy(conflictingObject);
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
