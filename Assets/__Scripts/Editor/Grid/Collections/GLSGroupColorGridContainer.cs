using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;

public class GLSGroupColorGridContainer : GLSGroupGridContainer<BaseLightColorEventBoxGroup>
{
    // Reuse indexed source groups because color pool refreshes run for every viewport movement.
    private readonly HashSet<BaseLightColorEventBoxGroup> retainedTransitionGroups = new();
    private readonly HashSet<BaseLightColorBase> changedColorNodes = new();
    private readonly Dictionary<BaseEventBoxGroup, HashSet<float>> changedColorAggregates = new();
    private bool colorTransitionRefreshPending;

    public override ObjectType ContainerType => ObjectType.GLSColor;

    // Outer previews must not unload color nodes whose outgoing or incoming ribbon crosses the edge.
    protected override void PrepareRetainedPreviewEvents(float lowerBound)
    {
        RetainedPreviewEvents.Clear();
        GLSEventCommon.GetColorTransitionSourcesAt(
            lowerBound,
            TrackFilterID,
            glsGroupGridProvider.ActiveGlsTrackIds,
            RetainedPreviewEvents);
    }

    internal override void SubscribeToCallbacks()
    {
        base.SubscribeToCallbacks();
        Settings.NotifyBySettingName(
            nameof(Settings.VisualizeGLSLightTransitions),
            RefreshLoadedTransitionRibbons);
    }

    internal override void UnsubscribeToCallbacks()
    {
        Settings.StopNotifyingBySettingName(
            nameof(Settings.VisualizeGLSLightTransitions),
            RefreshLoadedTransitionRibbons);
        base.UnsubscribeToCallbacks();
    }

    private void RefreshLoadedTransitionRibbons(object _) => RefreshLoadedTransitionRibbons();

    protected override void HandleObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectSpawned(obj, inCollection);
        // A newly inserted transition target changes the forward ribbon owned by an already-loaded prior node.
        GLSEventCommon.AddColorTransitionGroup((BaseLightColorEventBoxGroup)obj);
        colorTransitionRefreshPending = true;
    }

    protected override void HandleObjectDelete(BaseObject obj, bool inCollection = false)
    {
        base.HandleObjectDelete(obj, inCollection);
        // Deleting this group also invalidates source ribbons in other groups. Flush those changes before the batch's pool refresh returns.
        GLSEventCommon.RemoveColorTransitionGroup((BaseLightColorEventBoxGroup)obj);
        colorTransitionRefreshPending = true;
    }

    public override void DoPostObjectsSpawnedWorkflow()
    {
        base.DoPostObjectsSpawnedWorkflow();
        // Consolidate ribbon refresh after bulk color-group insertion.
        FlushColorTransitionRefresh();
    }

    public override void DoPostObjectsDeleteWorkflow()
    {
        base.DoPostObjectsDeleteWorkflow();
        // Consolidate ribbon refresh after bulk color-group deletion.
        FlushColorTransitionRefresh();
    }

    public override void RefreshPool(float lowerBound, float upperBound, bool forceRefresh = false)
    {
        // Query only transition intervals crossing the viewport boundary before parent pooling recycles their sources.
        retainedTransitionGroups.Clear();
        GLSEventCommon.GetColorTransitionSourceGroupsAt(
            lowerBound,
            TrackFilterID,
            glsGroupGridProvider.ActiveGlsTrackIds,
            retainedTransitionGroups);

        base.RefreshPool(lowerBound, upperBound, forceRefresh);

        // Recreate a recycled parent so its represented source ghost keeps drawing the ribbon.
        foreach (var group in retainedTransitionGroups)
        {
            if (IsGroupOnActivePage(group.ID) && !LoadedContainers.ContainsKey(group))
            {
                CreateContainerFromPool(group);
                ConfigureLoadedGroup((GLSGroupContainer)LoadedContainers[group]);
            }
        }

        FlushColorTransitionRefresh();
    }

    protected override bool ShouldRetainContainerOutsideBounds(BaseObject obj, float lowerBound, float upperBound) =>
        base.ShouldRetainContainerOutsideBounds(obj, lowerBound, upperBound)
        || (obj is BaseLightColorEventBoxGroup group
            && IsGroupOnActivePage(group.ID)
            && retainedTransitionGroups.Contains(group));

    // Mutations arriving outside a spawn/delete batch (e.g. RestoreRejectedDrag) request and flush here.
    public void RequestColorTransitionRefresh()
    {
        colorTransitionRefreshPending = true;
        FlushColorTransitionRefresh();
    }

    // Apply pending edits after the batch so remove/add replacements refresh their ribbons together.
    private void FlushColorTransitionRefresh()
    {
        if (!colorTransitionRefreshPending)
            return;

        colorTransitionRefreshPending = false;
        changedColorNodes.Clear();
        changedColorAggregates.Clear();
        if (!GLSEventCommon.TryCollectChangedColorTransitions(changedColorNodes, changedColorAggregates))
        {
            RefreshLoadedTransitionRibbons();
            return;
        }

        foreach (var container in LoadedContainers.Values)
        {
            var glsGroupContainer = container as GLSGroupContainer;
            if (glsGroupContainer != null)
                glsGroupContainer.RefreshTransitionRibbons(changedColorNodes, changedColorAggregates);
        }
    }

    private void RefreshLoadedTransitionRibbons()
    {
        foreach (var container in LoadedContainers.Values)
        {
            // Unity-owned GLS containers need explicit null checks before refreshing their ribbon ghosts.
            var glsGroupContainer = container as GLSGroupContainer;
            if (glsGroupContainer != null)
                glsGroupContainer.RefreshTransitionRibbons();
        }
    }
}
