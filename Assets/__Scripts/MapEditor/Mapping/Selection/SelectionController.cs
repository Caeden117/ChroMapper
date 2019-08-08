using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

/// <summary>
/// Big boi master class for everything Selection.
/// </summary>
public class SelectionController : MonoBehaviour
{

    public static List<BeatmapObjectContainer> SelectedObjects = new List<BeatmapObjectContainer>();
    private static List<BeatmapObjectContainer> CopiedObjects = new List<BeatmapObjectContainer>();

    public static Action<BeatmapObjectContainer> ObjectWasSelectedEvent;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private NotesContainer notes;
    [SerializeField] private ObstaclesContainer obstacles;
    [SerializeField] private EventsContainer events;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;
    [SerializeField] private NoteAppearanceSO noteAppearanceSO;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private GameObject notePrefab;
    [SerializeField] private GameObject bombPrefab;
    [SerializeField] private GameObject wallPrefab;
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private Transform notesGrid;
    [SerializeField] private Transform obstaclesGrid;
    [SerializeField] private Transform eventsGrid;

    private bool copied = false;
    private bool selectionWasCut = false;

    private static SelectionController instance;

    // Use this for initialization
    void Start()
    {
        instance = this;
        SelectedObjects.Clear();
    }

    #region Utils

    /// <summary>
    /// Does the user have any selected objects?
    /// </summary>
    public static bool HasSelectedObjects()
    {
        return SelectedObjects.Count > 0;
    }

    /// <summary>
    /// Does the user have any copied objects?
    /// </summary>
    public static bool HasCopiedObjects()
    {
        return CopiedObjects.Count > 0;
    }

    /// <summary>
    /// Returns true if the given container is selected, and false if it's not.
    /// </summary>
    /// <param name="container">Container to check.</param>
    public static bool IsObjectSelected(BeatmapObjectContainer container)
    {
        return SelectedObjects.IndexOf(container) > -1;
    }

    #endregion

    #region Selection

    /// <summary>
    /// Select an individual container.
    /// </summary>
    /// <param name="container">The container to select.</param>
    /// <param name="AddsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    public static void Select(BeatmapObjectContainer container, bool AddsToSelection = false)
    {
        if (IsObjectSelected(container)) return; //Cant select an already selected object now, can ya?
        if (!AddsToSelection) DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        SelectedObjects.Add(container);
        RefreshSelectionMaterial();
        ObjectWasSelectedEvent.Invoke(container);
        Debug.Log("Selected " + container.objectData.beatmapType.ToString());
    }

    public static void MassSelect<T>(T start, T end, bool AddsToSelection = false) where T : BeatmapObjectContainer
    {
        if (start.GetType() != end.GetType()) return;
        if (!AddsToSelection) DeselectAll();
        switch (start.objectData.beatmapType)
        {
            case BeatmapObject.Type.BOMB:
                SelectedObjects.AddRange(instance.notes.loadedNotes.Where(x => x.objectData._time >= start.objectData._time &&
                x.objectData._time <= end.objectData._time && !IsObjectSelected(x)));
                break;
            case BeatmapObject.Type.NOTE:
                SelectedObjects.AddRange(instance.notes.loadedNotes.Where(x => x.objectData._time >= start.objectData._time &&
                x.objectData._time <= end.objectData._time && !IsObjectSelected(x)));
                break;
            case BeatmapObject.Type.OBSTACLE:
                SelectedObjects.AddRange(instance.obstacles.loadedObstacles.Where(x => x.objectData._time >= start.objectData._time &&
                x.objectData._time <= end.objectData._time && !IsObjectSelected(x)));
                break;
            case BeatmapObject.Type.EVENT:
                SelectedObjects.AddRange(instance.events.loadedEvents.Where(x => x.objectData._time >= start.objectData._time &&
                x.objectData._time <= end.objectData._time && !IsObjectSelected(x)));
                break;
        }
        RefreshSelectionMaterial();
    }

    /// <summary>
    /// Selects EVERYTHING in the map. Will this take a few moments? Potentially.
    /// </summary>
    public static void SelectAll()
    {
        List<BeatmapObjectContainer> totalContainers = new List<BeatmapObjectContainer>();
        totalContainers.AddRange(instance.notes.loadedNotes);
        totalContainers.AddRange(instance.obstacles.loadedObstacles);
        totalContainers.AddRange(instance.events.loadedEvents);
        DeselectAll();
        SelectedObjects.AddRange(totalContainers); //Doing this instead of calling Select on everything will result in way less calls to
        RefreshSelectionMaterial(); //This one function, which we can call once instead of potentially tens of thousands of times.
    }

    /// <summary>
    /// Deselects a container if it is currently selected
    /// </summary>
    /// <param name="container">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObjectContainer container)
    {
        if (!IsObjectSelected(container)) return;
        SelectedObjects.Remove(container);
        RefreshSelectionMaterial();
        //We're doing this here instead of in the RefreshSelectionMaterial function so we do not loop through
        //potentially thousands of selected events. Not like that'll happen, but it'll still be good to do it once here.
        List<Material> containerMaterials = container.gameObject.GetComponentInChildren<MeshRenderer>().materials.ToList();
        if (containerMaterials.Count == 2) containerMaterials.Remove(containerMaterials.Last()); //Eh this should work.
        container.gameObject.GetComponentInChildren<MeshRenderer>().materials = containerMaterials.ToArray(); //Set materials
    }

    /// <summary>
    /// Deselect all selected objects.
    /// </summary>
    public static void DeselectAll()
    {
        if (instance.selectionWasCut) instance.Delete();
        foreach (BeatmapObjectContainer con in SelectedObjects)
        {
            //Take all materials from the MeshRenderer of the container
            List<Material> containerMaterials = con.gameObject.GetComponentInChildren<MeshRenderer>().materials.ToList();
            if (containerMaterials.Count == 2) containerMaterials.Remove(containerMaterials.Last()); //Eh this should work.
            con.gameObject.GetComponentInChildren<MeshRenderer>().materials = containerMaterials.ToArray(); //Set materials
        }
        SelectedObjects.Clear();
    }

    /// <summary>
    /// Can be very taxing. Use sparringly.
    /// </summary>
    private static void RefreshSelectionMaterial()
    {
        foreach (BeatmapObjectContainer con in SelectedObjects)
        {
            //Take all materials from the MeshRenderer of the container
            List<Material> containerMaterials = con.gameObject.GetComponentInChildren<MeshRenderer>().materials.ToList();
            if (containerMaterials.Count == 1)
            {   //Because we're dealing with instances of a material, we need to check something else, like the name.
                Material matInstance = new Material(instance.selectionMaterial); //Create a copy of the material
                matInstance.name = instance.selectionMaterial.name; //Slap it the same name as the OG
                containerMaterials.Add(matInstance); //Add ourselves the selection material.
            }
            containerMaterials.Last().color = instance.copied ? instance.copiedColor : instance.selectedColor;
            con.gameObject.GetComponentInChildren<MeshRenderer>().materials = containerMaterials.ToArray(); //Set materials
        }
    }

    #endregion

    #region Manipulation
    
    /// <summary>
    /// Deletes and clears the current selection.
    /// </summary>
    public void Delete()
    {
        foreach (BeatmapObjectContainer con in SelectedObjects)
        {
            if (con is BeatmapNoteContainer)
                notes.loadedNotes.Remove(con as BeatmapNoteContainer);
            else if (con is BeatmapObstacleContainer)
                obstacles.loadedObstacles.Remove(con as BeatmapObstacleContainer);
            else if (con is BeatmapEventContainer)
                events.loadedEvents.Remove(con as BeatmapEventContainer);
            Destroy(con.gameObject);
            RefreshMap();
        }
        SelectedObjects.Clear();
    }
    
    /// <summary>
    /// Copies the current selection for later Pasting.
    /// </summary>
    /// <param name="cut">Whether or not to delete the original selection after copying them.</param>
    public void Copy(bool cut = false)
    {
        Debug.Log("Copied!");
        copied = true;
        selectionWasCut = false;
        CopiedObjects = new List<BeatmapObjectContainer>(SelectedObjects);
        DeselectAll();
        foreach (BeatmapObjectContainer con in CopiedObjects) Select(con, true);
        if (cut)
            foreach (BeatmapObjectContainer con in CopiedObjects) con.gameObject.SetActive(false);
    }

    /// <summary>
    /// Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste()
    {
        if (SelectedObjects.Count == 0) return;
        CopiedObjects = CopiedObjects.OrderBy((x) => x.objectData._time).ToList();
        List<BeatmapObjectContainer> PastedObjects = new List<BeatmapObjectContainer>(); //For re-selecting
        copied = false;
        float t = atsc.CurrentBeat;
        float startTime = CopiedObjects.First().objectData._time;
        foreach (BeatmapObjectContainer con in CopiedObjects)
        {
            float newTime = t + (con.objectData._time - startTime);
            if (con is BeatmapNoteContainer)
            {
                BeatmapNote data = new BeatmapNote((con as BeatmapNoteContainer).mapNoteData.ConvertToJSON());
                data._time = newTime;
                BeatmapNoteContainer beatmapNote = BeatmapNoteContainer.SpawnBeatmapNote(data,
                    ref notePrefab, ref bombPrefab, ref noteAppearanceSO);
                beatmapNote.transform.SetParent(notesGrid);
                beatmapNote.UpdateGridPosition();
                notes.loadedNotes.Add(beatmapNote);
                PastedObjects.Add(beatmapNote);
            }
            if (con is BeatmapObstacleContainer)
            {
                BeatmapObstacle data = new BeatmapObstacle((con as BeatmapObstacleContainer).obstacleData.ConvertToJSON());
                data._time = newTime;
                BeatmapObstacleContainer beatmapObstacle = BeatmapObstacleContainer.SpawnObstacle(data, ref wallPrefab);
                beatmapObstacle.transform.SetParent(obstaclesGrid);
                beatmapObstacle.UpdateGridPosition();
                obstacles.loadedObstacles.Add(beatmapObstacle);
                PastedObjects.Add(beatmapObstacle);
            }
            if (con is BeatmapEventContainer)
            {
                MapEvent data = new MapEvent((con as BeatmapEventContainer).eventData.ConvertToJSON());
                data._time = newTime;
                BeatmapEventContainer beatmapEvent = BeatmapEventContainer.SpawnEvent(data,
                    ref eventPrefab, ref eventAppearanceSO);
                beatmapEvent.transform.SetParent(eventsGrid);
                beatmapEvent.UpdateGridPosition();
                events.loadedEvents.Add(beatmapEvent);
                PastedObjects.Add(beatmapEvent);
            }
        }
        CopiedObjects.Clear();
        DeselectAll();
        foreach (BeatmapObjectContainer pasted in PastedObjects) Select(pasted, true);
        RefreshMap();
        Debug.Log("Pasted!");
        CopiedObjects = new List<BeatmapObjectContainer>(SelectedObjects);
    }

    public void MoveSelection(float beats)
    {
        foreach (BeatmapObjectContainer con in SelectedObjects)
        {
            con.objectData._time += beats;
            con.UpdateGridPosition();
        }
    }

    public static void RefreshMap()
    {
        instance.notes.SortNotes();
        instance.obstacles.SortObstacles();
        instance.events.SortEvents();
        if (BeatSaberSongContainer.Instance.map != null)
        {
            List<BeatmapNote> newNotes = new List<BeatmapNote>();
            foreach (BeatmapNoteContainer n in instance.notes.loadedNotes) newNotes.Add(n.mapNoteData);
            List<BeatmapObstacle> newObstacles = new List<BeatmapObstacle>();
            foreach (BeatmapObstacleContainer o in instance.obstacles.loadedObstacles) newObstacles.Add(o.obstacleData);
            List<MapEvent> newEvents = new List<MapEvent>();
            foreach (BeatmapEventContainer e in instance.events.loadedEvents) newEvents.Add(e.eventData);
            BeatSaberSongContainer.Instance.map._notes = newNotes;
            BeatSaberSongContainer.Instance.map._obstacles = newObstacles;
            BeatSaberSongContainer.Instance.map._events = newEvents;
        }
    }

    #endregion

}
