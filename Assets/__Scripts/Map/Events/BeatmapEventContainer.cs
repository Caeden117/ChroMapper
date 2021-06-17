using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BeatmapEventContainer : BeatmapObjectContainer {


    public override BeatmapObject objectData { get => eventData; set => eventData = (MapEvent)value; }

    public MapEvent eventData;
    public EventsContainer eventsContainer;

    public bool UsePyramidModel
    {
        get => pyramidModel.activeSelf;
        set
        {
            pyramidModel.SetActive(value);
            cubeModel.SetActive(!value);
        }
    }

    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private List<Renderer> eventRenderer;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private TextMesh valueDisplay;
    [SerializeField] private EventGradientController eventGradientController;
    [SerializeField] private GameObject cubeModel;
    [SerializeField] private GameObject pyramidModel;
    [SerializeField] private CreateEventTypeLabels labels;

    private float oldAlpha = -1;

    /// <summary>
    /// Different modes to sort events in the editor.
    /// </summary>
    public static int ModifyTypeMode = 0;

    public static BeatmapEventContainer SpawnEvent(EventsContainer eventsContainer, MapEvent data, ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO, ref CreateEventTypeLabels labels)
    {
        BeatmapEventContainer container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.eventData = data;
        container.eventsContainer = eventsContainer;
        container.eventAppearance = eventAppearanceSO;
        container.labels = labels;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }

    public override void UpdateGridPosition()
    {
        var position = eventData.GetPosition(labels, eventsContainer.PropagationEditing, eventsContainer.EventTypeToPropagate);

        if (position == null)
        {
            transform.localPosition = new Vector3(
                -0.5f,
                0.5f,
                eventData._time * EditorScaleController.EditorScale
            );
            SafeSetActive(false);
        }
        else
        {
            transform.localPosition = new Vector3(
                position?.x ?? 0,
                position?.y ?? 0,
                eventData._time * EditorScaleController.EditorScale
            );
        }

        transform.localEulerAngles = Vector3.zero;
        if (eventData._lightGradient != null && Settings.Instance.VisualizeChromaGradients)
        {
            eventGradientController.UpdateDuration(eventData._lightGradient.Duration);
        }

        UpdateCollisionGroups();
    }

    private static readonly int ColorBase = Shader.PropertyToID("_ColorBase");
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int Position = Shader.PropertyToID("_Position");
    private static readonly int MainAlpha = Shader.PropertyToID("_MainAlpha");
    private static readonly int FadeSize = Shader.PropertyToID("_FadeSize");
    private static readonly int SpotlightSize = Shader.PropertyToID("_SpotlightSize");

    public void ChangeColor(Color color, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetColor(ColorTint, color);
        if (updateMaterials) UpdateMaterials();
    }

    public void ChangeBaseColor(Color color, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetColor(ColorBase, color);
        if (updateMaterials) UpdateMaterials();
    }

    public void ChangeFadeSize(float size, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetFloat(FadeSize, size);
        if (updateMaterials) UpdateMaterials();
    }

    public void ChangeSpotlightSize(float size, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetFloat(SpotlightSize, size);
        if (updateMaterials) UpdateMaterials();
    }

    public void UpdateOffset(Vector3 offset, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetVector(Position, offset);
        if (updateMaterials) UpdateMaterials();
    }

    public void UpdateAlpha(float alpha, bool updateMaterials = true)
    {
        float oldAlphaTemp = MaterialPropertyBlock.GetFloat(MainAlpha);
        if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
        if (oldAlpha == alpha) return;

        MaterialPropertyBlock.SetFloat(MainAlpha, alpha == -1 ? oldAlpha : alpha);
        if (updateMaterials) UpdateMaterials();
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
                ChangeBaseColor(eventData._lightGradient.StartColor);
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
