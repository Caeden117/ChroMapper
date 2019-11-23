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
    public static List<BeatmapObject> CopiedObjects = new List<BeatmapObject>();

    public static Action<BeatmapObjectContainer> ObjectWasSelectedEvent;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Transform moveableGridTransform;
    internal BeatmapObjectContainerCollection[] collections;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;

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
    public static void Select(BeatmapObjectContainer container, bool AddsToSelection = false, bool AutomaticallyRefreshes = true)
    {
        if (IsObjectSelected(container)) return; //Cant select an already selected object now, can ya?
        if (!AddsToSelection) DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        SelectedObjects.Add(container);
        if (AutomaticallyRefreshes) RefreshSelectionMaterial();
        ObjectWasSelectedEvent.Invoke(container);
        Debug.Log("Selected " + container.objectData.beatmapType.ToString());
    }

    /// <summary>
    /// Deselects a container if it is currently selected
    /// </summary>
    /// <param name="container">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObjectContainer container)
    {
        SelectedObjects.RemoveAll(x => x == null);
        if (container == null || !IsObjectSelected(container)) return;
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
        SelectedObjects.RemoveAll(x => x == null);
        foreach (BeatmapObjectContainer con in SelectedObjects) con.OutlineVisible = false;
        SelectedObjects.Clear();
    }

    /// <summary>
    /// Can be very taxing. Use sparringly.
    /// </summary>
    internal static void RefreshSelectionMaterial(bool triggersAction = true)
    {
        SelectedObjects.RemoveAll(x => x == null);
        foreach (BeatmapObjectContainer con in SelectedObjects) con.SetOutlineColor(instance.selectedColor);
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
        CopiedObjects.Clear();
        SelectedObjects = SelectedObjects.OrderBy(x => x.objectData._time).ToList();
        float firstTime = SelectedObjects.First().objectData._time;
        foreach (BeatmapObjectContainer con in SelectedObjects) con.SetOutlineColor(instance.copiedColor);
        if (cut) Delete();
    }

    /// <summary>
    /// Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste(bool triggersAction = true)
    {
        DeselectAll();
        CopiedObjects = CopiedObjects.OrderBy((x) => x._time).ToList();
        List<BeatmapObjectContainer> pasted = new List<BeatmapObjectContainer>();
        foreach (BeatmapObject data in CopiedObjects)
        {
            if (data == null) continue;
            float newTime = data._time + atsc.CurrentBeat;
            BeatmapObject newData = BeatmapObject.GenerateCopy(data);
            newData._time = newTime;
            BeatmapObjectContainer pastedContainer = collections.Where(x => x.ContainerType == newData.beatmapType).FirstOrDefault()?.SpawnObject(newData, out _);
            pasted.Add(pastedContainer);
        }
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, CopiedObjects, atsc.CurrentBeat));
        foreach (BeatmapObjectContainer obj in pasted) Select(obj, true, false);
        RefreshSelectionMaterial(false);
        RefreshMap();
        Debug.Log("Pasted!");
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        foreach (BeatmapObjectContainer con in SelectedObjects)
        {
            con.objectData._time += beats;
            if (snapObjects)
                con.objectData._time = Mathf.Round(beats / (1f / atsc.gridMeasureSnapping)) * (1f / atsc.gridMeasureSnapping);
            con.UpdateGridPosition();
        }
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        foreach(BeatmapObjectContainer con in SelectedObjects)
        {
            if (con is BeatmapNoteContainer note)
            {
                if (note.mapNoteData._lineIndex >= 1000)
                {
                    note.mapNoteData._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (note.mapNoteData._lineIndex < 1000) note.mapNoteData._lineIndex = 1000;
                }
                else if (note.mapNoteData._lineIndex <= -1000)
                {
                    note.mapNoteData._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (note.mapNoteData._lineIndex > -1000) note.mapNoteData._lineIndex = -1000;
                }
                else note.mapNoteData._lineIndex += leftRight;
                note.mapNoteData._lineLayer += upDown;
            }
            else if (con is BeatmapObstacleContainer obstacle)
            {
                if (obstacle.obstacleData._lineIndex >= 1000)
                {
                    obstacle.obstacleData._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (obstacle.obstacleData._lineIndex < 1000) obstacle.obstacleData._lineIndex = 1000;
                }
                else if (obstacle.obstacleData._lineIndex <= -1000)
                {
                    obstacle.obstacleData._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (obstacle.obstacleData._lineIndex > -1000) obstacle.obstacleData._lineIndex = -1000;
                }
                else obstacle.obstacleData._lineIndex += leftRight;
            }
            else if (con is BeatmapEventContainer e)
            {
                if (e.eventData._customData != null && e.eventData._customData["_propID"] != null)
                    e.eventData._customData["_propID"] = e.eventData._customData["_propID"].AsInt + leftRight;
                e.eventData._type += leftRight;
                if (e.eventData._type < 0) e.eventData._type = 0;
            }
            con.UpdateGridPosition();
        }
        RefreshMap();
    }

    public void AssignTrack()
    {
        PersistentUI.Instance.ShowInputBox("Assign the selected objects to a track ID.\n\n" +
            "If you dont know what you're doing, turn back now.", HandleTrackAssign);
    }

    private void HandleTrackAssign(string res)
    {
        if (res is null) return;
        if (res == "")
        {
            foreach (BeatmapObjectContainer obj in SelectedObjects)
            {
                if (obj.objectData._customData == null) continue;
                BeatmapObject copy = BeatmapObject.GenerateCopy(obj.objectData);
                copy._customData.Remove("track");
                obj.objectData = copy;
            }
        }
        foreach (BeatmapObjectContainer obj in SelectedObjects)
        {
            BeatmapObject copy = BeatmapObject.GenerateCopy(obj.objectData);
            if (copy._customData == null) obj.objectData._customData = new SimpleJSON.JSONObject();
            copy._customData["track"] = res;
            obj.objectData = copy;
        }
    }

    public static void RefreshMap()
    {
        if (BeatSaberSongContainer.Instance.map != null)
        {
            Dictionary<BeatmapObject.Type, List<BeatmapObject>> newObjects = new Dictionary<BeatmapObject.Type, List<BeatmapObject>>();
            foreach (BeatmapObjectContainerCollection collection in instance.collections)
            {
                collection.SortObjects();
                newObjects.Add(collection.ContainerType, collection.LoadedContainers.Select(x => x.objectData).ToList());
            }
            BeatSaberSongContainer.Instance.map._notes = newObjects[BeatmapObject.Type.NOTE].Cast<BeatmapNote>().ToList();
            BeatSaberSongContainer.Instance.map._obstacles = newObjects[BeatmapObject.Type.OBSTACLE].Cast<BeatmapObstacle>().ToList();
            BeatSaberSongContainer.Instance.map._events = newObjects[BeatmapObject.Type.EVENT].Cast<MapEvent>().ToList();
            BeatSaberSongContainer.Instance.map._customEvents = newObjects[BeatmapObject.Type.CUSTOM_EVENT].Cast<BeatmapCustomEvent>().ToList();
        }
    }

    #endregion

}
