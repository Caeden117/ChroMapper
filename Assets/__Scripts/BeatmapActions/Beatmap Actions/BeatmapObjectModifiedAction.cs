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

    private BaseObject preMergeOriginalData;
    
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
        merged.preMergeOriginalData = originalData;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == editedObject ? originalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (originalObject != editedObject || editedData.CompareTo(originalData) != 0)
        {
            DeleteObject(editedObject, false);

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
                DeleteObject(originalObject, false);
            }

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
        
        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(preMergeOriginalData);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        editedData = reader.GetBeatmapObject();
        editedObject = BeatmapFactory.Clone(editedData);
        originalData = reader.GetBeatmapObject();
        originalObject = BeatmapFactory.Clone(originalData);
        
        MergeCount = reader.GetInt();
        if (MergeCount > 0)
        {
            preMergeOriginalData = reader.GetBeatmapObject();
        }

        Data = new[] { editedObject, originalObject };
    }
}
