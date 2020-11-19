public class BeatmapObjectModifiedAction : BeatmapAction
{
    private BeatmapObject originalData;
    private BeatmapObject editedData;
    private BeatmapObjectContainerCollection collection;

    public BeatmapObjectModifiedAction(BeatmapObject edited, BeatmapObject original, string comment = "No comment.") : base(null, comment)
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType(original.beatmapType);
        editedData = BeatmapObject.GenerateCopy(edited);
        originalData = BeatmapObject.GenerateCopy(original);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(editedData, false);
        SelectionController.Deselect(editedData);

        collection.SpawnObject(originalData, false);
        SelectionController.Select(originalData, false, true, false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(originalData, false);
        SelectionController.Deselect(originalData);

        collection.SpawnObject(editedData, false);
        SelectionController.Select(editedData, false, true, false);
    }
}
