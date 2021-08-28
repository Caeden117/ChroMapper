public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private readonly BeatmapObject conflictingObject;

    public BeatmapObjectModifiedWithConflictingAction(BeatmapObject edited, BeatmapObject originalObject,
        BeatmapObject originalData, BeatmapObject conflicting, string comment = "No comment.") : base(edited,
        originalObject, originalData, comment) => conflictingObject = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.BeatmapType)
                .SpawnObject(conflictingObject, false);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.BeatmapType)
                .DeleteObject(conflictingObject, false);
        }
    }
}
