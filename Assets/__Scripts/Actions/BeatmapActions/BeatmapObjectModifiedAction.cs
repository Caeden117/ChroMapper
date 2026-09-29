using System;
using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

[Obsolete("This has potential to create ghost objects if used incorrectly. Use BeatmapObjectUpdatedAction")]
public class BeatmapObjectModifiedAction : BeatmapAction, IMergeableAction
{
    private bool addToSelection;

    private bool preserveSelection;

    public BaseObject EditedData;
    public BaseObject EditedObject;
    public BaseObject OriginalData;
    public BaseObject OriginalObject;

    private BaseObject preMergeOriginalData;

    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    // This constructor is needed for United Mapping
    public BeatmapObjectModifiedAction() : base() { }

    public BeatmapObjectModifiedAction(BaseObject edited, BaseObject originalObject, BaseObject originalData,
        string comment = "No comment.", bool keepSelection = false, ActionMergeType mergeType = ActionMergeType.None,
        bool preserveSelection = false) : base(new[] { edited, originalObject }, comment)
    {
        EditedObject = edited;
        EditedData = BeatmapFactory.Clone(edited);

        this.OriginalData = originalData;
        this.OriginalObject = originalObject;
        addToSelection = keepSelection;
        MergeType = mergeType;
        this.preserveSelection = preserveSelection;
    }

    public IMergeableAction TryMerge(IMergeableAction previous)
    {
        return CanMerge(previous) ? DoMerge(previous) : null;
    }

    public bool CanMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectModifiedAction previousAction) return false;
        return MergeType != ActionMergeType.None && previous.MergeType == MergeType && OriginalObject == previousAction.EditedObject && EditedData.CompareTo(previousAction.OriginalData) != 0;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectModifiedAction previousAction) return null;
        var merged = new BeatmapObjectModifiedAction(EditedObject, previousAction.OriginalObject, previousAction.OriginalData, Comment, addToSelection, MergeType, preserveSelection);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";
        merged.preMergeOriginalData = OriginalData;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == EditedObject ? OriginalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var restoreOriginalSelection = preserveSelection && SelectionController.IsObjectSelected(EditedObject);
        if (OriginalObject != EditedObject || EditedData.CompareTo(OriginalData) != 0)
        {
            DeleteObject(EditedObject, false, EditedObject is not BaseGLSEvent);

            if (OriginalData != OriginalObject) OriginalObject.Apply(OriginalData);
            SpawnObject(OriginalObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects
            if (OriginalData != OriginalObject) OriginalObject.Apply(OriginalData);
            if (OriginalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(OriginalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked && (!preserveSelection || restoreOriginalSelection))
        {
            SelectionController.Select(OriginalObject, addToSelection || preserveSelection, true, !inCollection);
        }
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var restoreEditedSelection = preserveSelection && SelectionController.IsObjectSelected(OriginalObject);
        if (OriginalObject != EditedObject || EditedData.CompareTo(OriginalData) != 0)
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
                DeleteObject(preMergeOriginalData, false, preMergeOriginalData is not BaseGLSEvent);
                
                // We've now handled the intermediate data, now treat it as a non-merged action so undos and redos work 
                MergeCount = 0;
            }
            else
            {
                DeleteObject(OriginalObject, false, OriginalObject is not BaseGLSEvent);
            }

            EditedObject.Apply(EditedData);
            SpawnObject(EditedObject, false, !inCollection);
        }
        else
        {
            // This is an optimisation only possible if the object has not changed position in the MapObjects 
            EditedObject.Apply(EditedData);
            if (OriginalObject is BaseBpmEvent) BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(OriginalObject.JsonTime);
            if (!inCollection) RefreshPools(Data);
        }

        if (!Networked && (!preserveSelection || restoreEditedSelection))
        {
            SelectionController.Select(EditedObject, addToSelection || preserveSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(EditedData);
        writer.PutBeatmapObject(OriginalData);
        
        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(preMergeOriginalData);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        EditedData = reader.GetBeatmapObject();
        EditedObject = BeatmapFactory.Clone(EditedData);
        OriginalData = reader.GetBeatmapObject();
        OriginalObject = BeatmapFactory.Clone(OriginalData);
        
        MergeCount = reader.GetInt();
        if (MergeCount > 0)
        {
            preMergeOriginalData = reader.GetBeatmapObject();
        }

        Data = new[] { EditedObject, OriginalObject };
    }
}
