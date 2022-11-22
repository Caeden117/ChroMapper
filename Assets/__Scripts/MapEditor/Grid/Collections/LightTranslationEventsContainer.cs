using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightTranslationEventsContainer : LightEventsContainerCollectionBase<
    BeatmapLightTranslationEvent,
    BeatmapLightTranslationEventBox,
    BeatmapLightTranslationEventData,
    BeatmapLightTranslationEventContainer,
    LightTranslationEventsContainer,
    TranslationEvent
    >
{
    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.LightTranslationEvent;

    protected override LightV3GeneratorAppearance.LightV3UIPanel ThisUIPannel => LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel;

    protected class TranslationStaticGraphEnumerator : StaticGraphEnumerator
    {
        private float deltaOffset;
        public override IEnumerable<int> AdditonalField() => new int[] { 0, 1, 2 };
        public override bool AdditonalFieldMatched(int additional, BeatmapLightTranslationEventBox LightEventBox)
            => additional == LightEventBox.Axis;
        public override void DeltaScaleByFilterLimit(
            IEnumerable<TranslationEvent> all, IEnumerable<IEnumerable<TranslationEvent>> filtered, BeatmapLightEventFilter filter, ref float deltaTime)
            => BeatmapLightEventFilter.DeltaScaleByFilterLimit(all, filtered, filter, ref deltaTime, ref deltaOffset);
        public override BeatmapLightTranslationEventData Next()
        {
            EventData.TranslateValue += (EventBox.TranslationAffectFirst == 0 && EventDataIdx == 0) ? 0 : deltaOffset;
            return BeatmapObject.GenerateCopy(EventData);
        }
        protected override void InitDeltaImpl(BeatmapLightTranslationEventBox lightEventBox, IEnumerable<IEnumerable<TranslationEvent>> filteredLightChunks)
        {
            deltaOffset = lightEventBox.TranslationDistribution;
            if (lightEventBox.Filter.FilterType == 1) deltaOffset /= BeatmapLightEventFilter.Intervals(filteredLightChunks);
            if (lightEventBox.Flip == 1) deltaOffset = -deltaOffset;
        }
        protected override void InitValueImpl(BeatmapLightTranslationEventData lightEventData, int eventDataIdx)
        {
            if (EventBox.Flip == 1) EventData.TranslateValue = -EventData.TranslateValue;
        }
    }
    private TranslationStaticGraphEnumerator translationGraphEnumerator = new TranslationStaticGraphEnumerator();

    protected override StaticGraphEnumerator GraphEnumerator => translationGraphEnumerator;

    protected override List<TranslationEvent> GetAllLights(int laneIdx) => platformDescriptor.LightsManagersV3[laneIdx].ControllingTranslations;
}
