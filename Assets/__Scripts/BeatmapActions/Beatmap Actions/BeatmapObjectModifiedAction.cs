using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

public class BeatmapObjectModifiedAction : BeatmapAction
{
    private bool addToSelection;

    private BeatmapObjectContainerCollection collection;
    private BaseObject editedData;

    private BaseObject editedObject;
    private BaseObject originalData;
    private BaseObject originalObject;

    public BeatmapObjectModifiedAction() : base() { }

    public BeatmapObjectModifiedAction(BaseObject edited, BaseObject originalObject, BaseObject originalData,
        string comment = "No comment.", bool keepSelection = false) : base(new[] { edited, originalObject }, comment)
    {
        collection = BeatmapObjectContainerCollection.GetCollectionForType(originalObject.ObjectType);
        editedObject = edited;
        editedData = BeatmapFactory.Clone(edited);

        this.originalData = originalData;
        this.originalObject = originalObject;
        addToSelection = keepSelection;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == editedObject ? originalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.JsonTime.CompareTo(originalData.JsonTime) != 0)
        {
            DeleteObject(editedObject, false);
            SelectionController.Deselect(editedObject, false);

            originalObject.Apply(originalData);
            SpawnObject(originalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet
            originalObject.Apply(originalData);
            if (!inCollection) RefreshPools(Data);
        }

        if (originalObject is BaseBpmEvent)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(originalObject.JsonTime);
        }

        if (!Networked)
        {
            SelectionController.Select(originalObject, addToSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.JsonTime.CompareTo(originalData.JsonTime) != 0)
        {
            DeleteObject(originalObject, false);
            SelectionController.Deselect(originalObject, false);

            editedObject.Apply(editedData);
            SpawnObject(editedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the SortedSet 
            editedObject.Apply(editedData);
            if (!inCollection) RefreshPools(Data);
        }

        if (originalObject is BaseBpmEvent)
        {
            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(originalObject.JsonTime);
        }

        if (!Networked)
        {
            SelectionController.Select(editedObject, addToSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(editedData);
        writer.PutBeatmapObject(originalData);
    }

    public override void Deserialize(NetDataReader reader)
    {
        editedData = reader.GetBeatmapObject();
        editedObject = BeatmapFactory.Clone(editedData);
        originalData = reader.GetBeatmapObject();
        originalObject = BeatmapFactory.Clone(originalData);

        Data = new[] { editedObject, originalObject };
    }
}
