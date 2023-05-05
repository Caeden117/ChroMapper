using System;
using System.Collections.Generic;
using Beatmap.Appearances;
using Beatmap.Base;
using Beatmap.Enums;
using Beatmap.Shared;
using UnityEngine;

namespace Beatmap.Containers
{
    public class EventContainer : ObjectContainer
    {
        /// <summary>
        ///     Different modes to sort events in the editor.
        /// </summary>
        public static int ModifyTypeMode = 0;

        private static readonly int colorBase = Shader.PropertyToID("_ColorBase");
        private static readonly int colorTint = Shader.PropertyToID("_ColorTint");
        private static readonly int position = Shader.PropertyToID("_Position");
        private static readonly int mainAlpha = Shader.PropertyToID("_MainAlpha");
        private static readonly int fadeSize = Shader.PropertyToID("_FadeSize");
        private static readonly int spotlightSize = Shader.PropertyToID("_SpotlightSize");
        [SerializeField] private EventGridContainer EventGridContainer;
        [SerializeField] private EventAppearanceSO eventAppearance;
        [SerializeField] private List<Renderer> eventRenderer;
        [SerializeField] private TracksManager tracksManager;
        [SerializeField] private TextMesh valueDisplay;
        [SerializeField] private LightGradientController lightGradientController;
        [SerializeField] private GameObject[] eventModels;
        [SerializeField] private CreateEventTypeLabels labels;

        [SerializeField] public BaseEvent EventData;

        // This needs to be an int for the below properties
        private int eventModel;

        private float oldAlpha = -1;

        public EventModelType EventModel
        {
            get => (EventModelType)eventModel;
            set
            {
                for (var i = 0; i < eventModels.Length; i++) eventModels[i].SetActive(i == (int)value);
                eventModel = (int)value;
            }
        }

        public Vector3 FlashShaderOffset =>
            eventModels[eventModel].GetComponent<MaterialParameters>().FlashShaderOffset;

        public Vector3 FadeShaderOffset => eventModels[eventModel].GetComponent<MaterialParameters>().FadeShaderOffset;
        public float DefaultFadeSize => eventModels[eventModel].GetComponent<MaterialParameters>().DefaultFadeSize;

        public float BoostEventFadeSize =>
            eventModels[eventModel].GetComponent<MaterialParameters>().BoostEventFadeSize;

        public override BaseObject ObjectData
        {
            get => EventData;
            set => EventData = (BaseEvent)value;
        }

        public static EventContainer SpawnEvent(EventGridContainer eventsContainer, BaseEvent data,
            ref GameObject prefab,
            ref EventAppearanceSO eventAppearanceSO, ref CreateEventTypeLabels labels)
        {
            var container = Instantiate(prefab).GetComponent<EventContainer>();
            container.EventData = data;
            container.EventGridContainer = eventsContainer;
            container.eventAppearance = eventAppearanceSO;
            container.labels = labels;
            container.transform.localEulerAngles = Vector3.zero;
            return container;
        }

        // This way we won't apply MaterialPropertyBlock to valueDisplay which breaks dynamic batching
        public override void Setup()
        {
            if (MaterialPropertyBlock == null)
            {
                MaterialPropertyBlock = new MaterialPropertyBlock();
                modelRenderers.AddRange(eventRenderer);
            }
        }

        public override void UpdateGridPosition()
        {
            var gridPos = EventData.GetPosition(labels, EventGridContainer.PropagationEditing,
                EventGridContainer.EventTypeToPropagate);

            if (gridPos == null)
            {
                transform.localPosition = new Vector3(
                    -0.5f,
                    0.5f,
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
                SafeSetActive(false);
            }
            else
            {
                transform.localPosition = new Vector3(
                    gridPos.Value.x,
                    gridPos.Value.y,
                    EventData.SongBpmTime * EditorScaleController.EditorScale
                );
            }

            transform.localEulerAngles = Vector3.zero;
            if (EventData.CustomLightGradient != null && Settings.Instance.VisualizeChromaGradients)
                lightGradientController.UpdateDuration(EventData.CustomLightGradient.Duration);
            //Move event up or down enough to give a constant distance from the bottom of the event, taking the y alpha scale into account
            if (Settings.Instance.VisualizeChromaAlpha)
                transform.localPosition = new Vector3(transform.localPosition.x,
                    transform.localPosition.y + ((GetHeight() - 1f) / 2.775f), transform.localPosition.z);
            UpdateCollisionGroups();
        }

        public void ChangeColor(Color c, bool updateMaterials = true)
        {
            MaterialPropertyBlock.SetColor(colorTint, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeBaseColor(Color c, bool updateMaterials = true)
        {
            MaterialPropertyBlock.SetColor(colorBase, c);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeFadeSize(float size, bool updateMaterials = true)
        {
            MaterialPropertyBlock.SetFloat(fadeSize, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void ChangeSpotlightSize(float size, bool updateMaterials = true)
        {
            MaterialPropertyBlock.SetFloat(spotlightSize, size);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateOffset(Vector3 offset, bool updateMaterials = true)
        {
            MaterialPropertyBlock.SetVector(position, offset);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateAlpha(float alpha, bool updateMaterials = true)
        {
            var oldAlphaTemp = MaterialPropertyBlock.GetFloat(mainAlpha);
            if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
            if (oldAlpha == alpha) return;

            MaterialPropertyBlock.SetFloat(mainAlpha, alpha == -1 ? oldAlpha : alpha);
            if (updateMaterials) UpdateMaterials();
        }

        public void UpdateScale(float scale) => transform.localScale =
            new Vector3(1, Settings.Instance.VisualizeChromaAlpha ? GetHeight() : 1, 1) * scale;

        //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on
        private float GetHeight()
        {
            var height = EventData.FloatValue;
            if (EventData.CustomData != null && EventData.CustomColor != null &&
                Math.Abs(EventData.CustomColor.Value.a - 1) > 0.001)
                height *= EventData.CustomColor.Value
                    .a; //The alpha of the event, clamped to avoid too small/too tall events
            else if (EventData.CustomLightGradient != null && EventData.CustomData != null &&
                     Math.Abs(EventData.CustomLightGradient.StartColor.a - 1) > 0.001)
                height *= EventData.CustomData["_lightGradient"]["_startColor"][3];
            return Mathf.Clamp(height, 0.1f, 1.5f);
        }

        public void UpdateGradientRendering(Color? startColor = null, Color? endColor = null, string easing = "easeLinear")
        {
            if (!EventData.IsLightEvent(BeatSaberSongContainer.Instance.Song.EnvironmentName))
            {
                lightGradientController.SetVisible(false);
                return;
            }

            if (EventData.CustomLightGradient != null)
            {
                if (Settings.Instance.EmulateChromaLite && EventData.Value != (int)LightValue.Off)
                {
                    ChangeColor(EventData.CustomLightGradient.StartColor);
                    ChangeBaseColor(EventData.CustomLightGradient.StartColor);
                }

                lightGradientController.SetVisible(true);
                lightGradientController.UpdateGradientData(EventData.CustomLightGradient);
            }
            else
            {
                if (startColor == null || endColor == null)
                {
                    lightGradientController.SetVisible(false);
                    return;
                }

                var transition = new ChromaLightGradient(startColor.Value, endColor.Value, EventData.Next.SongBpmTime - EventData.SongBpmTime, easing);
                lightGradientController.SetVisible(true);
                lightGradientController.UpdateGradientData(transition);
                lightGradientController.UpdateDuration(transition.Duration);
            }
        }

        public void UpdateTextDisplay(bool visible, string text = "")
        {
            if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
            valueDisplay.text = text;
        }

        public void UpdateTextColor(Color c) => valueDisplay.color = c;

        public void RefreshAppearance() => eventAppearance.SetEventAppearance(this);
    }
}
