public class BeatmapObjectModifiedAction : BeatmapAction
{
    private BeatmapObject originalData;
    private BeatmapObject editedData;
    private BeatmapObjectContainerCollection collection;
    private bool addToSelection = false;

    public BeatmapObjectModifiedAction(BeatmapObject edited, BeatmapObject original, string comment = "No comment.", bool rewrapOriginal = true, bool keepSelection = false) : base(null, comment)
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType(original.beatmapType);
        editedData = BeatmapObject.GenerateCopy(edited);

        originalData = rewrapOriginal ? BeatmapObject.GenerateCopy(original) : original;
        addToSelection = keepSelection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(editedData, false);
        SelectionController.Deselect(editedData);

        collection.SpawnObject(originalData, false);
        SelectionController.Select(originalData, addToSelection, true, false);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        collection.DeleteObject(originalData, false);
        SelectionController.Deselect(originalData);

        collection.SpawnObject(editedData, false);
        SelectionController.Select(editedData, addToSelection, true, false);
    }

    public BeatmapObject GetEdited() => editedData;
}
