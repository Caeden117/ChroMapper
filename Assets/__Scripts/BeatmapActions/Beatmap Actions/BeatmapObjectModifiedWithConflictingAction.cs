using Beatmap.Base;

public class BeatmapObjectModifiedWithConflictingAction : BeatmapObjectModifiedAction
{
    private readonly BaseObject conflictingObject;

    public BeatmapObjectModifiedWithConflictingAction(BaseObject edited, BaseObject originalObject,
        BaseObject originalData, BaseObject conflicting, string comment = "No comment.") : base(edited,
        originalObject, originalData, comment) => conflictingObject = conflicting;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Undo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.ObjectType)
                .SpawnObject(conflictingObject, false);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        base.Redo(param);
        if (conflictingObject != null)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(conflictingObject.ObjectType)
                .DeleteObject(conflictingObject, false);
        }
    }
}
