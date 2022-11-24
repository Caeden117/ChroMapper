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
    public LightRotationEventCallbackController RealSpawnCallbackController;
    public LightRotationEventCallbackController RealDespawnCallbackController;
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightRotationEvent;

    protected class RotationStaticGraphEnumerator : StaticGraphEnumerator
    {
        public override IEnumerable<int> AdditonalField() => new int[] { 0, 1, 2 };
        public override bool AdditonalFieldMatched(int additional, BeatmapLightRotationEventBox LightEventBox) => additional == LightEventBox.Axis;

        public override void DeltaScaleByFilterLimit(
            IEnumerable<RotatingEvent> all, IEnumerable<IEnumerable<RotatingEvent>> filtered, BeatmapLightEventFilter filter, ref float deltaTime)
        {
            BeatmapLightEventFilter.DeltaScaleByFilterLimit(all, filtered, filter, ref deltaTime, ref DistributionEnumerator.Value);
        }
        protected override void InitDeltaImpl(BeatmapLightRotationEventBox lightEventBox, IEnumerable<IEnumerable<RotatingEvent>> filteredLightChunks)
        {
             DistributionEnumerator.Reset(filteredLightChunks, lightEventBox.RotationDistributionType,
                lightEventBox.RotationDistribution * (EventBox.ReverseRotation == 1 ? -1 : 1), lightEventBox.DataDistributionEaseType);
        }
        protected override void InitValueImpl(BeatmapLightRotationEventData lightEventData, int eventDataIdx)
        {
            if (EventBox.ReverseRotation == 1) EventData.RotationValue = -EventData.RotationValue;
        }
        protected override BeatmapLightRotationEventData NextImpl()
        {
            var ret = BeatmapObject.GenerateCopy(EventData);
            ret.RotationValue += (EventDataIdx == 0 && EventBox.RotationAffectFirst == 0 ) ? 0 : DistributionEnumerator.Next();
            return ret;
        }
    }
    private RotationStaticGraphEnumerator rotationGraphEnumerator = new RotationStaticGraphEnumerator();

    protected override StaticGraphEnumerator GraphEnumerator => rotationGraphEnumerator;

    protected override LightV3GeneratorAppearance.LightV3UIPanel ThisUIPannel => LightV3GeneratorAppearance.LightV3UIPanel.LightRotationPanel;

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


    protected override List<RotatingEvent> GetAllLights(int laneIdx) => platformDescriptor.LightsManagersV3[laneIdx].ControllingRotations;
}
