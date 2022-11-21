using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightRotationEventsContainer : LightEventsContainerCollectionBase<
    BeatmapLightRotationEvent,
    BeatmapLightRotationEventBox,
    BeatmapLightRotationEventData,
    BeatmapLightRotationEventContainer,
    LightRotationEventsContainer,
    RotatingEvent
    >
{
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;
    public LightRotationEventCallbackController RealSpawnCallbackController;
    public LightRotationEventCallbackController RealDespawnCallbackController;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightRotationEvent;

    protected class RotationStaticGraphEnumerator : StaticGraphEnumerator
    {
        private float deltaDegree;
        public override IEnumerable<int> AdditonalField() => new int[] { 0, 1, 2 };
        public override bool AdditonalFieldMatched(int additional, BeatmapLightRotationEventBox LightEventBox) => additional == LightEventBox.Axis;

        public override void DeltaScaleByFilterLimit(
            IEnumerable<RotatingEvent> all, IEnumerable<IEnumerable<RotatingEvent>> filtered, BeatmapLightEventFilter filter, ref float deltaTime)
        {
            BeatmapLightEventFilter.DeltaScaleByFilterLimit(all, filtered, filter, ref deltaTime, ref deltaDegree);
        }
        protected override void InitDeltaImpl(BeatmapLightRotationEventBox lightEventBox, IEnumerable<IEnumerable<RotatingEvent>> filteredLightChunks)
        {
            deltaDegree = lightEventBox.RotationDistribution;
            if (EventBox.ReverseRotation == 1) deltaDegree = -deltaDegree;
            if (lightEventBox.RotationDistributionType == 1) deltaDegree /= BeatmapLightEventFilter.Intervals(filteredLightChunks);

        }
        protected override void InitValueImpl(BeatmapLightRotationEventData lightEventData, int eventDataIdx)
        {
            if (EventBox.ReverseRotation == 1) EventData.RotationValue = -EventData.RotationValue;
        }
        public override BeatmapLightRotationEventData Next()
        {
            EventData.RotationValue += (EventDataIdx == 0 && EventBox.RotationAffectFirst == 0 ) ? 0 : deltaDegree;
            return BeatmapObject.GenerateCopy(EventData);
        }
    }
    private RotationStaticGraphEnumerator rotationGraphEnumerator = new RotationStaticGraphEnumerator();

    protected override StaticGraphEnumerator GraphEnumerator => rotationGraphEnumerator;


    internal override void SubscribeToCallbacks()
    {
        base.SubscribeToCallbacks();
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
        base.SubscribeToCallbacks();
        uiGenerator.OnToggleUIPanelSwitch -= FlipAllContainers;
        RealSpawnCallbackController.ObjectPassedThreshold -= SpawnCallback;
        RealSpawnCallbackController.RecursiveObjectCheckFinished -= RecursiveCheckFinished;
        RealDespawnCallbackController.ObjectPassedThreshold -= DespawnCallback;
    }



    private void FlipAllContainers(LightV3GeneratorAppearance.LightV3UIPanel currentPanel)
    {
        containersUP = currentPanel == LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel;
        RefreshPool(true);
    }

    protected override List<RotatingEvent> GetAllLights(int laneIdx) => platformDescriptor.LightsManagersV3[laneIdx].ControllingRotations;
}
