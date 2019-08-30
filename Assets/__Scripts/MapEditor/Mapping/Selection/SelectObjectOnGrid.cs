using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectObjectOnGrid : MonoBehaviour {

    [SerializeField]
    private AudioTimeSyncController atsc;
    [SerializeField]
    private NotesContainer notes;
    [SerializeField]
    private ObstaclesContainer obstacles;
    [SerializeField]
    private EventsContainer events;

    private readonly int EventGridOffset = 17;
    private int highlightedIndex = 0;
    private int highlightedLayer = 0;
    private int wallType = 0;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		if (!atsc.IsPlaying)
        {
            int layerMask = 1 << 10;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 999, layerMask))
            {
                Collider collider = hit.transform.GetComponent<Collider>();
                highlightedIndex = Mathf.RoundToInt(Mathf.Clamp(Mathf.Ceil(hit.point.x),
                    Mathf.Ceil(collider.bounds.min.x),
                    Mathf.Floor(collider.bounds.max.x)) + 1);
                highlightedLayer = Mathf.RoundToInt(Mathf.Clamp(Mathf.Floor(hit.point.y - 0.1f), 0f,
                        Mathf.Floor(collider.bounds.max.y)));
                wallType = hit.point.y <= 1.5f ? 0 : 1;
                if (Input.GetMouseButtonDown(0)) AttemptToSelect();
            }
        }
	}

    private void AttemptToSelect()
    {
        if (highlightedIndex >= EventGridOffset)
        {
            int gridOffset = highlightedIndex - EventGridOffset; //Take the difference
            gridOffset = BeatmapEventContainer.ModifiedTypeToEventType(gridOffset); //And turn from modified to event type
            BeatmapObjectContainer highlighting = events.LoadedContainers.Where(
                (BeatmapObjectContainer x) => x.objectData._time >= atsc.CurrentBeat - 1/64f && //Check time, within a small margin
                x.objectData._time <= atsc.CurrentBeat + 1 / 64f && //Check time, within a small margin
                (x.objectData as MapEvent)._type == gridOffset //Check type against what we calculated earlier
            ).FirstOrDefault();
            if (highlighting != null) SelectOrDeselect(highlighting);
        }
        else //For the notes grid, check notes first then obstacles.
        {
            BeatmapObjectContainer highlighting = notes.LoadedContainers.Where(
                (BeatmapObjectContainer x) => x.objectData._time >= atsc.CurrentBeat - 1 / 64f && //Check time, within a small margin
                x.objectData._time <= atsc.CurrentBeat + 1 / 64f && //Check time, within a small margin
                (x.objectData as BeatmapNote)._lineIndex == highlightedIndex && //Check index
                (x.objectData as BeatmapNote)._lineLayer == highlightedLayer //Check layer
            ).FirstOrDefault(); //Grab first instance (Or null if there is none)
            if (highlighting != null)
                SelectOrDeselect(highlighting);
            else
            {
                highlighting = obstacles.LoadedContainers.Where((BeatmapObjectContainer x) =>
                    (x.objectData as BeatmapObstacle)._lineIndex <= highlightedIndex && //If it's between the left side
                    (x.objectData as BeatmapObstacle)._lineIndex + ((x.objectData as BeatmapObstacle)._width - 1) >= highlightedIndex && //...and the right
                    (x.objectData as BeatmapObstacle)._duration + x.objectData._time >= atsc.CurrentBeat && //If it's between the end point in time
                    x.objectData._time <= atsc.CurrentBeat && //...and the beginning point in time
                    (x.objectData as BeatmapObstacle)._type == wallType //And, for good measure, if they match types.
                ).FirstOrDefault();
                if (highlighting != null) SelectOrDeselect(highlighting);
            }
        }
    }

    private void SelectOrDeselect<T>(T container) where T : BeatmapObjectContainer
    {
        if (SelectionController.HasSelectedObjects() && Input.GetKey(KeyCode.LeftControl))
        {
            if (SelectionController.SelectedObjects.Last().objectData.beatmapType == container.objectData.beatmapType &&
                container.objectData._time >= SelectionController.SelectedObjects.Last().objectData._time)
            {
                SelectionController.MassSelect(SelectionController.SelectedObjects.Last(), container, Input.GetKey(KeyCode.LeftShift));
                return;
            }
        }
        if (!Input.GetKey(KeyCode.LeftShift)) return;
        if (SelectionController.IsObjectSelected(container)) //Shift Right-Click on a selected object will deselect.
            SelectionController.Deselect(container);
        else //Else it will try to select again.
            SelectionController.Select(container, Input.GetKey(KeyCode.LeftShift));
    }
}
