using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SelectObjectOnGrid : MonoBehaviour {
    //TODO is this needed?
    /*[SerializeField] private NotesContainer notesContainer;
    [SerializeField] private EventsContainer eventsContainer;

    void Start()
    {
        SelectionController.ObjectWasSelectedEvent += ObjectSelected;
    }

    void ObjectSelected(BeatmapObjectContainer container)
    {
        if (!KeybindsController.CtrlHeld || Settings.Instance.BoxSelect) return; //Only do mass selection on ctrl
        if (SelectionController.SelectedObjects.Count() < 2 && !KeybindsController.AltHeld) return;
        List<BeatmapObjectContainer> containers = new List<BeatmapObjectContainer>(SelectionController.SelectedObjects);
        List<BeatmapEventContainer> events = containers.Where(x => x is BeatmapEventContainer).Cast<BeatmapEventContainer>().ToList(); //Filter containers
        List<BeatmapNoteContainer> notes = containers.Where(x => x is BeatmapNoteContainer).Cast<BeatmapNoteContainer>().ToList();
        Debug.Log("MASS SELECT TIME");
        if (events.Count > 0)
        {
            events = events.OrderBy(x => x.eventData._type).ThenByDescending(x => x.objectData._time).ToList();
            for (var i = 0; i < 15; i++)
            {
                List<BeatmapEventContainer> eventsAtType = events.Where(x => x.eventData._type == i).ToList();
                if (eventsAtType.Count >= 2)
                {
                    List<BeatmapObjectContainer> inBetween = eventsContainer.LoadedObjects.Where(x =>
                        x.objectData._time >= eventsAtType.Last().objectData._time &&
                        x.objectData._time <= eventsAtType.First().objectData._time &&
                        (x.objectData as MapEvent)._type == i).ToList();
                    foreach (BeatmapObjectContainer con in inBetween) SelectionController.Select(con, true, false);
                }
            }
        }
        if (notes.Count > 0)
        {
            notes = notes.OrderBy(x => x.mapNoteData._lineIndex).ThenByDescending(x => x.objectData._time).ToList();
            for (var i = notes.First().mapNoteData._lineIndex; i <= notes.Last().mapNoteData._lineIndex; i++)
            {
                List<BeatmapNoteContainer> notesAtIndex = notes.Where(x => x.mapNoteData._lineIndex == i).ToList();
                if (notesAtIndex.Count >= 2)
                {
                    List<BeatmapObjectContainer> inBetween = notesContainer.LoadedObjects.Where(x =>
                        x.objectData._time >= notesAtIndex.Last().objectData._time &&
                        x.objectData._time <= notesAtIndex.First().objectData._time &&
                        ((BeatmapNote) x.objectData)._lineIndex == i).ToList();
                    foreach (BeatmapObjectContainer con in inBetween) SelectionController.Select(con, true, false);
                }
            }
        }
        SelectionController.RefreshSelectionMaterial();
    }

    private void Update()
    {
        /*
         * While this may be a copy of PlacementController code and BeatmapObjectContainer selecting, this is actually to help
         * select objects that are under the placement grid (such as walls)
         * /
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 999f, 1 << 11))
        {
            if (Physics.Raycast(ray, out _, hit.distance, 1 << 9)) return; //Do not select under grid if something is already selected.
            Ray gridRay = new Ray(hit.point, ray.direction);
            if (Physics.Raycast(gridRay, out RaycastHit gridHit, 999f, 1 << 9))
            {
                BeatmapObjectContainer con = gridHit.transform.gameObject.GetComponent<BeatmapObjectContainer>();
                if (con is null) return;
                //con.OnMouseOver();
            }
        }
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectSelected;
    }*/
}
