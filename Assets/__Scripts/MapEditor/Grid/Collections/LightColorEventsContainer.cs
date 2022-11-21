using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class LightColorEventsContainer : LightEventsContainerCollectionBase<
    BeatmapLightColorEvent,
    BeatmapLightColorEventBox,
    BeatmapLightColorEventData,
    BeatmapLightColorEventContainer,
    LightColorEventsContainer,
    LightingEvent
    >
{

    [SerializeField] private LightColorEventPlacement lightColorEventPlacement;
    [SerializeField] private GameObject label;
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;
    public LightColorEventCallbackController RealSpawnCallbackController;
    public LightColorEventCallbackController RealDespawnCallbackController;

    [Tooltip("if this environment is not using v3 light system, disable all these objects")]
    [SerializeField] private GameObject[] disableGameObjects;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightColorEvent;


    protected class LightStaticGraphEnumerator : StaticGraphEnumerator
    {
        private float deltaAlpha;
        public override IEnumerable<int> AdditonalField() => new int[] { 0 };
        public override bool AdditonalFieldMatched(int additional, BeatmapLightColorEventBox LightEventBox) => true;
        public override void DeltaScaleByFilterLimit(
            IEnumerable<LightingEvent> all, IEnumerable<IEnumerable<LightingEvent>> filtered, BeatmapLightEventFilter filter, ref float deltaTime)
            => BeatmapLightEventFilter.DeltaScaleByFilterLimit(all, filtered, filter, ref deltaTime, ref deltaAlpha);
        public override void InitDelta(BeatmapLightColorEventBox lightEventBox, IEnumerable<IEnumerable<LightingEvent>> filteredLightChunks)
        {
            deltaAlpha = lightEventBox.BrightnessDistribution;
            if (lightEventBox.BrightnessDistributionType == 1) deltaAlpha /= BeatmapLightEventFilter.Intervals(filteredLightChunks);
        }
        public override BeatmapLightColorEventData InitValue(BeatmapLightColorEventData lightEventData)
        {
            EventData = BeatmapObject.GenerateCopy(lightEventData);
            return BeatmapObject.GenerateCopy(EventData);
        }
        public override BeatmapLightColorEventData Next()
        {
            EventData.Brightness += deltaAlpha;
            return BeatmapObject.GenerateCopy(EventData);
        }
    }
    private LightStaticGraphEnumerator lightGraphEnumerator = new LightStaticGraphEnumerator();
    protected override StaticGraphEnumerator GraphEnumerator => lightGraphEnumerator;


    internal override void SubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
        uiGenerator.OnToggleUIPanelSwitch += FlipAllContainers;
        RealSpawnCallbackController.ObjectPassedThreshold += SpawnCallback;
        RealSpawnCallbackController.RecursiveObjectCheckFinished += RecursiveCheckFinished;
        RealDespawnCallbackController.ObjectPassedThreshold += DespawnCallback;
    }

    private void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData)) RecycleContainer(objectData);
    }
    private void RecursiveCheckFinished(bool natural, int lastPassedIndex) => RefreshPool();
    private void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    internal override void UnsubscribeToCallbacks()
    {
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
        uiGenerator.OnToggleUIPanelSwitch -= FlipAllContainers;
        RealSpawnCallbackController.ObjectPassedThreshold -= SpawnCallback;
        RealSpawnCallbackController.RecursiveObjectCheckFinished -= RecursiveCheckFinished;
        RealDespawnCallbackController.ObjectPassedThreshold -= DespawnCallback;
    }

    protected override void Start()
    {
        base.Start();
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }
    protected override void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
        base.OnDestroy();
    }
    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        StartCoroutine(AfterPlatformLoaded());
    }
    
    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        if (platformDescriptor == null)
        {
            foreach (var obj in disableGameObjects) obj.SetActive(false);
        }
        else
        {
            UpdateGrids();
        }
    }

    public void UpdateGrids()
    {
        lightColorEventPlacement.SetGridSize(platformDescriptor.LightsManagersV3.Length);
        for (int i = 0; i < platformDescriptor.LightsManagersV3.Length; ++i)
        {
            var instantiate = Instantiate(label, label.transform.parent);
            instantiate.SetActive(true);
            instantiate.transform.localPosition = new Vector3(i, 0, 0);
            var textMesh = instantiate.GetComponentInChildren<TextMeshProUGUI>();
            textMesh.text = platformDescriptor.LightsManagersV3[i].name;
        }
    }

    public void OnPlayToggle(bool isPlaying)
    {
        if (isPlaying)
        {
            LinkAllLightEventDatas();
        }
    }
    private void FlipAllContainers(LightV3GeneratorAppearance.LightV3UIPanel currentPanel)
    {
        containersUP = currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel;
        RefreshPool(true);
    }

    protected override List<LightingEvent> GetAllLights(int laneIdx) => platformDescriptor.LightsManagersV3[laneIdx].ControllingLights;
}
