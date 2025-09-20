using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

public class BeatmapObjectModifiedAction : BeatmapAction, IMergeableAction
{
    private bool addToSelection;

    private BaseObject editedData;

    public BaseObject EditedObject;
    private BaseObject originalData;
    public BaseObject OriginalObject;

    private BaseObject preMergeOriginalData;
    
    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedAction() : base() { }

    public BeatmapObjectModifiedAction(BaseObject edited, BaseObject originalObject, BaseObject originalData,
        string comment = "No comment.", bool keepSelection = false, ActionMergeType mergeType = ActionMergeType.None) : base(new[] { edited, originalObject }, comment)
    {
        EditedObject = edited;
        editedData = BeatmapFactory.Clone(edited);

        this.originalData = originalData;
        this.OriginalObject = originalObject;
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
        return MergeType != ActionMergeType.None && previous.MergeType == MergeType && OriginalObject == previousAction.EditedObject && editedData.CompareTo(previousAction.originalData) != 0;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectModifiedAction previousAction) return null;
        var merged = new BeatmapObjectModifiedAction(EditedObject, previousAction.OriginalObject, previousAction.originalData, Comment, addToSelection, MergeType);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";
        merged.preMergeOriginalData = originalData;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == EditedObject ? OriginalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (OriginalObject != EditedObject || editedData.CompareTo(originalData) != 0)
        {
            DeleteObject(EditedObject, false);

            if (originalData != OriginalObject) OriginalObject.Apply(originalData);
            SpawnObject(OriginalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects
            if (originalData != OriginalObject) OriginalObject.Apply(originalData);
            if (OriginalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(OriginalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked)
        {
            SelectionController.Select(OriginalObject, addToSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (OriginalObject != EditedObject || editedData.CompareTo(originalData) != 0)
        {
            if (Networked && MergeCount > 0)
            {
                /*
                 * Since actions over the network come merged, we use the pre-merge data to correctly remove object
                 * e.g.
                 * PC 1 edits object A to B
                 * PC 2 receives edit Action A to B
                 * PC 1 edits objects B to C -> Merges into A to C
                 * PC 2 receives edit Action A to C (with preMerge original data B)
                 */
                DeleteObject(preMergeOriginalData, false);
                
                // We've now handled the intermediate data, now treat it as a non-merged action so undos and redos work 
                MergeCount = 0;
            }
            else
            {
                DeleteObject(OriginalObject, false);
            }

            EditedObject.Apply(editedData);
            SpawnObject(EditedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects 
            EditedObject.Apply(editedData);
            if (OriginalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(OriginalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked)
        {
            SelectionController.Select(EditedObject, addToSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(editedData);
        writer.PutBeatmapObject(originalData);
        
        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(preMergeOriginalData);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        editedData = reader.GetBeatmapObject();
        EditedObject = BeatmapFactory.Clone(editedData);
        originalData = reader.GetBeatmapObject();
        OriginalObject = BeatmapFactory.Clone(originalData);
        
        MergeCount = reader.GetInt();
        if (MergeCount > 0)
        {
            preMergeOriginalData = reader.GetBeatmapObject();
        }

        Data = new[] { EditedObject, OriginalObject };
    }
}
