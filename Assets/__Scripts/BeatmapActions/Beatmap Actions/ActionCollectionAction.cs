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
    private bool clearSelection = false;

    public ActionCollectionAction(IEnumerable<BeatmapAction> beatmapActions, bool forceRefreshPool = false, bool clearsSelection = true, string comment = "No comment.")
        : base(beatmapActions.SelectMany(x => x.Data), comment)
    {
        foreach (var beatmapAction in beatmapActions)
        {
            // Stops the actions wastefully refreshing the object pool
            beatmapAction.InCollection = true;
        }

        actions = beatmapActions;
        clearSelection = clearsSelection;
        forceRefreshesPool = forceRefreshPool;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection)
        {
            SelectionController.DeselectAll();
        }

        foreach (BeatmapAction action in actions)
        {
            action.Redo(param);
        }

        if (forceRefreshesPool) RefreshPools(Data);
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection)
        {
            SelectionController.DeselectAll();
        }

        foreach (BeatmapAction action in actions)
        {
            action.Undo(param);
        }

        if (forceRefreshesPool) RefreshPools(Data);
    }
}
