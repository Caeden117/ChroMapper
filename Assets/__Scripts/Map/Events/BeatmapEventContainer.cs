using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class BeatmapEventContainer : BeatmapObjectContainer
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

    [FormerlySerializedAs("eventData")] public MapEvent EventData;
    [FormerlySerializedAs("eventsContainer")] public EventsContainer EventsContainer;

    public EventModelType EventModel
    {
        get => (EventModelType)eventModel;
        set
        {
            for (var i = 0; i < eventModels.Length; i++)
            {
                eventModels[i].SetActive(i == (int)value);
            }
            eventModel = (int)value;
        }
    }

    // This needs to be an int for the below properties
    private int eventModel;

    public Vector3 FlashShaderOffset => eventModels[eventModel].GetComponent<MaterialParameters>().FlashShaderOffset;
    public Vector3 FadeShaderOffset => eventModels[eventModel].GetComponent<MaterialParameters>().FadeShaderOffset;
    public float DefaultFadeSize => eventModels[eventModel].GetComponent<MaterialParameters>().DefaultFadeSize;
    public float BoostEventFadeSize => eventModels[eventModel].GetComponent<MaterialParameters>().BoostEventFadeSize;

    [SerializeField] protected EventAppearanceSO EventAppearance;
    [SerializeField] private List<Renderer> eventRenderer;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private TextMesh valueDisplay;
    [SerializeField] private EventGradientController eventGradientController;
    [SerializeField] private GameObject[] eventModels;
    [SerializeField] private CreateEventTypeLabels labels;

    private float oldAlpha = -1;

    public override BeatmapObject ObjectData { get => EventData; set => EventData = (MapEvent)value; }

    public static BeatmapEventContainer SpawnEvent(EventsContainer eventsContainer, MapEvent data, ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO, ref CreateEventTypeLabels labels)
    {
        var container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.EventData = data;
        container.EventsContainer = eventsContainer;
        container.EventAppearance = eventAppearanceSO;
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
        {
            eventGradientController.UpdateDuration(EventData.LightGradient.Duration);
        }
        //Move event up or down enough to give a constant distance from the bottom of the event, taking the y alpha scale into account
        if (Settings.Instance.VisualizeChromaAlpha) transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y + ((GetHeight() - 1f) / 2.775f), transform.localPosition.z);
        UpdateCollisionGroups();
    }

    public void ChangeColor(Color color, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetColor(colorTint, color);
        if (updateMaterials) UpdateMaterials();
    }

    public void ChangeBaseColor(Color color, bool updateMaterials = true)
    {
        MaterialPropertyBlock.SetColor(colorBase, color);
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

    public void UpdateScale(float scale) => transform.localScale = new Vector3(1, Settings.Instance.VisualizeChromaAlpha ? GetHeight() : 1, 1) * scale; //you can do this instead//Change the scale of the event height based on the alpha of the event if alpha visualization is on

    private float GetHeight()
    {
        var height = EventData.FloatValue;
        if (EventData.CustomData != null && EventData.CustomData.HasKey("_color") && EventData.CustomData["_color"].Count == 4)
        {
            height *= EventData.CustomData["_color"][3];  //The alpha of the event, clamped to avoid too small/too tall events
        }
        else if (EventData.CustomData != null && EventData.CustomData.HasKey("_lightGradient") && EventData.CustomData["_lightGradient"].HasKey("_startColor") && EventData.CustomData["_lightGradient"]["_startColor"].Count == 4)
        {
            height *= EventData.CustomData["_lightGradient"]["_startColor"][3];
        }
        return Mathf.Clamp(height, 0.1f, 1.5f);
    }

    public void UpdateGradientRendering()
    {
        if (EventData.LightGradient != null && !EventData.IsUtilityEvent)
        {
            if (Settings.Instance.EmulateChromaLite && EventData.Value != MapEvent.LightValueOff)
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

    public void UpdateTextColor(Color color)
    {
        valueDisplay.color = color;
    }
    public void RefreshAppearance() => EventAppearance.SetEventAppearance(this);
}
