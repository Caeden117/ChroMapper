using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Helper;

/// <summary>
/// Replaces one live beatmap object with an edited copy while preserving undo identity.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="OriginalObject"/> must be the exact, currently live object instance. It must not be cloned and must
/// remain unedited when the action is constructed.
/// </para>
/// <para>
/// <see cref="EditedObject"/> must be a separate clone of that live object containing the requested edits. Never
/// mutate the live original and pass an unedited snapshot as <see cref="OriginalObject"/>; older placement and paste
/// actions retain the live reference and will otherwise become stale during undo.
/// </para>
/// <para>
/// If input code has already edited the live object in place, first clone its edited state, restore and reparse the
/// live object from the pre-edit snapshot, then construct this action with the edited clone and restored live object
/// and add it with <c>perform: true</c>.
/// </para>
/// </remarks>
public class BeatmapObjectUpdatedAction : BeatmapAction, IMergeableAction
{
    // This must be a separately edited clone, never the currently live object instance.
    public BaseObject EditedObject;

    // This must be the exact unedited live instance, never a clone of its original state.
    public BaseObject OriginalObject;

    public BaseObject PreMergeOriginalObject;

    public ActionMergeType MergeType { get; set; }
    public int MergeCount { get; set; }

    private bool addToSelection;
    private bool preserveSelection;

    // This constructor is needed for United Mapping
    public BeatmapObjectUpdatedAction() : base() { }

    /// <summary>
    /// Creates an object replacement action from an edited clone and its exact unedited live original.
    /// </summary>
    /// <param name="editedObject">A separately cloned object containing the edits.</param>
    /// <param name="originalObject">The exact currently live, unedited object instance; never a clone.</param>
    /// <param name="comment">Description stored in the action history.</param>
    /// <param name="keepSelection">Whether the replacement should be added to the current selection.</param>
    /// <param name="mergeType">The operation identity used to merge consecutive compatible replacements.</param>
    /// <param name="preserveSelection">Whether unrelated selection remains unchanged and the replacement inherits only the original object's selected state.</param>
    public BeatmapObjectUpdatedAction(
        BaseObject editedObject,
        BaseObject originalObject,
        string comment = "No comment.",
        bool keepSelection = false,
        ActionMergeType mergeType = ActionMergeType.None,
        bool preserveSelection = false)
        : base(new[] { editedObject, originalObject }, comment)
    {
        EditedObject = editedObject;
        OriginalObject = originalObject;
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
        if (previous is not BeatmapObjectUpdatedAction previousAction) return false;
        return MergeType != ActionMergeType.None
            && previous.MergeType == MergeType
            && OriginalObject == previousAction.EditedObject
            // Don't merge if the new edit restores the previous action's pre-edit state (e.g. a toggle back to the original value).
            // Without this check, toggle operations (A→B then B→A) would merge into A→A, causing undo to appear broken
            // since the intermediate state B would be lost and undoing would leave the object at A instead of B.
            && EditedObject.CompareTo(previousAction.OriginalObject) != 0;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not BeatmapObjectUpdatedAction previousAction) return null;
        var merged = new BeatmapObjectUpdatedAction(
            EditedObject,
            previousAction.OriginalObject,
            Comment,
            addToSelection,
            MergeType,
            preserveSelection);

        merged.MergeCount = previousAction.MergeCount + 1;
        merged.Comment += $" ({merged.MergeCount}x merged)";
        merged.PreMergeOriginalObject = OriginalObject;

        return merged;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj) => obj == EditedObject ? OriginalObject : null;

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var restoreOriginalSelection = preserveSelection && SelectionController.IsObjectSelected(EditedObject);
        DeleteObject(EditedObject, false, EditedObject is not BaseGLSEvent);
        SpawnObject(OriginalObject);
        if (!preserveSelection && !addToSelection) SelectionController.DeselectAll();
        // This is necessary or else undo's leave weird ghost stuff around that reappears on redo or something wonky like that.
        // Unclear why this is necessary but Redo's isnt.
        RefreshPools(Data);

        if (!Networked && (!preserveSelection || restoreOriginalSelection))
        {
            SelectionController.Select(OriginalObject, preserveSelection || addToSelection, true, !inCollection);
        }
    }

    /// <summary>
    /// THIS CAN ALSO BE CALLED TO DO THE ACTION THE FIRST TIME IT IS PERFORMED WHEN BeatmapActionContainer.AddAction(updateAction, true); IS PASSED.
    /// Redo the undone action (or in BeatmapActionContainer.AddAction(updateAction, true) case, do it for the first time as well).
    /// </summary>
    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        var restoreEditedSelection = preserveSelection && SelectionController.IsObjectSelected(OriginalObject);
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
            DeleteObject(PreMergeOriginalObject, false, PreMergeOriginalObject is not BaseGLSEvent);

            // We've now handled the intermediate data, now treat it as a non-merged action so undos and redos work 
            MergeCount = 0;
        }
        else
        {
            DeleteObject(OriginalObject, false, OriginalObject is not BaseGLSEvent);
        }

        SpawnObject(EditedObject, false, !inCollection);
        if (!preserveSelection && !addToSelection) SelectionController.DeselectAll();

        // Don't think refresh pools is necessary
        // RefreshPools(Data);

        if (!Networked && (!preserveSelection || restoreEditedSelection))
        {
            SelectionController.Select(EditedObject, preserveSelection || addToSelection, true, !inCollection);
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.PutBeatmapObject(EditedObject);
        writer.PutBeatmapObject(OriginalObject);

        writer.Put(MergeCount);
        if (MergeCount > 0)
        {
            writer.PutBeatmapObject(PreMergeOriginalObject);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        EditedObject = BeatmapFactory.Clone(reader.GetBeatmapObject());
        OriginalObject = BeatmapFactory.Clone(reader.GetBeatmapObject());

        MergeCount = reader.GetInt();
        if (MergeCount > 0)
        {
            PreMergeOriginalObject = reader.GetBeatmapObject();
        }

        Data = new[] { EditedObject, OriginalObject };
    }
}
