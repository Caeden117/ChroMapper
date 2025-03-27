using System.Collections.Generic;
using System.Linq;
using LiteNetLib.Utils;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Containers;

/*
 * Seems weird? Let me explain.
 * 
 * In a nutshell, this is an Action that groups together multiple other Actions, which are mass undone/redone.
 * This is useful for storing many separate Actions that need to be grouped together, and not clog up the queue.
 *
 * Note that the undos and redos are performed in order. If these undos can result in overlapping objects such that the
 * next undo will affect the wrong object this can cause seemingly random unselected, ghost, or stacked object.
 * For this case, use BeatmapObjectModifiedCollectionAction
 */
public class ActionCollectionAction : BeatmapAction, IMergeableAction
{
    private IEnumerable<BeatmapAction> actions;
    private bool clearSelection;
    private bool forceRefreshesPool;

    public ActionMergeType MergeType {  get; set; }
    public int MergeCount { get; set; }

    // This constructor is needed for United Mapping
    public ActionCollectionAction() : base() { }
    
    public ActionCollectionAction(IEnumerable<BeatmapAction> beatmapActions, bool forceRefreshPool = false,
        bool clearsSelection = true, string comment = "No comment.", ActionMergeType mergeType = ActionMergeType.None)
        : base(beatmapActions.SelectMany(x => x.Data), comment)
    {
        foreach (var beatmapAction in beatmapActions)
        {
            // Stops the actions wastefully refreshing the object pool
            beatmapAction.inCollection = true;
            affectsSeveralObjects = true;
        }

        actions = beatmapActions;
        clearSelection = clearsSelection;
        forceRefreshesPool = forceRefreshPool;
        MergeType = mergeType;
    }
    
    public IMergeableAction TryMerge(IMergeableAction previous)
    {
        return CanMerge(previous) ? DoMerge(previous) : null;
    }

    public bool CanMerge(IMergeableAction previous)
    {
        if (previous is not ActionCollectionAction previousActionCollection) return false;

        if (MergeType == ActionMergeType.None || previousActionCollection.MergeType != MergeType) return false;

        foreach (var action in actions)
            if (action is not IMergeableAction) return false;

        foreach (var action in previousActionCollection.actions)
            if (action is not IMergeableAction) return false;

        if (actions.Count() != previousActionCollection.actions.Count()) return false;

        return true;
    }

    public IMergeableAction DoMerge(IMergeableAction previous)
    {
        if (previous is not ActionCollectionAction previousActionCollection) return null;

        var actionPairs = new Dictionary<IMergeableAction, IMergeableAction>();
        foreach (var action in actions)
        {
            var modifiedAction = (IMergeableAction)action;
            var correspondingAction = (IMergeableAction)previousActionCollection.actions.FirstOrDefault(x => modifiedAction.CanMerge((IMergeableAction)x));
            if (correspondingAction == null) return null;
            actionPairs[modifiedAction] = correspondingAction;
        }

        var mergedActions = new List<BeatmapAction>();
        foreach (var (currentAction, previousAction) in actionPairs)
        {
            mergedActions.Add((BeatmapAction)currentAction.DoMerge(previousAction));
        }

        previousActionCollection.actions = mergedActions;

        previousActionCollection.Comment = previousActionCollection.Comment.Replace($" ({previousActionCollection.MergeCount}x merged)", "");
        previousActionCollection.MergeCount++;
        previousActionCollection.Comment += $" ({previousActionCollection.MergeCount}x merged)";

        return previous;
    }

    public override BaseObject DoesInvolveObject(BaseObject obj)
    {
        foreach (var action in actions)
        {
            var involvedObject = action.DoesInvolveObject(obj);

            if (involvedObject != null) return involvedObject;
        }

        return null;
    }

    public override void Redo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection && !Networked) SelectionController.DeselectAll();

        foreach (var action in actions) action.Redo(param);

        if (forceRefreshesPool) RefreshPools(Data);

        RefreshEventAppearance();
    }

    public override void Undo(BeatmapActionContainer.BeatmapActionParams param)
    {
        if (clearSelection && !Networked) SelectionController.DeselectAll();

        foreach (var action in actions) action.Undo(param);

        if (forceRefreshesPool) RefreshPools(Data);

        RefreshEventAppearance();
    }

    protected override void RefreshEventAppearance()
    {
        var events = actions.SelectMany(x => x.Data).OfType<BaseEvent>().ToList();
        if (!events.Any())
            return;

        var eventContainer = BeatmapObjectContainerCollection.GetCollectionForType<EventGridContainer>(ObjectType.Event);
        eventContainer.MarkEventsToBeRelinked(events);
        eventContainer.LinkAllLightEvents();
        foreach (var evt in events)
        {
            if (evt.Prev != null && eventContainer.LoadedContainers.TryGetValue(evt.Prev, out var evtPrevContainer))
                (evtPrevContainer as EventContainer).RefreshAppearance();
            if (eventContainer.LoadedContainers.TryGetValue(evt, out var evtContainer))
                (evtContainer as EventContainer).RefreshAppearance();
        }
    }

    public override void Serialize(NetDataWriter writer)
    {
        writer.Put(clearSelection);
        writer.Put(forceRefreshesPool);
        writer.Put(actions.Count());

        foreach (var action in actions)
        {
            writer.PutBeatmapAction(action);
        }
    }

    public override void Deserialize(NetDataReader reader)
    {
        clearSelection = reader.GetBool();
        forceRefreshesPool = reader.GetBool();

        var count = reader.GetInt();
        var deserializedActions = new List<BeatmapAction>(count);

        for (var i = 0; i < count; i++)
        {
            var action = reader.GetBeatmapAction(Identity);
            action.inCollection = true;
            deserializedActions.Add(action);
        }

        actions = deserializedActions;
        Data = actions.Where(x => x != null && x.Data != null).SelectMany(x => x.Data);
    }
}
