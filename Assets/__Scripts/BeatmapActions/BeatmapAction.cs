using System.Collections.Generic;
using Beatmap.Base;

/// <summary>
///     A BeatmapAction contains a BeatmapObjectContainer as well as a methods to Undo and Redo the action.
///     An action can consist of placing and deleting, as well as more advanced options, like modifying via Node Editor,
///     or using Strobe Generator
/// </summary>
public abstract class BeatmapAction
{
    public bool Active = true;
    public IEnumerable<BaseObject> Data;
    internal bool inCollection = false;

    public BeatmapAction(IEnumerable<BaseObject> data, string comment = "No comment.")
    {
        Data = data;
        Comment = comment;
    }

    public string Comment { get; } = "No comment.";

    protected void RefreshPools(IEnumerable<BaseObject> data)
    {
        foreach (var unique in data.DistinctBy(x => x.ObjectType))
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(unique.ObjectType);
            collection.RefreshPool(true);

            if (collection is BPMChangeGridContainer con) con.RefreshModifiedBeat();
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
