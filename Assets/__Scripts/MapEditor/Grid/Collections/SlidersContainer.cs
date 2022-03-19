using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Note that <see cref="SlidersContainer"></see> uses `UseChunkLoadingWhenPlaying`. Therefore slider doesn't fade after passing through.
/// </summary>
public class SlidersContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject sliderPrefab;
    [FormerlySerializedAs("sliderAppearanceSO")] [SerializeField] private SliderAppearanceSO sliderAppearanceSO;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    private bool isPlaying;

    private Queue<BeatmapSliderContainer> queuedUpdatingSliders = new Queue<BeatmapSliderContainer>();
    private const int maxRecomputePerFrame = 2;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Slider;

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapSliderContainer.SpawnSlider(null, ref sliderPrefab);
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

    private void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }
    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();
    private void DespawnCallback(bool initial, int index, BeatmapObject objectData)
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
        if (isPlaying) RefreshPool(true);
        foreach (BeatmapSliderContainer obj in LoadedContainers.Values)
        {
            obj.SetIndicatorBlocksActive(!this.isPlaying);
        }
    }

    public void UpdateColor(Color red, Color blue) => sliderAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var slider = con as BeatmapSliderContainer;
        var sliderData = obj as BeatmapSlider;
        slider.NotifySplineChanged(sliderData);
        sliderAppearanceSO.SetSliderAppearance(slider);
        slider.Setup();
        slider.SetIndicatorBlocksActive(false);
        var track = tracksManager.GetTrackAtTime(sliderData.Time);
        track.AttachContainer(con);
    }

    /// <summary>
    /// Push a container into waiting queue to recompute.
    /// </summary>
    /// <param name="container"></param>
    public void RequestForSplineRecompute(BeatmapSliderContainer container)
    {
        queuedUpdatingSliders.Enqueue(container);
    }

    /// <summary>
    /// Only compute several splines per frame, avoid burst stuck.   
    /// </summary>
    /// <returns></returns>
    private void ScheduleRecomputePosition()
    {
        for (int i = 0; i < maxRecomputePerFrame && queuedUpdatingSliders.Count != 0; ++i)
        {
            var container = queuedUpdatingSliders.Dequeue();
            container.RecomputePosition();
            container.SetIndicatorBlocksActive(!isPlaying);
        }
    }
}
