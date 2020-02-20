using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

public class BeatmapEventContainer : BeatmapObjectContainer {


    public override BeatmapObject objectData { get => eventData; set => eventData = (MapEvent)value; }

    public MapEvent eventData;
    public EventsContainer eventsContainer;

    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private Renderer eventRenderer;
    [SerializeField] private TracksManager tracksManager;
    private Material mat;
    private float oldAlpha = -1;

    /// <summary>
    /// Different modes to sort events in the editor.
    /// </summary>
    public static int ModifyTypeMode = 0;

    protected override void Awake()
    {
        mat = eventRenderer.material;
        base.Awake();
    }

    public static BeatmapEventContainer SpawnEvent(EventsContainer eventsContainer, MapEvent data, ref GameObject prefab, ref EventAppearanceSO eventAppearanceSO,
        ref TracksManager tracksManager)
    {
        BeatmapEventContainer container = Instantiate(prefab).GetComponent<BeatmapEventContainer>();
        container.eventData = data;
        container.eventsContainer = eventsContainer;
        container.eventAppearance = eventAppearanceSO;
        container.transform.localEulerAngles = Vector3.zero;
        container.tracksManager = tracksManager;
        eventAppearanceSO.SetEventAppearance(container);
        return container;
    }

    public override void UpdateGridPosition()
    {
        if (eventsContainer.RingPropagationEditing)
        {
            if (eventData._customData["_propID"].IsNumber)
            {
                transform.localPosition = new Vector3(
                    (eventData._customData["_propID"] + 1) + 0.5f,
                    0.5f,
                    eventData._time * EditorScaleController.EditorScale
                );
            }
            else
            {
                transform.localPosition = new Vector3(
                    0 + 0.5f,
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
        
        transform.localEulerAngles = Vector3.zero;
    }


    private static int[] ModifiedToEventArray = { 14, 15, 0, 1, 2, 3, 4, 8, 9, 12, 13, 5, 6, 7, 10, 11 };
    private static int[] EventToModifiedArray = { 2, 3, 4, 5, 6, 11, 12, 13, 7, 8, 14, 15, 9, 10, 0, 1 };
    private static readonly int ColorTint = Shader.PropertyToID("_ColorTint");
    private static readonly int Position = Shader.PropertyToID("_Position");
    private static readonly int MainAlpha = Shader.PropertyToID("_MainAlpha");

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
            return ModifiedToEventArray[modifiedType];
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
        if (gameObject.activeInHierarchy)
            mat.SetColor(ColorTint, color);
    }

    public void UpdateOffset(Vector3 offset)
    {
        if (gameObject.activeInHierarchy)
            mat.SetVector(Position, offset);
    }

    public void UpdateAlpha(float alpha)
    {
        if (gameObject.activeInHierarchy)
            if (mat.GetFloat(MainAlpha) > 0) oldAlpha = mat.GetFloat(MainAlpha);
            mat.SetFloat(MainAlpha, alpha == -1 ? oldAlpha : alpha);
    }

    public void UpdateScale(float scale)
    {
        if (gameObject.activeInHierarchy)
            transform.localScale = Vector3.one * scale; //you can do this instead
    }

    public void RefreshAppearance()
    {
        eventAppearance.SetEventAppearance(this);
    }

    private IEnumerator changeColor(Color color)
    {
        yield return new WaitUntil(() => mat != null);
        mat.SetColor("_ColorTint", color);
    }

    private IEnumerator updateOffset(Vector3 offset)
    {
        yield return new WaitUntil(() => mat != null);
        mat.SetVector("_Position", offset);
    }

    private IEnumerator updateAlpha(float alpha = -1)
    {
        yield return new WaitUntil(() => mat != null);
        if (mat.GetFloat("_MainAlpha") > 0) oldAlpha = mat.GetFloat("_MainAlpha");
        mat.SetFloat("_MainAlpha", alpha == -1 ? oldAlpha : alpha);
    }

    internal override void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(2) && !KeybindsController.ShiftHeld)
        {
            if (eventData.IsRotationEvent)
            {
                int? rotation = eventData.GetRotationDegreeFromValue();
                if (rotation != null)
                {
                    if (eventData._value >= 0 && eventData._value < MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.Length)
                        eventData._value = MapEvent.LIGHT_VALUE_TO_ROTATION_DEGREES.ToList().IndexOf((rotation ?? 0) * -1);
                    else if (eventData._value >= 1000 && eventData._value <= 1720) //Invert Mapping Extensions precision rotation
                        eventData._value = 1720 - (eventData._value - 1000);
                }
                eventAppearance.SetEventAppearance(this);
                tracksManager.RefreshTracks();
                return;
            }
            if (eventData.IsUtilityEvent) return;
            if (eventData._value > 4 && eventData._value < 8) eventData._value -= 4;
            else if (eventData._value > 0 && eventData._value <= 4) eventData._value += 4;
            eventAppearance.SetEventAppearance(this);
        }
        else if (Input.GetAxis("Mouse ScrollWheel") != 0)
        {
            if (KeybindsController.AltHeld && !eventData.IsRingEvent)
            {
                eventData._value += Input.GetAxis("Mouse ScrollWheel") > 0 ? 1 : -1;
                if (eventData._value == 4 && !eventData.IsUtilityEvent)
                    eventData._value += Input.GetAxis("Mouse ScrollWheel") > 0 ? 1 : -1;
                if (eventData._value < 0) eventData._value = 0;
                if (!eventData.IsLaserSpeedEvent)
                    if (eventData._value > 7) eventData._value = 7;
                eventAppearance.SetEventAppearance(this);
            }
        }
        else base.OnMouseOver();
    }

    internal override void SafeSetActive(bool active)
    {
        if (active != (eventRenderer is null ? active : eventRenderer.enabled))
        {
            eventRenderer.enabled = active;
            TextMeshProUGUI text = GetComponentInChildren<TextMeshProUGUI>();
            if (text != null) text.enabled = active;
            boxCollider.enabled = active;
        }
    }
}
