using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class SlidersContainer : BeatmapObjectContainerCollection
{
    [SerializeField] private GameObject sliderPrefab;
    [FormerlySerializedAs("sliderAppearanceSO")] [SerializeField] private SliderAppearanceSO sliderAppearanceSO;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private CountersPlusController countersPlus;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Slider;

    public override BeatmapObjectContainer CreateContainer()
    {
        return BeatmapSliderContainer.SpawnSlider(null, ref sliderPrefab);
    }
    internal override void SubscribeToCallbacks() 
    {
    }

    internal override void UnsubscribeToCallbacks()
    {
    }

    internal override void LateUpdate()
    {
        if (Settings.Instance.Load_MapV3) base.LateUpdate();
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
    private void OnPlayToggle(bool isPlaying)
    {
        if (!isPlaying) RefreshPool();
    }
    public void UpdateColor(Color red, Color blue) => sliderAppearanceSO.UpdateColor(red, blue);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        var slider = con as BeatmapSliderContainer;
        var sliderData = obj as BeatmapSlider;
        slider.RecomputePosition(sliderData);
        sliderAppearanceSO.SetSliderAppearance(slider);
        slider.Setup();
        var track = tracksManager.GetTrackAtTime(sliderData.B);
        track.AttachContainer(con);
    }
}
