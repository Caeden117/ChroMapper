using System.Collections.Generic;

/// <summary>
///     A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
///     An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
///     or using Strobe Generator
/// </summary>
public abstract class BeatmapAction
{
    public bool Active = true;
    public IEnumerable<BeatmapObject> Data;
    internal bool InCollection = false;

    public BeatmapAction(IEnumerable<BeatmapObject> data, string comment = "No comment.")
    {
        Data = data;
        Comment = comment;
    }

    public string Comment { get; } = "No comment.";

    protected void RefreshPools(IEnumerable<BeatmapObject> data)
    {
        foreach (var unique in data.DistinctBy(x => x.BeatmapType))
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(unique.BeatmapType);
            collection.RefreshPool(true);

            if (collection is BPMChangesContainer con) con.RefreshModifiedBeat();
        }
    }

    /// <summary>
    ///     Steps that should be taken to Undo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Undo(BeatmapActionContainer.BeatmapActionParams param);

    /// <summary>
    ///     Steps that should be taken to Redo an Action.
    /// </summary>
    /// <param name="param">Collection of useful stuff.</param>
    public abstract void Redo(BeatmapActionContainer.BeatmapActionParams param);
}
