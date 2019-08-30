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
    [SerializeField] private Transform moveableGridTransform;
    private BeatmapObjectContainerCollection[] collections;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;

    private bool copied = false;
    private bool selectionWasCut = false;

    private static SelectionController instance;

    // Use this for initialization
    void Start()
    {
        collections = moveableGridTransform.GetComponents<BeatmapObjectContainerCollection>();
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
        foreach(BeatmapObjectContainerCollection collection in instance.collections)
        {
            SelectedObjects.AddRange(collection.LoadedContainers.Where(x => x.objectData._time >= start.objectData._time &&
                x.objectData._time <= end.objectData._time && !IsObjectSelected(x) &&
                x.objectData.beatmapType == start.objectData.beatmapType));
        }
        RefreshSelectionMaterial();
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
    internal static void RefreshSelectionMaterial(bool triggersAction = true)
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
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionChangedAction(SelectedObjects));
    }

    #endregion

    #region Manipulation
    
    /// <summary>
    /// Deletes and clears the current selection.
    /// </summary>
    public void Delete(bool triggersAction = true)
    {
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionDeletedAction(SelectedObjects));
        foreach (BeatmapObjectContainer con in SelectedObjects)
            foreach (BeatmapObjectContainerCollection container in collections) container.DeleteObject(con);
        SelectedObjects.Clear();
        RefreshMap();
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
        foreach (BeatmapObjectContainer con in CopiedObjects)
        {
            Select(con, true);
            con.objectData._time = con.objectData._time - atsc.CurrentBeat;
        }
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
        foreach (BeatmapObjectContainer con in CopiedObjects)
        {
            float newTime = (con.objectData._time + atsc.CurrentBeat);
            BeatmapObjectContainer pastedContainer = null;
            if (con is BeatmapNoteContainer)
            {
                BeatmapNote data = new BeatmapNote((con as BeatmapNoteContainer).mapNoteData.ConvertToJSON());
                data._time = newTime;
                NotesContainer notes = collections.Where(x => x is NotesContainer).FirstOrDefault() as NotesContainer;
                pastedContainer = notes?.SpawnObject(data);
            }
            if (con is BeatmapObstacleContainer)
            {
                BeatmapObstacle data = new BeatmapObstacle((con as BeatmapObstacleContainer).obstacleData.ConvertToJSON());
                data._time = newTime;
                ObstaclesContainer obstacles = collections.Where(x => x is ObstaclesContainer).FirstOrDefault() as ObstaclesContainer;
                pastedContainer = obstacles?.SpawnObject(data);
            }
            if (con is BeatmapEventContainer)
            {
                MapEvent data = new MapEvent((con as BeatmapEventContainer).eventData.ConvertToJSON());
                data._time = newTime;
                EventsContainer events = collections.Where(x => x is EventsContainer).FirstOrDefault() as EventsContainer;
                pastedContainer = events?.SpawnObject(data);
            }
            PastedObjects.Add(pastedContainer);
        }
        CopiedObjects.Clear();
        List<BeatmapObjectContainer> previouslySelected = new List<BeatmapObjectContainer>(SelectedObjects);
        DeselectAll();
        SelectedObjects.AddRange(PastedObjects);
        RefreshSelectionMaterial(false);
        BeatmapActionContainer.AddAction(new SelectionPastedAction(SelectedObjects, previouslySelected));
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

    public void ShiftSelection(int leftRight, int upDown)
    {
        foreach(BeatmapObjectContainer con in SelectedObjects)
        {
            if (con is BeatmapNoteContainer note)
            {
                note.mapNoteData._lineIndex += leftRight;
                note.mapNoteData._lineLayer += upDown;
            }
            else if (con is BeatmapObstacleContainer obstacle)
                obstacle.obstacleData._lineIndex += leftRight;
            else if (con is BeatmapEventContainer e)
            {
                e.eventData._type += leftRight;
                if (e.eventData._type < 0) e.eventData._type = 0;
            }
            con.UpdateGridPosition();
        }
        RefreshMap();
    }

    public static void RefreshMap()
    {
        foreach (BeatmapObjectContainerCollection collection in instance.collections) collection.SortObjects();
        if (BeatSaberSongContainer.Instance.map != null)
        {
            List<BeatmapNote> newNotes = new List<BeatmapNote>();
            foreach (BeatmapObjectContainer n in instance.collections.Where(x => x is NotesContainer).FirstOrDefault()?.LoadedContainers)
                newNotes.Add((n as BeatmapNoteContainer).mapNoteData);
            List<BeatmapObstacle> newObstacles = new List<BeatmapObstacle>();
            foreach (BeatmapObjectContainer n in instance.collections.Where(x => x is ObstaclesContainer).FirstOrDefault()?.LoadedContainers)
                newObstacles.Add((n as BeatmapObstacleContainer).obstacleData);
            List<MapEvent> newEvents = new List<MapEvent>();
            foreach (BeatmapObjectContainer n in instance.collections.Where(x => x is EventsContainer).FirstOrDefault()?.LoadedContainers)
                newEvents.Add((n as BeatmapEventContainer).eventData);
            BeatSaberSongContainer.Instance.map._notes = newNotes;
            BeatSaberSongContainer.Instance.map._obstacles = newObstacles;
            BeatSaberSongContainer.Instance.map._events = newEvents;
        }
    }

    #endregion

}
