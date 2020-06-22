using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// Big boi master class for everything Selection.
/// </summary>
public class SelectionController : MonoBehaviour, CMInput.ISelectingActions, CMInput.IModifyingSelectionActions
{

    public static HashSet<BeatmapObject> SelectedObjects = new HashSet<BeatmapObject>();
    public static HashSet<BeatmapObject> CopiedObjects = new HashSet<BeatmapObject>();

    public static Color SelectedColor => instance.selectedColor;
    public static Color CopiedColor => instance.copiedColor;

    public static Action<BeatmapObject> ObjectWasSelectedEvent;
    public static Action<IEnumerable<BeatmapObject>> SelectionPastedEvent;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;

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
    public static bool IsObjectSelected(BeatmapObject container) => SelectedObjects.Contains(container);

    #endregion

    #region Selection

    /// <summary>
    /// Select an individual container.
    /// </summary>
    /// <param name="container">The container to select.</param>
    /// <param name="AddsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="AddActionEvent">If an action event to undo the selection should be made</param>
    public static void Select(BeatmapObject obj, bool AddsToSelection = false, bool AutomaticallyRefreshes = true, bool AddActionEvent = true)
    {
        if (!AddsToSelection) DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        SelectedObjects.Add(obj);
        if (BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).LoadedContainers.TryGetValue(obj, out BeatmapObjectContainer container))
        {
            container.SetOutlineColor(instance.selectedColor);
        }
        if (AddActionEvent) ObjectWasSelectedEvent.Invoke(obj);
    }

    /// <summary>
    /// Deselects a container if it is currently selected
    /// </summary>
    /// <param name="obj">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObject obj)
    {
        SelectedObjects.Remove(obj);
        if (BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType).LoadedContainers.TryGetValue(obj, out BeatmapObjectContainer container))
        {
            container.OutlineVisible = false;
        }
    }

    /// <summary>
    /// Deselect all selected objects.
    /// </summary>
    public static void DeselectAll()
    {
        foreach (BeatmapObject obj in SelectedObjects.ToArray())
        {
            Deselect(obj);
        }
    }

    /// <summary>
    /// Can be very taxing. Use sparringly.
    /// </summary>
    internal static void RefreshSelectionMaterial(bool triggersAction = true)
    {
        foreach (BeatmapObject data in SelectedObjects)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.OutlineVisible = true;
                con.SetOutlineColor(instance.selectedColor);
            }
        }
        //if (triggersAction) BeatmapActionContainer.AddAction(new SelectionChangedAction(SelectedObjects));
    }

    #endregion

    #region Manipulation
    
    /// <summary>
    /// Deletes and clears the current selection.
    /// </summary>
    public void Delete(bool triggersAction = true)
    {
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionDeletedAction(SelectedObjects));
        foreach (BeatmapObject con in SelectedObjects)
        {
            BeatmapObjectContainerCollection.GetCollectionForType(con.beatmapType).DeleteObject(con, false, false);
        }
        BeatmapObjectContainerCollection.RefreshAllPools();
        SelectedObjects.Clear();
    }
    
    /// <summary>
    /// Copies the current selection for later Pasting.
    /// </summary>
    /// <param name="cut">Whether or not to delete the original selection after copying them.</param>
    public void Copy(bool cut = false)
    {
        if (!HasSelectedObjects()) return;
        Debug.Log("Copied!");
        CopiedObjects.Clear();
        float firstTime = SelectedObjects.OrderBy(x => x._time).First()._time;
        foreach (BeatmapObject data in SelectedObjects)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.SetOutlineColor(instance.copiedColor);
            }
            BeatmapObject copy = BeatmapObject.GenerateCopy(data);
            copy._time -= firstTime;
            CopiedObjects.Add(copy);
        }
        if (cut) Delete();
    }

    /// <summary>
    /// Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste(bool triggersAction = true)
    {
        DeselectAll();
        HashSet<BeatmapObject> pasted = new HashSet<BeatmapObject>();
        Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection> collections = new Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection>();
        BeatmapObjectContainerCollection bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType(BeatmapObject.Type.BPM_CHANGE);
        BeatmapBPMChange lastBPMChange = (bpmChanges as BPMChangesContainer).FindLastBPM(atsc.CurrentBeat, true);
        foreach (BeatmapObject data in CopiedObjects)
        {
            if (data == null) continue;
            float bpmTime = data._time * (atsc.song.beatsPerMinute / (lastBPMChange?._BPM ?? atsc.song.beatsPerMinute));
            float newTime = bpmTime + atsc.CurrentBeat;
            BeatmapObject newData = BeatmapObject.GenerateCopy(data);
            newData._time = newTime;
            if (!collections.TryGetValue(newData.beatmapType, out BeatmapObjectContainerCollection collection))
            {
                collection = BeatmapObjectContainerCollection.GetCollectionForType(newData.beatmapType);
                collection.RemoveConflictingObjects(CopiedObjects.Where(x => x.beatmapType == collection.ContainerType));
                collections.Add(newData.beatmapType, collection);
            }
            collection.SpawnObject(newData, false, false);
            Select(newData, true, false, false);
            pasted.Add(newData);
        }
        foreach (BeatmapObjectContainerCollection collection in collections.Values)
        {
            collection.RefreshPool();
        }
        if (CopiedObjects.Any(x => (x is MapEvent e) && e.IsRotationEvent))
        {
            tracksManager.RefreshTracks();
        }
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, atsc.CurrentBeat));
        SelectionPastedEvent?.Invoke(pasted);
        RefreshSelectionMaterial(false);

        if (eventPlacement.objectContainerCollection.PropagationEditing)
            eventPlacement.objectContainerCollection.PropagationEditing = eventPlacement.objectContainerCollection.PropagationEditing;
        Debug.Log("Pasted!");
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        foreach (BeatmapObject data in SelectedObjects)
        {
            data._time += beats;
            if (snapObjects)
                data._time = Mathf.Round(beats / (1f / atsc.gridMeasureSnapping)) * (1f / atsc.gridMeasureSnapping);
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.UpdateGridPosition();
            }
        }
        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        foreach(BeatmapObject data in SelectedObjects)
        {
            if (data is BeatmapNote note)
            {
                if (note._lineIndex >= 1000)
                {
                    note._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (note._lineIndex < 1000) note._lineIndex = 1000;
                }
                else if (note._lineIndex <= -1000)
                {
                    note._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (note._lineIndex > -1000) note._lineIndex = -1000;
                }
                else note._lineIndex += leftRight;
                note._lineLayer += upDown;
            }
            else if (data is BeatmapObstacle obstacle)
            {
                if (obstacle._lineIndex >= 1000)
                {
                    obstacle._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (obstacle._lineIndex < 1000) obstacle._lineIndex = 1000;
                }
                else if (obstacle._lineIndex <= -1000)
                {
                    obstacle._lineIndex += Mathf.RoundToInt((1f / atsc.gridMeasureSnapping) * 1000 * leftRight);
                    if (obstacle._lineIndex > -1000) obstacle._lineIndex = -1000;
                }
                else obstacle._lineIndex += leftRight;
            }
            else if (data is MapEvent e)
            {
                if (eventPlacement.objectContainerCollection.PropagationEditing)
                {
                    int pos = -1 + leftRight;
                    if (data._customData != null && data._customData["_propID"].IsNumber)
                        pos = (data?._customData["_propID"]?.AsInt ?? -1) + leftRight;
                    if (pos < -1) pos = -1;
                    EventsContainer events = eventPlacement.objectContainerCollection;
                    int lightPropMax = events.platformDescriptor.LightingManagers[events.EventTypeToPropagate].LightsGroupedByZ.Length;
                    if (pos > lightPropMax) pos = lightPropMax;
                    if (pos == -1)
                    {
                        data._customData?.Remove("_propID");
                    }
                    else
                    {
                        data._customData["_propID"] = pos;
                    }
                }
                else
                {
                    EventsContainer container = BeatmapObjectContainerCollection.GetCollectionForType(e.beatmapType) as EventsContainer;
                    if (e._customData != null && e._customData["_propID"] != null)
                        e._customData["_propID"] = e._customData["_propID"].AsInt + leftRight;
                    int modified = BeatmapEventContainer.EventTypeToModifiedType(e._type);
                    modified += leftRight;
                    if (modified < 0) modified = 0;
                    if (modified > container.EventTypePropagationSize) modified = 15;
                    e._type = BeatmapEventContainer.ModifiedTypeToEventType(modified);
                }
            }
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.UpdateGridPosition();
            }
            if (eventPlacement.objectContainerCollection.PropagationEditing) 
                eventPlacement.objectContainerCollection.PropagationEditing = eventPlacement.objectContainerCollection.PropagationEditing;
        }
        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    /// <summary>
    /// Applies objects to the loaded <see cref="BeatSaberMap"/>. Should be done before saving the map.
    /// </summary>
    public static void RefreshMap()
    {
        if (BeatSaberSongContainer.Instance.map != null)
        {
            Dictionary<BeatmapObject.Type, IEnumerable<BeatmapObject>> newObjects = new Dictionary<BeatmapObject.Type, IEnumerable<BeatmapObject>>();
            foreach (int num in Enum.GetValues(typeof(BeatmapObject.Type)))
            {
                BeatmapObject.Type type = (BeatmapObject.Type)num;
                BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
                if (collection is null) continue;
                newObjects.Add(type, collection.GrabSortedObjects());
            }
            if (Settings.Instance.Load_Notes)
                BeatSaberSongContainer.Instance.map._notes = newObjects[BeatmapObject.Type.NOTE].Cast<BeatmapNote>().ToList();
            if (Settings.Instance.Load_Obstacles)
                BeatSaberSongContainer.Instance.map._obstacles = newObjects[BeatmapObject.Type.OBSTACLE].Cast<BeatmapObstacle>().ToList();
            if (Settings.Instance.Load_Events)
                BeatSaberSongContainer.Instance.map._events = newObjects[BeatmapObject.Type.EVENT].Cast<MapEvent>().ToList();
            if (Settings.Instance.Load_Others)
            {
                BeatSaberSongContainer.Instance.map._BPMChanges = newObjects[BeatmapObject.Type.BPM_CHANGE].Cast<BeatmapBPMChange>().ToList();
                BeatSaberSongContainer.Instance.map._customEvents = newObjects[BeatmapObject.Type.CUSTOM_EVENT].Cast<BeatmapCustomEvent>().ToList();
            }
        }
    }

    #endregion

    public void OnDeselectAll(InputAction.CallbackContext context)
    {
        if (context.performed) DeselectAll();
    }

    public void OnPaste(InputAction.CallbackContext context)
    {
        if (context.performed) Paste();
    }

    public void OnDeleteObjects(InputAction.CallbackContext context)
    {
        if (context.performed) Delete();
    }

    public void OnCopy(InputAction.CallbackContext context)
    {
        if (context.performed) Copy();
    }

    public void OnCut(InputAction.CallbackContext context)
    {
        if (context.performed) Copy(true);
    }

    public void OnShiftinTime(InputAction.CallbackContext context)
    {
        if (!context.performed || !KeybindsController.ShiftHeld) return;
        float value = context.ReadValue<float>();
        MoveSelection(value * (1f / atsc.gridMeasureSnapping));
    }

    public void OnShiftinPlace(InputAction.CallbackContext context)
    {
        if (!context.performed || !KeybindsController.CtrlHeld) return;
        Vector2 movement = context.ReadValue<Vector2>();
        Debug.Log(movement);
        ShiftSelection(Mathf.RoundToInt(movement.x), Mathf.RoundToInt(movement.y));
    }

}
