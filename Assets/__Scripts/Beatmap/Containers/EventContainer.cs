using System;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Constants;
using Beatmap.Enums;
using Beatmap.Shared;
using TMPro;
using UnityEngine;

namespace Beatmap.Containers
{
    public class EventContainer : ObjectContainer
    {
        private static readonly int colorAId = Shader.PropertyToID("_ColorA");
        private static readonly int colorBId = Shader.PropertyToID("_ColorB");
        private static readonly int mainAlphaId = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSizeId = Shader.PropertyToID("_FadeSize");
        private static readonly int offsetId = Shader.PropertyToID("_Offset");

        [SerializeField] public VisualModelController VModelController;
        [SerializeField] private EventGridContainer eventGridContainer;
        [SerializeField] private EventAppearanceSO eventAppearance;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMeshPro valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] private CreateEventTypeLabels labels;
        [SerializeField] public TracksDefinitionSO TracksDefinition;

        public BaseEvent EventData;

        private void Awake() => defaultValueDisplayFontSize = valueDisplay.fontSize;

        private bool useBlockModel;
        private float defaultValueDisplayFontSize;
        public bool AlternateShader;
        private float oldAlpha = -1;

        public override BaseObject ObjectData
        {
            get => EventData;
            set => EventData = (BaseEvent)value;
        }

        public bool UseBlockModel
        {
            get => useBlockModel;
            set
            {
                useBlockModel = value;
                HandleModelChanged();
            }
        }

        protected override void RegisterCallback()
        {
            VisualSettings.OnBlockModelChanged += HandleModelChanged;
            VisualSettings.OnEventModelChanged += HandleModelChanged;
        }

        protected override void UnregisterCallback()
        {
            VisualSettings.OnBlockModelChanged -= HandleModelChanged;
            VisualSettings.OnEventModelChanged -= HandleModelChanged;
        }

        private void HandleModelChanged()
        {
            var vm = useBlockModel ? VisualSettings.GetEventBlockModel() : VisualSettings.GetEventModel();
            VModelController.Set(vm);
            
            if (AlternateShader == vm.AlternateShader) return;
            AlternateShader = vm.AlternateShader;
            // Placement previews are styled by EventPlacement and have no EventGridContainer for final-node boost/transition queries.
            if (EventData != null && eventGridContainer != null) RefreshAppearance();
        }

        public static EventContainer SpawnEvent(
            EventGridContainer eventsContainer,
            BaseEvent data,
            TracksDefinitionSO tracksDefinitionSo,
            ref GameObject prefab,
            ref CreateEventTypeLabels labels)
        {
            var container = Instantiate(prefab).GetComponent<EventContainer>();
            container.EventData = data;
            container.eventGridContainer = eventsContainer;
            container.TracksDefinition = tracksDefinitionSo;
            container.labels = labels;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        public override void UpdateGridPosition()
        {
            var gridPos = EventData.GetPosition(
                labels,
                eventGridContainer.PropagationEditing,
                eventGridContainer.EventTypeToPropagate);

            if (gridPos == null)
            {
                transform.localPosition = new Vector3(
                    0.5f,
                    // Keep hidden fallback nodes on the same grounded event baseline as visible Basic Events.
                    BeatmapConstant.EventNodeGroundedCenterY,
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
                SafeSetActive(false);
            }
            else
            {
                transform.localPosition = new Vector3(
                    gridPos.Value.x,
                    // Shift Basic Events down to the shared grounded node baseline before applying alpha-height compensation.
                    gridPos.Value.y - (0.5f - BeatmapConstant.EventNodeGroundedCenterY),
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
            }

            transform.localEulerAngles = Vector3.zero;
            if (EventData.CustomLightGradient != null && Settings.Instance.VisualizeChromaGradients)
                lightGradientController.UpdateDuration(EventData.CustomLightGradient.Duration);
            // Offset by exactly half the rendered-height delta so every alpha-scaled event shares the same bottom plane.
            if (Settings.Instance.VisualizeChromaAlpha)
            {
                transform.localPosition = new Vector3(
                    transform.localPosition.x,
                    transform.localPosition.y + ((GetHeight() - 1f) * (EventAppearanceSO.FinalNodeScale / 2f)),
                    transform.localPosition.z);
            }

            UpdateCollisionGroups();
        }

        public void ChangeColorA(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(colorAId, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeColorB(Color c, bool updateMaterials = true)
        {
            MpbController.Mpb.SetColor(colorBId, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeFadeSize(float size, bool updateMaterials = true)
        {
            MpbController.Mpb.SetFloat(fadeSizeId, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateAlpha(float alpha, bool updateMaterials = true)
        {
            var oldAlphaTemp = MpbController.Mpb.GetFloat(mainAlphaId);
            if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
            if (Mathf.Approximately(oldAlpha, alpha)) return;

            MpbController.Mpb.SetFloat(mainAlphaId, Mathf.Approximately(alpha, -1) ? oldAlpha : alpha);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateOffset(float offset)
        {
            MpbController.Mpb.SetFloat(offsetId, offset);
            UpdateMaterials();
        }

        public void UpdateScale(float scale) =>
            transform.localScale =
                new Vector3(
                    1,
                    IsRingRotationEvent || IsRingZoomEvent || IsLaserSpeedEvent
                        || Settings.Instance.VisualizeChromaAlpha
                        ? GetHeight()
                        : 1,
                    1) * scale;

        // Ring capabilities come from the active environment rather than conventional event-type numbers.
        private bool IsRingRotationEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.RingRotation);

        private bool IsRingZoomEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.RingZoom)
            || IsSmoothStepRingZoomEvent;

        // SmoothStepRingZoom only applies to The Second's legacy ring right now.
        private bool IsSmoothStepRingZoomEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components
                .HasFlag(BasicEventComponent.SmoothStepRingZoom);

        // Basic Event light-rotation consumers use speed as their primary visual magnitude.
        private bool IsLaserSpeedEvent =>
            TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.LightRotation);

        //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on
        private float GetHeight()
        {
            if (IsRingRotationEvent && EventData.CustomRingRotation.HasValue)
                return Mathf.Clamp(
                    Mathf.Abs(EventData.CustomRingRotation.Value) / RingEventHeightConstants.RingRotationHeightScaleDegrees,
                    1f / RingEventHeightConstants.RingRotationHeightScaleDegrees,
                    RingEventHeightConstants.RingRotationHeightMaxMultiplier);

            if (IsRingZoomEvent && (EventData.CustomStep.HasValue
                || IsSmoothStepRingZoomEvent))
            {
                // SmoothStepRingZoom only applies to The Second's ring and falls back to i when step is absent.
                var ringZoomStep = IsSmoothStepRingZoomEvent
                    ? EventData.CustomStep ?? EventData.Value
                    : EventData.CustomStep.Value;
                return Mathf.Clamp(
                    Mathf.Abs(ringZoomStep) / RingEventHeightConstants.RingZoomHeightScaleStep,
                    1f / RingEventHeightConstants.RingZoomHeightScaleStep,
                    RingEventHeightConstants.RingZoomHeightMaxMultiplier);
            }

            if (IsLaserSpeedEvent)
            {
                // Scale speed 0..40 across the minimum node height through 300%, clamping larger values visually.
                var speed = Mathf.Max(0f, EventData.CustomSpeed ?? EventData.Value);
                return Mathf.Clamp(speed / 40f * 3f, 0.1f, 3f);
            }

            // Non-light events should not have different heights
            if (TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights) return 1f;

            var height = EventData.FloatValue;
            if (EventData.CustomColor != null && Math.Abs(EventData.CustomColor.Value.a - 1) > 0.001)
                height *= EventData.CustomColor.Value.a;
            else if (EventData.CustomLightGradient != null
                && Math.Abs(EventData.CustomLightGradient.StartColor.a - 1) > 0.001)
                height *= EventData.CustomLightGradient.StartColor.a;

            // Clamped to avoid too small/too tall events
            return Mathf.Clamp(height, 0.1f, 1.5f);
        }

        public void UpdateGradientRendering(
            Color? startColor = null,
            Color? endColor = null,
            string easing = "easeLinear",
            BasicEventColorLerpType colorLerpType = BasicEventColorLerpType.RGB,
            float startBrightness = 1f,
            float endBrightness = 1f,
            bool allowNonLight = false,
            BaseEvent transitionTarget = null)
        {
            // Use dev's singular serialized track-definition field.
            if (!allowNonLight && TracksDefinition.GetBasicOrDefault(EventData.Type).Kind != BasicEventKind.Lights)
            {
                lightGradientController.SetVisible(false);
                return;
            }

            if (EventData.CustomLightGradient != null)
            {
                if (Settings.Instance.EmulateChromaLite && EventData.Value != (int)LightValue.Off)
                {
                    ChangeColorB(EventData.CustomLightGradient.StartColor);
                    ChangeColorA(EventData.CustomLightGradient.StartColor);
                }

                lightGradientController.SetVisible(true);
                var gradient = new ChromaLightGradient(
                    BasicEventColorLerp.ApplyBrightness(
                        EventData.CustomLightGradient.StartColor,
                        EventData.FloatValue),
                    BasicEventColorLerp.ApplyBrightness(
                        EventData.CustomLightGradient.EndColor,
                        EventData.FloatValue),
                    EventData.CustomLightGradient.Duration,
                    EventData.CustomLightGradient.EasingType);
                lightGradientController.UpdateGradientData(gradient, BasicEventColorLerpType.RGB);
                lightGradientController.UpdateDuration(EventData.CustomLightGradient.Duration);
            }
            else
            {
                if (startColor == null || endColor == null)
                {
                    lightGradientController.SetVisible(false);
                    return;
                }

                // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt uses the effective endpoint for ribbon length.
                var renderedTransitionTarget = transitionTarget ?? EventData.Next;
                var transition = new ChromaLightGradient(
                    BasicEventColorLerp.ApplyBrightness(startColor.Value, startBrightness),
                    BasicEventColorLerp.ApplyBrightness(endColor.Value, endBrightness),
                    renderedTransitionTarget?.SongBpmTime - EventData.SongBpmTime ?? 0f,
                    easing);
                lightGradientController.SetVisible(true);
                // Basic Event ribbons preserve both legacy normalized HSV and the opt-in true angular HSV path.
                lightGradientController.UpdateGradientData(transition, colorLerpType);
                lightGradientController.UpdateDuration(transition.Duration);
            }
        }

        public void UpdateTextDisplay(bool visible, string text = "")
        {
            if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
            var isRotationEvent = IsRingRotationEvent
                || TracksDefinition.GetBasicOrDefault(EventData.Type).Components.HasFlag(BasicEventComponent.LightRotation);

            var lineCount = text.Split('\n').Length;
            var scaleFactor = 1f;
            if (isRotationEvent && lineCount >= 2)
            {
                scaleFactor = lineCount switch
                {
                    2 => 0.5f,
                    3 => 0.4f,
                    _ => 0.3f
                };
            }
            else if (text.Contains('\n') || isRotationEvent)
            {
                scaleFactor = 0.5f;
            }

            // TheSecondRingZoomFontShrinksForLongRenderedStep scales single-line zoom labels from their rendered length so signed thousandths remain inside the node.
            if (lineCount == 1 && IsRingZoomEvent && text.Length > 3)
            {
                scaleFactor *= 3f / text.Length;
            }

            // Give single-line decimal speeds extra width without compounding the multiline label reduction.
            if (lineCount == 1 && IsLaserSpeedEvent && EventData.CustomSpeed.HasValue
                && !Mathf.Approximately(EventData.CustomSpeed.Value, Mathf.Round(EventData.CustomSpeed.Value)))
                scaleFactor *= 0.8f;

            valueDisplay.fontSize = defaultValueDisplayFontSize * scaleFactor;
            valueDisplay.text = text;
        }

        public void RefreshAppearance()
        {
            // LightIdTransitionRibbonEndsAtAllLightsTransitionInterrupt resolves the finalized container's grid endpoint.
            eventAppearance.SetAppearance(
                this,
                true,
                eventGridContainer.IsBoostAt(EventData.JsonTime),
                eventGridContainer.GetEffectiveNextLightEvent(EventData));
        }

        // Both LightIdTransitionRibbon interruption regressions require one endpoint for appearance and hover editing.
        public BaseEvent GetEffectiveNextLightEvent() => eventGridContainer.GetEffectiveNextLightEvent(EventData);
    }
}
