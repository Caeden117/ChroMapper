using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.UI;

public class EventPreview : MonoBehaviour {

    [SerializeField] EventAppearanceSO eventAppearance;
    [SerializeField] GameObject blankEvent;
    [SerializeField] AudioTimeSyncController atsc;
    [SerializeField] Transform eventsGrid;
    [SerializeField] EventsContainer eventsContainer;
    [SerializeField] InputField laserSpeedInputField;

    public static bool IsActive = false;

    private GameObject hoverEvent;
    private BeatSaberMap map;
    private bool ready = false;
    private int previousType = -1;

    public static int QueuedValue = MapEvent.LIGHT_VALUE_RED_ON;
    public static int QueuedChromaColor = -1;
    private static bool IsChromaEvent = false;
    private static EventPreview instance;
    private static BeatmapEventContainer container;

    // Use this for initialization
    void Start () {
        instance = this;
        map = BeatSaberSongContainer.Instance.map;
	}
	
	void OnMouseOver()
    {
        if (PauseManager.IsPaused) return;
        if (hoverEvent == null) RefreshHovers();
        if (atsc.IsPlaying && hoverEvent.activeSelf) hoverEvent.SetActive(false);
        if (!Input.GetMouseButton(1) && !atsc.IsPlaying)
        {
            IsActive = true;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit))
            {
                hoverEvent.transform.position = new Vector3(
                    Mathf.Clamp(Mathf.Ceil(hit.point.x + 0.1f),
                        Mathf.Ceil(GetComponent<MeshCollider>().bounds.min.x),
                        Mathf.Floor(GetComponent<MeshCollider>().bounds.max.x)
                    ) - 0.5f,
                    Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                        Mathf.Floor(GetComponent<MeshCollider>().bounds.max.y)) + 0.5f,
                    0);
            }
            hoverEvent.SetActive(true);
            container.eventData._type = BeatmapEventContainer.ModifiedTypeToEventType(Mathf.RoundToInt(hoverEvent.transform.position.x - 15.5f));
            if (container.eventData._type == MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED ||
                container.eventData._type == MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED)
            {
                int laserSpeed = 0;
                if (int.TryParse(laserSpeedInputField.text, out laserSpeed))
                    container.eventData._value = laserSpeed;
            }
            if (previousType != container.eventData._type) UpdateHoverEvent();
            previousType = container.eventData._type;
        }
        else if (hoverEvent.activeSelf) OnMouseExit();
        if (Input.GetMouseButtonDown(0)) ApplyEventToMap();
        if (Input.GetKeyDown(KeyCode.Delete)) DeleteHoveringEvent();
    }

    void OnMouseExit()
    {
        IsActive = false;
        if (hoverEvent != null) hoverEvent.SetActive(false);
    }

    public static void UpdateHoverChromaColor(Color chromaRGBColor) {
        if (chromaRGBColor == Color.black) QueuedChromaColor = -1;
        else QueuedChromaColor = ColourManager.ColourToInt(chromaRGBColor);
        IsChromaEvent = true;
    }

    public static void UpdateHoverEventValue(int value)
    {
        QueuedValue = value;
        try
        {
            container.eventData._value = value;
            UpdateHoverEvent();
        }
        catch { }
    }

    void ApplyEventToMap()
    {
        if (atsc.IsPlaying) return; //woops forgot about this
        eventsContainer.loadedEvents.Add(AddEvent(container.eventData, atsc.CurrentBeat));
        if (container.eventData._type == MapEvent.EVENT_TYPE_LEFT_LASERS_SPEED ||
                container.eventData._type == MapEvent.EVENT_TYPE_RIGHT_LASERS_SPEED)
        {
            int laserSpeed = 0;
            if (int.TryParse(laserSpeedInputField.text, out laserSpeed))
                container.eventData._value = laserSpeed;
        }
        if (QueuedChromaColor != -1 && !container.eventData.IsUtilityEvent() && container.eventData._value != MapEvent.LIGHT_VALUE_OFF)
        {
            MapEvent chromaEvent = new MapEvent(container.eventData._time, container.eventData._type, QueuedChromaColor);
            eventsContainer.loadedEvents.Add(AddEvent(chromaEvent, atsc.CurrentBeat - (1f/64f), true));
        }
        eventsContainer.SortEvents();
        List<MapEvent> newEvents = new List<MapEvent>();
        foreach (BeatmapEventContainer con in eventsContainer.loadedEvents) newEvents.Add(con.eventData);
        BeatSaberSongContainer.Instance.map._events = newEvents;
        RefreshHovers();
        UndoRedoController.CreateSnapshot();
    }

    public BeatmapEventContainer AddEvent(MapEvent data, float time, bool triggersColourHistory = false)
    {
        BeatmapObjectContainer conflicting = eventsContainer.loadedEvents.Where(
            (BeatmapObjectContainer x) => x.objectData._time == time && //Check time
            (x.objectData as MapEvent)._type == data._type
            ).FirstOrDefault();
        if (conflicting != null)
        {
            eventsContainer.loadedEvents.Remove(conflicting);
            Destroy(conflicting.gameObject);
        }
        data._time = time;
        BeatmapEventContainer beatmapEvent = BeatmapEventContainer.SpawnEvent(data, ref blankEvent, ref eventAppearance);
        beatmapEvent.transform.SetParent(eventsGrid);
        beatmapEvent.UpdateGridPosition();
        if (triggersColourHistory) ColourHistory.AddColour(ColourManager.ColourFromInt(QueuedChromaColor));
        return beatmapEvent;
    }

    void DeleteHoveringEvent()
    {
        BeatmapObjectContainer conflicting = eventsContainer.loadedEvents.Where(
            (BeatmapObjectContainer x) => x.objectData._time == atsc.CurrentBeat && //Check time
            (x.objectData as MapEvent)._type == container.eventData._type //Check type (same location)
            ).FirstOrDefault();
        if (conflicting == null) return;
        UndoRedoController.CreateSnapshot();

        //Detect a Chroma event to delete as well.
        BeatmapObjectContainer conflictingChroma = eventsContainer.loadedEvents.Where((BeatmapObjectContainer x) =>
        (x.objectData as MapEvent)._type == container.eventData._type && //Ensure same type
        !(x.objectData as MapEvent).IsUtilityEvent() && //And that they are not utility
        x.objectData._time >= conflicting.objectData._time - (1f / 16f) && //They are close enough behind said container
        (x.objectData as MapEvent)._value >= ColourManager.RGB_INT_OFFSET //And they be a Chroma event.
        ).FirstOrDefault() as BeatmapEventContainer;

        eventsContainer.loadedEvents.Remove(conflicting);
        if (conflictingChroma != null) eventsContainer.loadedEvents.Remove(conflictingChroma);
        UndoRedoController.AddToRecover(conflicting);
        if (conflictingChroma != null) UndoRedoController.AddToRecover(conflictingChroma);
        eventsContainer.SortEvents();
        List<MapEvent> newEvents = new List<MapEvent>();
        foreach (BeatmapEventContainer con in eventsContainer.loadedEvents) newEvents.Add(con.eventData);
        BeatSaberSongContainer.Instance.map._events = newEvents;
    }

    void RefreshHovers()
    {
        ready = false;
        if (hoverEvent != null) Destroy(hoverEvent);
        hoverEvent = Instantiate(blankEvent);
        hoverEvent.name = "Hover Event";
        StopAllCoroutines();
        container = hoverEvent.GetComponent<BeatmapEventContainer>();
        if (QueuedValue != -1) container.eventData._value = QueuedValue;
        ready = true;
        UpdateHoverEvent();
    }

    private static void UpdateHoverEvent()
    {
        instance.StopAllCoroutines();
        if (instance.hoverEvent != null) instance.StartCoroutine(instance.WaitThenUpdate());
    }

    private IEnumerator WaitThenUpdate()
    {
        yield return new WaitUntil(() => ready);
        yield return new WaitUntil(() => hoverEvent.activeSelf == true);
        try
        {
            eventAppearance.SetEventAppearance(container);
        }
        catch
        {
            Debug.Log("That Hover Event boi probably inactive...");
        }
    }
}
