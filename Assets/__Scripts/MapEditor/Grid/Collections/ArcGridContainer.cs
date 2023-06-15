using System;
using System.Collections;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Note that <see cref="ArcGridContainer"></see> uses `UseChunkLoadingWhenPlaying`. Therefore arc doesn't fade after passing through.
/// </summary>
public class ArcGridContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject arcPrefab;
    [FormerlySerializedAs("arcAppearanceSO")][SerializeField] private ArcAppearanceSO arcAppearanceSO;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    private bool isPlaying;

    private Queue<ArcContainer> queuedUpdatingArcs = new Queue<ArcContainer>();
    private const int maxRecomputePerFrame = 2;
    public override ObjectType ContainerType => ObjectType.Arc;

    public override ObjectContainer CreateContainer()
    {
        return ArcContainer.SpawnArc(null, ref arcPrefab);
    }
    internal override void SubscribeToCallbacks()
    {
        if (!Settings.Instance.Load_MapV3) return;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        if (!Settings.Instance.Load_MapV3) return;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    internal override void LateUpdate()
    {
        if (Settings.Instance.Load_MapV3)
        {
            base.LateUpdate();
            ScheduleRecomputePosition();
        }
    }

    private void SpawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }
    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();
    private void DespawnCallback(bool initial, int index, BaseObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }

    /// <summary>
    /// When playing, disable all indicator blocks
    /// </summary>
    /// <param name="isPlaying"></param>
    private void OnPlayToggle(bool isPlaying)
    {
        this.isPlaying = isPlaying;
        // if (isPlaying) RefreshPool(true); // I dont know if removing this line affects anything, we'll see
        foreach (ArcContainer obj in LoadedContainers.Values)
        {
            obj.SetIndicatorBlocksActive(!this.isPlaying);
        }
    }

    public void UpdateColor(Color red, Color blue) => arcAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(ObjectContainer con, BaseObject obj)
    {
        var arc = con as ArcContainer;
        var arcData = obj as BaseArc;
        arc.NotifySplineChanged(arcData);
        arcAppearanceSO.SetArcAppearance(arc);
        arc.Setup();
        arc.SetIndicatorBlocksActive(false);
        var track = tracksManager.GetTrackAtTime(arcData.SongBpmTime);
        track.AttachContainer(con);
    }

    /// <summary>
    /// Push a container into waiting queue to recompute.
    /// </summary>
    /// <param name="container"></param>
    public void RequestForSplineRecompute(ArcContainer container)
    {
        queuedUpdatingArcs.Enqueue(container);
    }

    /// <summary>
    /// Only compute several splines per frame, avoid burst stuck.
    /// </summary>
    /// <returns></returns>
    private void ScheduleRecomputePosition()
    {
        for (int i = 0; i < maxRecomputePerFrame && queuedUpdatingArcs.Count != 0; ++i)
        {
            var container = queuedUpdatingArcs.Dequeue();
            container.RecomputePosition();
            container.SetIndicatorBlocksActive(!isPlaying);
        }
    }
}
