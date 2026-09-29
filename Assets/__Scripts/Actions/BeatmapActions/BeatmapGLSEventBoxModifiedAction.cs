using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

public class BeatmapGLSEventBoxModifiedAction : BeatmapAction, IMergeableAction
{
    public BaseObject OriginalObject;
    public BaseObject EditedObject;

    public BaseObject PreMergeOriginalData;

    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    // This constructor is needed for United Mapping
    public BeatmapGLSEventBoxModifiedAction() : base() { }

    public BeatmapGLSEventBoxModifiedAction(
        BaseObject newObject,
        BaseObject oldObject,
        string comment = "No comment.",
        ActionMergeType mergeType = ActionMergeType.None) : base(new[] { newObject }, comment)
    {
        EditedObject = newObject;
        OriginalObject = oldObject;
        MergeType = mergeType;
    }

    public IMergeableAction TryMerge(IMergeableAction previous) => CanMerge(previous) ? DoMerge(previous) : null;

    public bool CanMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapGLSEventBoxModifiedAction previousAction) return false;
        return MergeType != ActionMergeType.None
            && previous.MergeType == MergeType
            && OriginalObject.CompareTo(previousAction.OriginalObject) == 0;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapGLSEventBoxModifiedAction previousAction) return null;
        var merged = new BeatmapGLSEventBoxModifiedAction(
            EditedObject,
            previousAction.OriginalObject,
            Comment,
            MergeType);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";
        merged.PreMergeOriginalData = OriginalObject;
        merged.wasMerged = true;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == EditedObject ? OriginalObject : null;

    private bool wasMerged;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var restoreGroupSelection = !Networked && SelectionController.IsObjectSelected(EditedObject);
        DeleteObject(EditedObject, false);
        SpawnObject(OriginalObject);
        if (restoreGroupSelection)
        {
            SelectionController.Select(OriginalObject, true, false, false);
            SelectionController.OnSelectionChanged?.Invoke();
        }
        // Refresh only the replaced GLS group; force-refreshing every group races rapid outer-preview wheel input.
        RefreshModifiedGroupPool();
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var removedGroup = wasMerged ? PreMergeOriginalData : OriginalObject;
        var restoreGroupSelection = !Networked && SelectionController.IsObjectSelected(removedGroup);
        DeleteObject(removedGroup, false);
        SpawnObject(EditedObject);
        if (restoreGroupSelection)
        {
            SelectionController.Select(EditedObject, true, false, false);
            SelectionController.OnSelectionChanged?.Invoke();
        }
        // Refresh only the replaced GLS group; force-refreshing every group races rapid outer-preview wheel input.
        RefreshModifiedGroupPool();
        wasMerged = false;
    }

    private void RefreshModifiedGroupPool()
    {
        BeatmapObjectContainerCollection
            .GetCollectionForType(EditedObject.ObjectType)
            .RefreshPool();
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(EditedObject);
        writer.PutBeatmapObject(OriginalObject);

        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(PreMergeOriginalData);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        EditedObject = reader.GetBeatmapObject();
        EditedObject = BeatmapFactory.Clone(EditedObject);
        OriginalObject = reader.GetBeatmapObject();
        OriginalObject = BeatmapFactory.Clone(OriginalObject);

        MergeCount = reader.GetInt();
        if (MergeCount > 0) PreMergeOriginalData = reader.GetBeatmapObject();

        Data = new[] { EditedObject, OriginalObject };
    }
}
