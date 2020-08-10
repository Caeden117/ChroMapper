using System.Collections.Generic;
using System.Linq;

/*
 * Seems weird? Let me explain.
 * 
 * In a nutshell, this is an Action that groups together multiple other Actions, which are mass undone/redone.
 * This is useful for storing many separate Actions that need to be grouped together, and not clog up the queue.
 */
public class ActionCollectionAction : BeatmapAction
{
    private IEnumerable<BeatmapAction> actions;
    private bool forceRefreshesPool = false;

    public ActionCollectionAction(IEnumerable<BeatmapAction> beatmapActions, bool forceRefreshPool = false, string comment = "No comment.")
        : base(beatmapActions.SelectMany(x => x.Data), comment)
    {
        actions = beatmapActions;
        forceRefreshesPool = forceRefreshPool;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapAction action in actions)
        {
            action.Redo(param);
        }
        if (forceRefreshesPool)
        {
            foreach (BeatmapObject unique in Data.DistinctBy(x => x.beatmapType))
            {
                BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
            }
        }
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        foreach (BeatmapAction action in actions)
        {
            action.Undo(param);
        }
        if (forceRefreshesPool)
        {
            foreach (BeatmapObject unique in Data.DistinctBy(x => x.beatmapType))
            {
                BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
            }
        }
    }
}
