using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

public class BeatmapObjectModifiedAction : BeatmapAction, IMergeableAction
{
    private bool addToSelection;

    private BaseObject editedData;

    private BaseObject editedObject;
    private BaseObject originalData;
    private BaseObject originalObject;

    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedAction() : base() { }

    public BeatmapObjectModifiedAction(BaseObject edited, BaseObject originalObject, BaseObject originalData,
        string comment = "No comment.", bool keepSelection = false, ActionMergeType mergeType = ActionMergeType.None) : base(new[] { edited, originalObject }, comment)
    {
        editedObject = edited;
        editedData = BeatmapFactory.Clone(edited);

        this.originalData = originalData;
        this.originalObject = originalObject;
        addToSelection = keepSelection;
        MergeType = mergeType;
    }

    public IMergeableAction TryMerge(IMergeableAction previous)
    {
        return CanMerge(previous) ? DoMerge(previous) : null;
    }

    public bool CanMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectModifiedAction previousAction) return false;
        return MergeType != ActionMergeType.None && previous.MergeType == MergeType && originalObject == previousAction.editedObject && editedData.CompareTo(previousAction.originalData) != 0;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectModifiedAction previousAction) return null;
        var merged = new BeatmapObjectModifiedAction(editedObject, previousAction.originalObject, previousAction.originalData, Comment, addToSelection, MergeType);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == editedObject ? originalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.CompareTo(originalData) != 0)
        {
            DeleteObject(editedObject, false);
            SelectionController.Deselect(editedObject, false);

            if (originalData != originalObject) originalObject.Apply(originalData);
            SpawnObject(originalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects
            if (originalData != originalObject) originalObject.Apply(originalData);
            if (originalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(originalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked)
        {
            SelectionController.Select(originalObject, addToSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.CompareTo(originalData) != 0)
        {
            DeleteObject(originalObject, false);
            SelectionController.Deselect(originalObject, false);

            editedObject.Apply(editedData);
            SpawnObject(editedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects 
            editedObject.Apply(editedData);
            if (originalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(originalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
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
