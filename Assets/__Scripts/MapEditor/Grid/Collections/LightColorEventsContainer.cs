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
        protected override void InitDeltaImpl(BeatmapLightColorEventBox lightEventBox, IEnumerable<IEnumerable<LightingEvent>> filteredLightChunks)
        {
            deltaAlpha = lightEventBox.BrightnessDistribution;
            if (lightEventBox.BrightnessDistributionType == 1) deltaAlpha /= BeatmapLightEventFilter.Intervals(filteredLightChunks);
        }
        protected override void InitValueImpl(BeatmapLightColorEventData lightEventData, int evetnDataIdx)
        {
        }
        public override BeatmapLightColorEventData Next()
        {
            EventData.Brightness += (EventDataIdx == 0 && EventBox.BrightnessAffectFirst == 0) ? 0 : deltaAlpha;
            return BeatmapObject.GenerateCopy(EventData);
        }
    }
    private LightStaticGraphEnumerator lightGraphEnumerator = new LightStaticGraphEnumerator();
    protected override StaticGraphEnumerator GraphEnumerator => lightGraphEnumerator;

    protected override LightV3GeneratorAppearance.LightV3UIPanel ThisUIPannel => LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel;

    internal override void SubscribeToCallbacks()
    {
        base.SubscribeToCallbacks();
        RealSpawnCallbackController.ObjectPassedThreshold += SpawnCallback;
        RealSpawnCallbackController.RecursiveObjectCheckFinished += RecursiveCheckFinished;
        RealDespawnCallbackController.ObjectPassedThreshold += DespawnCallback;
    }



    internal override void UnsubscribeToCallbacks()
    {
        base.SubscribeToCallbacks();
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

    private string ConstructLightEventString(LightsManagerV3 lightsManager)
    {
        List<string> events = new List<string>();
        if (lightsManager.HasColorEvent) events.Add("C");
        if (lightsManager.HasRotationEvent)
        {
            string tmp = "R";
            if (lightsManager.XRotatable) tmp += "x";
            if (lightsManager.YRotatable) tmp += "y";
            if (lightsManager.ZRotatable) tmp += "z";
            events.Add(tmp);
        }
        if (lightsManager.HasTranslationEvent)
        {
            string tmp = "T";
            if (lightsManager.TranslationConfig.XTranslatable) tmp += "x";
            if (lightsManager.TranslationConfig.YTranslatable) tmp += "y";
            if (lightsManager.TranslationConfig.ZTranslatable) tmp += "z";
            events.Add(tmp);
        }
        return "{" + string.Join(", ", events) + "}";
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
            var name = platformDescriptor.LightsManagersV3[i].name;
            var eventString = ConstructLightEventString(platformDescriptor.LightsManagersV3[i]);
            textMesh.text = name + "\t\t" + eventString;
        }
    }


    protected override List<LightingEvent> GetAllLights(int laneIdx) => platformDescriptor.LightsManagersV3[laneIdx].ControllingLights;
}
