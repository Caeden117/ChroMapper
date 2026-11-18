using System;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine;
using ZLinq;

public class GLSEventGridContainer : BeatmapObjectContainerCollection<BaseGLSEvent>
{
    [SerializeField] private GLSEventGridProvider glsEventGridProvider;
    [SerializeField] private EventGridContainer eventGridContainer;

    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private GLSEventAppearanceSO glsEventAppearance;

    [SerializeField] private CountersPlusController countersPlus;

    // A collection action replaces several child events before the parent GLS group can be rebuilt safely.
    private int groupReplacementBatchDepth;
    // Alt-drag mutates a child still referenced by its parent, so the next replacement must consume an untouched pre-drag group clone.
    private BaseEventBoxGroup nextReplacementOriginalGroupData;
    // Reuse indexed transition candidates because viewport refreshes occur while dragging and scrolling.
    private readonly List<BaseLightColorBase> retainedTransitionSources = new();
    // Cache the environment light count for ribbon bounds checks during this refresh.
    private int displayedColorLightCount;
    // Keep selected children while replacing their group, then select the corresponding replacement nodes.
    private BaseEventBoxGroup pendingSelectionSource;
    private List<BaseGLSEvent> pendingSelectionRestore;
    private bool selectionPreparedForReplacement;

    public override ObjectType ContainerType => ObjectType.GLSEvent;

    // Queue previews need the same boost lookup as finalized GLS child nodes without rediscovering the event grid.
    public bool IsBoostAt(float jsonTime) => eventGridContainer.IsBoostAt(jsonTime);

    public override ObjectContainer CreateContainer() =>
        GLSEventContainer.SpawnGLSEvent(null, BeatmapContext.TrackDefinitions, ref eventPrefab);

    internal override void SubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled += HandlePlayToggle;
        BeatmapContext.OnBeatmapTimingChanged += HandleBeatmapTimingChanged;
        glsEventGridProvider.OnGroupChanged += HandleGroupChanged;
        // Retired groups have no replacement group, so clear their inner node collection through the dedicated lifecycle signal.
        glsEventGridProvider.OnGroupRetired += HandleGroupRetired;
        eventGridContainer.OnBoostAppearanceRangeInvalidated += RefreshBoostDependentAppearances;
        Settings.NotifyBySettingName(
            nameof(Settings.VisualizeGLSLightTransitions),
            RefreshLoadedColorTransitionRibbons);
    }

    internal override void UnsubscribeToCallbacks()
    {
        BeatmapContext.Atsc.OnPlayToggled -= HandlePlayToggle;
        BeatmapContext.OnBeatmapTimingChanged -= HandleBeatmapTimingChanged;
        glsEventGridProvider.OnGroupChanged -= HandleGroupChanged;
        // Match the dedicated retirement subscription so destroyed containers cannot receive later cleanup callbacks.
        glsEventGridProvider.OnGroupRetired -= HandleGroupRetired;
        eventGridContainer.OnBoostAppearanceRangeInvalidated -= RefreshBoostDependentAppearances;
        Settings.StopNotifyingBySettingName(
            nameof(Settings.VisualizeGLSLightTransitions),
            RefreshLoadedColorTransitionRibbons);
    }

    private void RefreshLoadedColorTransitionRibbons(object _)
    {
        foreach (var loaded in LoadedContainers.Values)
        {
            if (loaded is not GLSEventContainer container || container.EventData is not BaseLightColorBase)
            {
                continue;
            }

            glsEventAppearance.UpdateTransitionRibbon(
                container, eventGridContainer.IsBoostAt, eventGridContainer.BoostEvents);
        }
    }

    private void RefreshBoostDependentAppearances(float startJsonTime, float endJsonTime)
    {
        var songContainer = BeatSaberSongContainer.Instance;
        var map = songContainer != null
            ? songContainer.Map
            : null;
        // Ribbon bounds are indexed in song-bpm time, so convert the interval once.
        var startSongBpmTime = map?.JsonTimeToSongBpmTime(startJsonTime) ?? startJsonTime;
        var endSongBpmTime = map?.JsonTimeToSongBpmTime(endJsonTime) ?? endJsonTime;
        foreach (var loaded in LoadedContainers.Values)
        {
            if (loaded is not GLSEventContainer container
                || container.EventData is not BaseLightColorBase colorEvent)
            {
                continue;
            }

            // Basic events already repaint this interval; GLS child containers need the same boost lookup reapplied.
            if (colorEvent.JsonTime >= startJsonTime && colorEvent.JsonTime < endJsonTime)
            {
                glsEventAppearance.SetAppearance(container, true, eventGridContainer.IsBoostAt(colorEvent.JsonTime));
            }

            // A source that predates the edited boost still spans it: refresh ribbons by bounds overlap,
            // not only by node time.
            if (map != null
                && GLSEventCommon.TryGetColorRibbonBounds(
                    colorEvent, container.GlsLightCount, out var ribbonStart, out var ribbonEnd)
                && ribbonStart < endSongBpmTime
                && ribbonEnd > startSongBpmTime)
            {
                glsEventAppearance.UpdateTransitionRibbon(
                    container, eventGridContainer.IsBoostAt, eventGridContainer.BoostEvents);
            }
        }
    }

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        if (groupReplacementBatchDepth == 0) ReplaceGroup(obj, "Placed a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        if (groupReplacementBatchDepth == 0) ReplaceGroup(obj, "Deleted a GLS Event.");
        countersPlus.UpdateStatistic(CountersPlusStatistic.GLSEvents);
    }

    public void BeginGroupReplacementBatch()
    {
        // Suppress intermediate GLS group actions while a bulk edit replaces individual child events.
        groupReplacementBatchDepth++;
    }

    public void EndGroupReplacementBatch(string message)
    {
        // Publish one complete parent group so the light simulation never caches a partial bulk edit.
        if (groupReplacementBatchDepth == 0) return;
        groupReplacementBatchDepth--;
        if (groupReplacementBatchDepth == 0 && MapObjects.Count > 0) ReplaceGroup(MapObjects[0], message);
    }

    // A nested parent replacement selects the parent, so restore the affected child identities after bulk child actions finish.
    public void RebindSelectionAfterBatch(IEnumerable<BaseGLSEvent> sourceEvents)
    {
        var group = glsEventGridProvider.GroupContext;
        if (group == null || MapObjects.Count == 0)
        {
            return;
        }

        SelectionController.Deselect(group, false);
        var replacementLookup = new GLSEventReplacementLookup(MapObjects);
        foreach (var sourceEvent in sourceEvents)
        {
            if (sourceEvent.EventBoxGroupData?.HasSameContent(group) != true)
            {
                continue;
            }

            if (replacementLookup.TryTake(sourceEvent, out var replacement))
            {
                SelectionController.Select(replacement, true, false, false);
            }
        }

        SelectionController.NotifySelectionChanged();
    }

    // Preserve the complete pre-drag parent, including the source and any destination conflict, for one replacement action.
    public void UseOriginalGroupForNextReplacement(BaseEventBoxGroup originalGroup) =>
        nextReplacementOriginalGroupData = originalGroup;

    // A view-only XYZ lane becomes real when it gets its first node added.
    public void PlaceInDisplayOnlyLane(BaseGLSEvent sourceEvent, BaseEventBox displayBox)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null || displayBox == null)
        {
            return;
        }

        var newGroup = BeatmapFactory.Clone(liveGroup);
        if (!GLSCommonCommand.TryMaterializeAutomaticAxisLane(
                newGroup,
                displayBox,
                out var newBox,
                out var newBoxIndex))
        {
            return;
        }

        var newEvent = BeatmapFactory.Clone(sourceEvent);
        newEvent.EventBoxGroupData = newGroup;
        newEvent.EventBoxData = newBox;
        newEvent.BoxIndex = newBoxIndex;
        newBox.SetEvents(new[] { newEvent });

        // Sorting changes existing box indexes, so finalize all ownership before pruning and publishing the replacement group.
        GLSCommonCommand.RebindGroup(newGroup);
        newGroup.PruneEmptyAutomaticAxisLanes();

        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            liveGroup,
            "Placed a GLS Event in a new axis lane.");
        BeatmapActionContainer.AddAction(action, true);
    }

    public void MoveToDisplayOnlyLane(
        BaseGLSEvent movedEvent,
        BaseGLSEvent originalEvent,
        BaseEventBox displayBox,
        BaseEventBoxGroup originalGroup)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null
            || originalEvent == null
            || originalGroup == null
            || liveGroup.GetType() != originalGroup.GetType()
            || originalEvent.BoxIndex < 0
            || originalEvent.BoxIndex >= originalGroup.ReadOnlyBoxes.Count)
        {
            return;
        }

        var newGroup = BeatmapFactory.Clone(originalGroup);
        var sourceBox = newGroup.ReadOnlyBoxes[originalEvent.BoxIndex];
        var sourceEventIndex = -1;
        for (var eventIndex = 0; eventIndex < sourceBox.ReadOnlyEvents.Count; eventIndex++)
        {
            if (Math.Abs(sourceBox.ReadOnlyEvents[eventIndex].RelativeJsonTime - originalEvent.RelativeJsonTime)
                < BeatmapObjectContainerCollection.Epsilon)
            {
                sourceEventIndex = eventIndex;
                break;
            }
        }

        if (sourceEventIndex < 0)
        {
            return;
        }

        if (!GLSCommonCommand.TryMaterializeAutomaticAxisLane(
                newGroup,
                displayBox,
                out var newBox,
                out var newBoxIndex))
        {
            return;
        }

        var remainingEvents = new BaseGLSEvent[sourceBox.ReadOnlyEvents.Count - 1];
        for (var sourceIndex = 0; sourceIndex < sourceEventIndex; sourceIndex++)
        {
            remainingEvents[sourceIndex] = sourceBox.ReadOnlyEvents[sourceIndex];
        }
        for (var sourceIndex = sourceEventIndex + 1; sourceIndex < sourceBox.ReadOnlyEvents.Count; sourceIndex++)
        {
            remainingEvents[sourceIndex - 1] = sourceBox.ReadOnlyEvents[sourceIndex];
        }
        sourceBox.SetEvents(remainingEvents);

        var newEvent = BeatmapFactory.Clone(movedEvent);
        newEvent.EventBoxGroupData = newGroup;
        newEvent.EventBoxData = newBox;
        newEvent.BoxIndex = newBoxIndex;
        newBox.SetEvents(new[] { newEvent });

        // Materialization reorders boxes and a drag may vacate its source, so rebind first and let pruning repair removals.
        GLSCommonCommand.RebindGroup(newGroup);
        newGroup.PruneEmptyAutomaticAxisLanes();
        movedEvent.RelativeJsonTime = originalEvent.RelativeJsonTime;
        movedEvent.EventBoxGroupData = liveGroup;
        movedEvent.EventBoxData = liveGroup.ReadOnlyBoxes[originalEvent.BoxIndex];
        movedEvent.BoxIndex = originalEvent.BoxIndex;
        movedEvent.RecomputeSongBpmTime();
        var action = new BeatmapGLSEventBoxModifiedAction(
            newGroup,
            liveGroup,
            "Moved a GLS Event into a new axis lane.");
        BeatmapActionContainer.AddAction(action, true);
    }

    // Rejected drags need an action-free rollback because their live parent temporarily contains the dragged child's invalid offset.
    public void RestoreRejectedDrag(BaseEventBoxGroup originalGroup)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null
            || originalGroup == null
            || liveGroup.GetType() != originalGroup.GetType())
        {
            // A rejected drag must never publish an invalid transient group merely because its original snapshot cannot be applied.
            Debug.LogError($"Could not restore a rejected GLS drag because its group context was unavailable. {Beatmap.BeatmapLogContext.Describe(originalGroup ?? liveGroup)}");
            return;
        }

        // Color transition indexes own child identities, so retire them before Apply replaces every child clone in place.
        if (liveGroup is BaseLightColorEventBoxGroup oldColorGroup)
        {
            GLSEventCommon.RemoveColorTransitionGroup(oldColorGroup);
        }

        // Restore the exact live group and its open child collection without creating an undo entry for the rejected destination.
        liveGroup.Apply(originalGroup);
        if (liveGroup is BaseLightColorEventBoxGroup restoredColorGroup)
        {
            GLSEventCommon.AddColorTransitionGroup(restoredColorGroup);
        }

        // The restore bypasses spawn/delete callbacks, so flush outer preview ribbons explicitly.
        var colorCollection = BeatmapObjectContainerCollection
            .GetCollectionForType<GLSGroupColorGridContainer>(ObjectType.GLSColor);
        if (colorCollection != null)
        {
            colorCollection.RequestColorTransitionRefresh();
        }

        nextReplacementOriginalGroupData = null;
        HandleGroupChanged(liveGroup);
    }

    // Selection deletion must publish one parent edit instead of one replacement action per selected child.
    public BeatmapAction CreateSelectionDeleteAction(IReadOnlyCollection<BaseGLSEvent> deletedEvents)
    {
        var liveGroup = glsEventGridProvider.GroupContext;
        if (liveGroup == null || deletedEvents.Count == 0)
        {
            return null;
        }

        // A hash set keeps the single open-group rebuild linear when many selected inner nodes are deleted together.
        var excludedEvents = new HashSet<BaseGLSEvent>(deletedEvents);
        var newGroup = BuildGroupFromMapObjects(liveGroup, excludedEvents);
        // GLS parent actions intentionally do not select the outer group after replacing inner node identities.
        return new BeatmapGLSEventBoxModifiedAction(newGroup, liveGroup, "Deleted a selection of GLS events.");
    }

    // Parent replacement publishes the edit after all conflicting children have been removed.
    protected override void DeleteConflictingObject(BaseGLSEvent conflict) =>
        DeleteObject(conflict, false, false, triggerHandle: false);

    private void ReplaceGroup(BaseObject obj, string msg)
    {
        var glsEvt = obj as BaseGLSEvent;
        var liveOriginalGroup = glsEventGridProvider.GroupContext;
        if (liveOriginalGroup == null
            || !ReferenceEquals(glsEvt.EventBoxGroupData, liveOriginalGroup))
        {
            // A retired dragged child must never replace the currently open group after undo or another group action changes context.
            // SpawnObject inserts before this callback, so remove the rejected stale child without publishing another group action.
            SilentRemoveObject(glsEvt);
            nextReplacementOriginalGroupData = null;
            return;
        }

        var originalGroupData = nextReplacementOriginalGroupData;
        nextReplacementOriginalGroupData = null;
        // Convert the authoritative child collection back into one replacement parent group.
        var newGroup = BuildGroupFromMapObjects(glsEvt.EventBoxGroupData);

        if (originalGroupData != null)
        {
            // Restore the exact live group before manager removal; the immutable clone supplies its pre-drag child data only.
            liveOriginalGroup.Apply(originalGroupData);
        }

        // Inner-node mutation replaces its parent, but must never auto-select that outer group in the EventBox view.
        var action = new BeatmapGLSEventBoxModifiedAction(newGroup, liveOriginalGroup, msg);
        BeatmapActionContainer.AddAction(action, true);
    }

    // Share the open-group rebuild so single edits and bulk selection deletes preserve identical lane/conflict behavior.
    private BaseEventBoxGroup BuildGroupFromMapObjects(
        BaseEventBoxGroup sourceGroup,
        ISet<BaseGLSEvent> excludedEvents = null)
    {
        // Every inner mutation must rebuild only its open parent and preserve all authored filter lanes.
        var newGroup = BeatmapFactory.Clone(sourceGroup);
        // Preserve every authored lane when the final event is deleted so the mapper can place into them again.
        foreach (var box in newGroup.ReadOnlyBoxes) box.ClearEvents();

        // the typa shit i had to pull to amke this work
        foreach (var boxEvents in MapObjects
            .Where(e => excludedEvents == null || !excludedEvents.Contains(e))
            .Select(e =>
            {
                var newEvt = BeatmapFactory.Clone(e);
                newEvt.EventBoxGroupData = newGroup;
                newEvt.EventBoxData = newGroup.ReadOnlyBoxes[e.BoxIndex];
                newEvt.BoxIndex = e.BoxIndex;
                return newEvt;
            })
            .GroupBy(e => e.BoxIndex))
            newGroup.ReadOnlyBoxes[boxEvents.Key].SetEvents(boxEvents.ToArray());
        // co-variant deez

        newGroup.PruneEmptyAutomaticAxisLanes();
        return newGroup;
    }

    private void HandlePlayToggle(bool playing)
    {
        if (!playing) RefreshPool();
    }

    private void HandleBeatmapTimingChanged(float _) => RefreshPool(true);

    public void PrepareSelectionForGroupReplacement(
        BaseEventBoxGroup sourceGroup,
        BaseEventBoxGroup replacementGroup,
        List<BaseGLSEvent> selectedEvents)
    {
        if (!ReferenceEquals(glsEventGridProvider.GroupContext, sourceGroup))
            return;

        // The manager replaces the child collection after the action returns, so restore known destinations at that callback.
        pendingSelectionSource = replacementGroup;
        pendingSelectionRestore = selectedEvents;
        selectionPreparedForReplacement = true;
    }

    private void HandleGroupChanged(BaseEventBoxGroup group)
    {
        // Snapshot only selected child nodes owned by the retiring collection before parent replacement changes identity.
        var selectedEvents = new List<BaseGLSEvent>();
        foreach (var selectedObject in SelectionController.SelectedObjects)
        {
            if (selectedObject is BaseGLSEvent selectedEvent && MapObjects.Contains(selectedEvent))
            {
                selectedEvents.Add(selectedEvent);
            }
        }

        if (group == null)
        {
            RetireGroupContext(selectedEvents);
            return;
        }

        var newEvents = group.ReadOnlyBoxes.AsValueEnumerable().SelectMany(box => box.ReadOnlyEvents).ToArray();

        // Retire visuals owned by the previous parent before replacing the child-object identities.
        RecycleAllContainers();
        MapObjects.Clear();
        MapObjects.AddRange(newEvents);
        MapObjects.Sort();
        RefreshPool();

        if (pendingSelectionSource != null)
        {
            if (pendingSelectionSource.IsConflictingWith(group))
            {
                selectedEvents = pendingSelectionRestore;
            }

            pendingSelectionSource = null;
            pendingSelectionRestore = null;
            selectionPreparedForReplacement = false;
        }

        if (selectedEvents.Count == 0) return;

        // Queue replacement nodes by identity once so stacked duplicates rebind in O(old selections + replacements).
        var replacementLookup = new GLSEventReplacementLookup(newEvents);
        List<BaseGLSEvent> laneMovedSelections = null;
        foreach (var selectedEvent in selectedEvents)
        {
            SelectionController.Deselect(selectedEvent, false);
            if (selectedEvent.EventBoxGroupData?.HasSameContent(group) != true)
                continue;

            if (!replacementLookup.TryTake(selectedEvent, out var replacement))
            {
                laneMovedSelections ??= new List<BaseGLSEvent>();
                laneMovedSelections.Add(selectedEvent);
                continue;
            }

            SelectionController.Select(replacement, true, false, false);
        }

        // A lane move changes a node's stable identity, so leftovers rebind only when exactly one
        // same-time replacement remains unconsumed in this single-type group.
        if (laneMovedSelections != null)
        {
            foreach (var selectedEvent in laneMovedSelections)
            {
                if (replacementLookup.TryTakeUniqueIgnoringLane(selectedEvent, out var replacement))
                {
                    SelectionController.Select(replacement, true, false, false);
                }
            }
        }

        SelectionController.NotifySelectionChanged();
    }

    // Retired groups deliberately have no replacement, so reuse the container's null-retirement branch without notifying group UI.
    private void HandleGroupRetired() => HandleGroupChanged(null);

    // A deleted outer group has no replacement context; retire every inner child and its pooled visual immediately.
    private void RetireGroupContext(IReadOnlyCollection<BaseGLSEvent> selectedEvents)
    {
        RecycleAllContainers();

        MapObjects.Clear();

        // Stash the retired group's selected children before deselecting them; a replacement context rebinds
        // them by stable identity while a genuine retirement leaves nothing to restore.
        if (selectedEvents.Count > 0 && !selectionPreparedForReplacement)
        {
            pendingSelectionRestore = new List<BaseGLSEvent>(selectedEvents);
            pendingSelectionSource = selectedEvents.First().EventBoxGroupData;
        }

        foreach (var selectedEvent in selectedEvents)
        {
            SelectionController.Deselect(selectedEvent, false);
        }

        if (selectedEvents.Count > 0)
        {
            SelectionController.NotifySelectionChanged();
        }
    }

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var c = con as GLSEventContainer;
        c.DisplayLaneIndex = glsEventGridProvider.GetDisplayedLaneIndex(((BaseGLSEvent)obj).BoxIndex);
        c.LightGradientController.BindRibbonLane(glsEventGridProvider.RibbonGridLane,
            c.transform, c.transform.parent);
        c.IncomingLightGradientController.BindRibbonLane(glsEventGridProvider.RibbonGridLane,
            c.transform, c.transform.parent);
        con.UpdateGridPosition();

        c.GlsLightCount = BeatmapContext.GetGlsLightCount(glsEventGridProvider.GroupContext.ID);
        glsEventAppearance.SetAppearance(c, true, eventGridContainer.IsBoostAt(obj.JsonTime));
        glsEventAppearance.UpdateTransitionRibbon(
            c, eventGridContainer.IsBoostAt, eventGridContainer.BoostEvents);
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Incoming first-node ribbons must be indexed before their target's ordinary timestamp enters the viewport.
        var group = glsEventGridProvider.GroupContext;
        displayedColorLightCount = group is BaseLightColorEventBoxGroup ? BeatmapContext.GetGlsLightCount(group.ID) : 0;
        if (group != null)
            GLSEventCommon.SetColorTransitionLightCount(group.ID, displayedColorLightCount);
        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        GLSEventCommon.GetColorTransitionSourcesAt(
            lowerBound,
            glsEventGridProvider.GroupContext,
            TrackFilterID,
            retainedTransitionSources);
        for (var sourceIndex = 0; sourceIndex < retainedTransitionSources.Count; sourceIndex++)
        {
            var source = retainedTransitionSources[sourceIndex];
            if (!LoadedContainers.ContainsKey(source))
            {
                CreateContainerFromPool(source);
            }
        }
    }

    protected override bool ShouldRetainContainerOutsideBounds(BaseObject obj, float lowerBound, float upperBound) =>
        obj is BaseLightColorBase color
        && ReferenceEquals(color.EventBoxGroupData, glsEventGridProvider.GroupContext)
        && GLSEventCommon.TryGetColorRibbonBounds(color, displayedColorLightCount, out var start, out var end)
        && start <= upperBound && end >= lowerBound;

    protected override void DeleteObjectAt(
        int index,
        bool triggersAction,
        bool refreshesPool,
        string comment,
        bool inCollectionOfDeletes,
        bool deselect,
        bool triggerHandle)
    {
        var deletedObj = MapObjects[index];
        RecycleContainer(deletedObj);
        MapObjects.RemoveAt(index);
        if (deselect) SelectionController.Deselect(deletedObj, triggersAction);
        if (refreshesPool) RefreshPool();
        if (triggerHandle) HandleObjectDelete(deletedObj, inCollectionOfDeletes);
    }
}
