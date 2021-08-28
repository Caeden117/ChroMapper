public class BeatmapObjectModifiedAction : BeatmapAction
{
    private readonly bool addToSelection;
    private readonly BeatmapObjectContainerCollection collection;
    private readonly BeatmapObject editedData;

    private readonly BeatmapObject editedObject;
    private readonly BeatmapObject originalData;
    private readonly BeatmapObject originalObject;

    public BeatmapObjectModifiedAction(BeatmapObject edited, BeatmapObject originalObject, BeatmapObject originalData,
        string comment = "No comment.", bool keepSelection = false) : base(new[] {edited}, comment)
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType(originalObject.BeatmapType);
        editedObject = edited;
        editedData = BeatmapObject.GenerateCopy(edited);

        this.originalData = originalData;
        this.originalObject = originalObject;
        addToSelection = keepSelection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.Time.CompareTo(originalData.Time) != 0)
        {
            collection.DeleteObject(editedObject, false, false);
            SelectionController.Deselect(editedObject, false);

            originalObject.Apply(originalData);
            collection.SpawnObject(originalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet
            originalObject.Apply(originalData);
            if (!inCollection) RefreshPools(Data);
        }

        SelectionController.Select(originalObject, addToSelection, true, !inCollection);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.Time.CompareTo(originalData.Time) != 0)
        {
            collection.DeleteObject(originalObject, false, false);
            SelectionController.Deselect(originalObject, false);

            editedObject.Apply(editedData);
            collection.SpawnObject(editedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet 
            editedObject.Apply(editedData);
            if (!inCollection) RefreshPools(Data);
        }

        SelectionController.Select(editedObject, addToSelection, true, !inCollection);
    }
}
