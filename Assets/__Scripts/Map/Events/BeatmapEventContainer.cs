using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapEventContainer : BeatmapObjectContainer
{
    /// <summary>
    ///     Different modes to sort events in the editor.
    /// </summary>
    public static int ModifyTypeMode = 0;

    private static readonly int ColorBase = Shader.PropertyToID("_ColorBase");
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int Position = Shader.PropertyToID("_Position");
    private static readonly int MainAlpha = Shader.PropertyToID("_MainAlpha");
    private static readonly int FadeSize = Shader.PropertyToID("_FadeSize");
    private static readonly int SpotlightSize = Shader.PropertyToID("_SpotlightSize");

    [FormerlySerializedAs("eventData")] public MapEvent EventData;
    [FormerlySerializedAs("eventsContainer")] public EventsContainer EventsContainer;

    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private List<Renderer> eventRenderer;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private TextMesh valueDisplay;
    [SerializeField] private EventGradientController eventGradientController;
    [SerializeField] private GameObject cubeModel;
    [SerializeField] private GameObject pyramidModel;
    [SerializeField] private CreateEventTypeLabels labels;

    private float oldAlpha = -1;


    public override BeatmapObject ObjectData { get => EventData; set => EventData = (MapEvent)value; }

    public bool UsePyramidModel
    {
        get => pyramidModel.activeSelf;
        set
        {
            pyramidModel.SetActive(value);
            cubeModel.SetActive(!value);
        }
    }

    public static BeatmapEventContainer SpawnEvent(EventsContainer eventsContainer, MapEvent data,
        ref GameObject prefab, ref EventAppearanceSO eventAppearanceSo, ref CreateEventTypeLabels labels)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.EventData = data;
        container.EventsContainer = eventsContainer;
        container.eventAppearance = eventAppearanceSo;
        container.labels = labels;
        container.transform.localEulerAngles = Vector3.zero;
        return container;
    }

    public override void UpdateGridPosition()
    {
        var position = EventData.GetPosition(labels, EventsContainer.PropagationEditing,
            EventsContainer.EventTypeToPropagate);

        if (position == null)
        {
            transform.localPosition = new Vector3(
                -0.5f,
                0.5f,
                EventData.Time * EditorScaleController.EditorScale
            );
            SafeSetActive(false);
        }
        else
        {
            transform.localPosition = new Vector3(
                position?.x ?? 0,
                position?.y ?? 0,
                EventData.Time * EditorScaleController.EditorScale
            );
        }

        transform.localEulerAngles = Vector3.zero;
        if (EventData.LightGradient != null && Settings.Instance.VisualizeChromaGradients)
            eventGradientController.UpdateDuration(EventData.LightGradient.Duration);

        UpdateCollisionGroups();
    }

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
        var oldAlphaTemp = MaterialPropertyBlock.GetFloat(MainAlpha);
        if (oldAlphaTemp > 0) oldAlpha = oldAlphaTemp;
        if (oldAlpha == alpha) return;

        MaterialPropertyBlock.SetFloat(MainAlpha, alpha == -1 ? oldAlpha : alpha);
        if (updateMaterials) UpdateMaterials();
    }

    public void UpdateScale(float scale) => transform.localScale = Vector3.one * scale; //you can do this instead

    public void UpdateGradientRendering()
    {
        if (EventData.LightGradient != null && !EventData.IsUtilityEvent)
        {
            if (EventData.Value != MapEvent.LightValueOff)
            {
                ChangeColor(EventData.LightGradient.StartColor);
                ChangeBaseColor(EventData.LightGradient.StartColor);
            }

            eventGradientController.SetVisible(true);
            eventGradientController.UpdateGradientData(EventData.LightGradient);
        }
        else
        {
            eventGradientController.SetVisible(false);
        }
    }

    public void UpdateTextDisplay(bool visible, string text = "")
    {
        if (visible != valueDisplay.gameObject.activeSelf) valueDisplay.gameObject.SetActive(visible);
        valueDisplay.text = text;
    }

    public void RefreshAppearance() => eventAppearance.SetEventAppearance(this);
}
