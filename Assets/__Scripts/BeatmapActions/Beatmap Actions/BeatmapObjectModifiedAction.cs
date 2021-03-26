public class BeatmapObjectModifiedAction : BeatmapAction
{
    private readonly BeatmapObject _originalObject;
    private readonly BeatmapObject _originalData;

    private readonly BeatmapObject _editedObject;
    private readonly BeatmapObject _editedData;
    private readonly BeatmapObjectContainerCollection _collection;
    private readonly bool _addToSelection;

    public BeatmapObjectModifiedAction(BeatmapObject edited, BeatmapObject originalObject, BeatmapObject originalData, string comment = "No comment.", bool keepSelection = false) : base(new[] { edited }, comment)
    {
        _collection = BeatmapObjectContainerCollection.GetCollectionForType(originalObject.beatmapType);
        _editedObject = edited;
        _editedData = BeatmapObject.GenerateCopy(edited);

        _originalData = originalData;
        _originalObject = originalObject;
        _addToSelection = keepSelection;
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (_originalObject != _editedObject || _editedData._time.CompareTo(_originalData._time) != 0)
        {
            _collection.DeleteObject(_editedObject, false, false);
            SelectionController.Deselect(_editedObject, false);

            _originalObject.Apply(_originalData);
            _collection.SpawnObject(_originalObject, false, !InCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet
            _originalObject.Apply(_originalData);
            if (!InCollection) RefreshPools(Data);
        }

        SelectionController.Select(_originalObject, _addToSelection, true, !InCollection);
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (_originalObject != _editedObject || _editedData._time.CompareTo(_originalData._time) != 0)
        {
            _collection.DeleteObject(_originalObject, false, false);
            SelectionController.Deselect(_originalObject, false);

            _editedObject.Apply(_editedData);
            _collection.SpawnObject(_editedObject, false, !InCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet 
            _editedObject.Apply(_editedData);
            if (!InCollection) RefreshPools(Data);
        }

        SelectionController.Select(_editedObject, _addToSelection, true, !InCollection);
    }
}
