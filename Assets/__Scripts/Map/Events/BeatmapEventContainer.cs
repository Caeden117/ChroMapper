using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class BeatmapEventContainer : BeatmapObjectContainer {


    public override BeatmapObject objectData { get => eventData; set => eventData = (MapEvent)value; }

    public MapEvent eventData;
    public EventsContainer eventsContainer;

    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private List<Renderer> eventRenderer;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private TextMeshPro valueDisplay;
    [SerializeField] private EventGradientController eventGradientController;
    private List<Material> mat;
    private float oldAlpha = -1;

    /// <summary>
    /// Different modes to sort events in the editor.
    /// </summary>
    public static int ModifyTypeMode = 0;

    private void Awake()
    {
        mat = eventRenderer.Select(it => it.materials[0]).ToList();
    }

    public static BeatmapEventContainer SpawnEvent(EventsContainer eventsContainer, MapEvent data, ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO)
    {
        BeatmapEventContainer container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.eventData = data;
        container.eventsContainer = eventsContainer;
        container.eventAppearance = eventAppearanceSO;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }

    public override void UpdateGridPosition()
    {
        if (eventsContainer.PropagationEditing)
        {
            if (eventData._type == eventsContainer.EventTypeToPropagate)
            {
                if (eventData._customData != null &&
                    eventData._customData.Count > 0 &&
                    eventData._customData.HasKey("_propID")
                    && eventData._customData["_propID"].IsNumber)
                {
                    transform.localPosition = new Vector3(
                        eventData._customData["_propID"] + 1.5f,
                        0.5f,
                        eventData._time * EditorScaleController.EditorScale
                    );
                }
                else
                {
                    transform.localPosition = new Vector3(
                        0.5f,
                        0.5f,
                        eventData._time * EditorScaleController.EditorScale
                    );
                }
            }
            else
            {
                SafeSetActive(false);
                transform.localPosition = new Vector3(
                    -0.5f,
                    0.5f,
                    eventData._time * EditorScaleController.EditorScale
                );
            }
        }
        else
        {
            transform.localPosition = new Vector3(
                EventTypeToModifiedType(eventData._type) + 0.5f,
                0.5f,
                eventData._time * EditorScaleController.EditorScale
            );
        }

        chunkID = (int)Math.Round(objectData._time / (double)BeatmapObjectContainerCollection.ChunkSize,
                 MidpointRounding.AwayFromZero);
        transform.localEulerAngles = Vector3.zero;
        if (eventData._lightGradient != null && Settings.Instance.VisualizeChromaGradients)
        {
            eventGradientController.UpdateDuration(eventData._lightGradient.Duration);
        }
    }


    private static int[] ModifiedToEventArray = { 14, 15, 0, 1, 2, 3, 4, 8, 9, 12, 13, 5, 6, 7, 10, 11 };
    private static int[] EventToModifiedArray = { 2, 3, 4, 5, 6, 11, 12, 13, 7, 8, 14, 15, 9, 10, 0, 1 };
    private static readonly int ColorBase = Shader.PropertyToID("_ColorBase");
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int Position = Shader.PropertyToID("_Position");
    private static readonly int MainAlpha = Shader.PropertyToID("_MainAlpha");
    private static readonly int FadeSize = Shader.PropertyToID("_FadeSize");

    /// <summary>
    /// Turns an eventType to a modified type for organizational purposes in the Events Grid.
    /// </summary>
    /// <param name="eventType">Type usually found in a MapEvent object.</param>
    /// <returns></returns>
    public static int EventTypeToModifiedType(int eventType)
    {
        if (ModifyTypeMode == -1) return eventType;
        if (ModifyTypeMode == 0)
        {
            if (!EventToModifiedArray.Contains(eventType))
            {
                Debug.LogWarning($"Event Type {eventType} does not have a modified type");
                return eventType;
            }
            return EventToModifiedArray[eventType];
        }
        else if (ModifyTypeMode == 1)
            switch (eventType)
            {
                case 5: return 1;
                case 1: return 2;
                case 6: return 3;
                case 2: return 4;
                case 7: return 5;
                case 3: return 6;
                case 10: return 7;
                case 4: return 8;
                case 11: return 9;
                case 8: return 10;
                case 9: return 11;
                default: return eventType;
            }
        return -1;
    }

    /// <summary>
    /// Turns a modified type to an event type to be stored in a MapEvent object.
    /// </summary>
    /// <param name="modifiedType">Modified type (Usually from EventPreview)</param>
    /// <returns></returns>
    public static int ModifiedTypeToEventType(int modifiedType)
    {
        if (ModifyTypeMode == -1) return modifiedType;
        if (ModifyTypeMode == 0)
        {
            if (!ModifiedToEventArray.Contains(modifiedType))
            {
                Debug.LogWarning($"Event Type {modifiedType} does not have a valid event type! WTF!?!?");
                return modifiedType;
            }
            return ModifiedToEventArray[modifiedType];
        }
        else if (ModifyTypeMode == 1)
            switch (modifiedType)
            {
                case 1: return 5;
                case 2: return 1;
                case 3: return 6;
                case 4: return 2;
                case 5: return 7;
                case 6: return 3;
                case 7: return 10;
                case 8: return 4;
                case 9: return 11;
                case 10: return 8;
                case 11: return 9;
                default: return modifiedType;
            }
        return -1;
    }

    public void ChangeColor(Color color)
    {
        mat.ForEach(it => it.SetColor(ColorTint, color));
    }

    public void ChangeBaseColor(Color color)
    {
        mat.ForEach(it => it.SetColor(ColorBase, color));
    }

    public void ChangeFadeSize(float size)
    {
        mat.ForEach(it => it.SetFloat(FadeSize, size));
    }

    public void UpdateOffset(Vector3 offset)
    {
        if (gameObject.activeInHierarchy)
            mat.ForEach(it => it.SetVector(Position, offset));
    }

    public void UpdateAlpha(float alpha)
    {
        if (mat.First().GetFloat(MainAlpha) > 0) oldAlpha = mat.First().GetFloat(MainAlpha);

        mat.ForEach(it =>
        {
            it.SetFloat(MainAlpha, alpha == -1 ? oldAlpha : alpha);
        });
    }

    public void UpdateScale(float scale)
    {
        transform.localScale = Vector3.one * scale; //you can do this instead
    }

    public void UpdateGradientRendering()
    {
        if (eventData._lightGradient != null && !eventData.IsUtilityEvent)
        {
            if (eventData._value != MapEvent.LIGHT_VALUE_OFF)
            {
                ChangeColor(eventData._lightGradient.StartColor);
            }
            eventGradientController.SetVisible(true);
            eventGradientController.UpdateGradientData(eventData._lightGradient);
        }
        else
        {
            eventGradientController.SetVisible(false);
        }
    }

    public void UpdateTextDisplay(bool visible, string text = "")
    {
        if (visible != valueDisplay.gameObject.activeSelf)
        {
            valueDisplay.gameObject.SetActive(visible);
        }
        valueDisplay.text = text;
    }

    public void RefreshAppearance()
    {
        eventAppearance.SetEventAppearance(this);
    }
}
