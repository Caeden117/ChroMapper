﻿using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEngine.InputSystem;
using SimpleJSON;

/// <summary>
/// Big boi master class for everything Selection.
/// </summary>
public class SelectionController : MonoBehaviour, CMInput.ISelectingActions, CMInput.IModifyingSelectionActions
{

    public static HashSet<BeatmapObject> SelectedObjects = new HashSet<BeatmapObject>();
    public static HashSet<BeatmapObject> CopiedObjects = new HashSet<BeatmapObject>();
    private static float copiedBPM = 100;

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

    /// <summary>
    /// Shows what types of object groups are in the passed in group of objects through output parameters.
    /// </summary>
    /// <param name="objects">Enumerable group of objects</param>
    /// <param name="hasNoteOrObstacle">Whether or not an object is in the note or obstacle group</param>
    /// <param name="hasEvent">Whether or not an object is in the event group</param>
    /// <param name="hasBpmChange">Whether or not an object is in the bpm change group</param>
    public static void GetObjectTypes(IEnumerable<BeatmapObject> objects, out bool hasNoteOrObstacle, out bool hasEvent, out bool hasBpmChange)
    {
        hasNoteOrObstacle = false;
        hasEvent = false;
        hasBpmChange = false;
        foreach(BeatmapObject beatmapObject in objects){
            switch (beatmapObject.beatmapType)
            {
                case BeatmapObject.Type.NOTE:
                case BeatmapObject.Type.OBSTACLE:
                case BeatmapObject.Type.CUSTOM_NOTE:
                    hasNoteOrObstacle = true;
                    break;
                case BeatmapObject.Type.EVENT:
                case BeatmapObject.Type.CUSTOM_EVENT:
                    hasEvent = true;
                    break;
                case BeatmapObject.Type.BPM_CHANGE:
                    hasBpmChange = true;
                    break;
            }
        }
    }

    /// <summary>
    /// Invokes a callback for all objects between a time by group
    /// </summary>
    /// <param name="start">Start time in beats</param>
    /// <param name="start">End time in beats</param>
    /// <param name="hasNoteOrObstacle">Whether or not to include the note or obstacle group</param>
    /// <param name="hasEvent">Whether or not to include the event group</param>
    /// <param name="hasBpmChange">Whether or not to include the bpm change group</param>
    /// <param name="callback">Callback with an object container and the collection it belongs to</param>
    public static void ForEachObjectBetweenTimeByGroup(float start, float end, bool hasNoteOrObstacle, bool hasEvent, bool hasBpmChange, Action<BeatmapObjectContainerCollection, BeatmapObjectContainer> callback)
    {
        List<BeatmapObject.Type> clearTypes = new List<BeatmapObject.Type>();
        if (hasNoteOrObstacle)
            clearTypes.AddRange(new BeatmapObject.Type[] { BeatmapObject.Type.NOTE, BeatmapObject.Type.OBSTACLE, BeatmapObject.Type.CUSTOM_NOTE });
        if (hasNoteOrObstacle && !hasEvent)
            clearTypes.Add(BeatmapObject.Type.EVENT);//for rotation events
        if (hasEvent)
            clearTypes.AddRange(new BeatmapObject.Type[] { BeatmapObject.Type.EVENT, BeatmapObject.Type.CUSTOM_EVENT, BeatmapObject.Type.BPM_CHANGE });
        float epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        foreach (BeatmapObject.Type type in clearTypes)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
            if (collection == null) continue;

            foreach (KeyValuePair<BeatmapObject, BeatmapObjectContainer> toCheck in collection.LoadedContainers.Where(x => x.Key._time > start - epsilon && x.Key._time < end + epsilon))
            {
                if (!hasEvent && toCheck.Key is MapEvent mapEvent && !mapEvent.IsRotationEvent) //Includes only rotation events when neither of the two objects are events
                    continue;
                callback?.Invoke(collection, toCheck.Value);
            }
        }
    }

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
    /// Selects objects between 2 objects, sorted by group.
    /// </summary>
    /// <param name="first">The beatmap object at the one end of the selection.</param>
    /// <param name="second">The beatmap object at the other end of the selection</param>
    /// <param name="AddsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="AddActionEvent">If an action event to undo the selection should be made</param>
    public static void SelectBetween(BeatmapObject first, BeatmapObject second, bool AddsToSelection = false, bool AddActionEvent = true)
    {
        if (!AddsToSelection) DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        if (first._time > second._time)
            (first, second) = (second, first);
        GetObjectTypes(new BeatmapObject[] { first, second }, out bool hasNoteOrObstacle, out bool hasEvent, out bool hasBpmChange);
        ForEachObjectBetweenTimeByGroup(first._time, second._time, hasNoteOrObstacle, hasEvent, hasBpmChange, (collection, container) =>
        {
            if (SelectedObjects.Contains(container.objectData)) return;
            SelectedObjects.Add(container.objectData);
            container.SetOutlineColor(instance.selectedColor);
            if (AddActionEvent) ObjectWasSelectedEvent.Invoke(container.objectData);
        });
    }

    /// <summary>
    /// Deselects a container if it is currently selected
    /// </summary>
    /// <param name="obj">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObject obj)
    {
        SelectedObjects.Remove(obj);
        BeatmapObjectContainer container = null;
        if (BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType)?.LoadedContainers?.TryGetValue(obj, out container) ?? false)
        {
            if (container != null)
            {
                container.OutlineVisible = false;
            }
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
        foreach (BeatmapObject con in SelectedObjects.ToList())
        {
            BeatmapObjectContainerCollection.GetCollectionForType(con.beatmapType).DeleteObject(con, false, false);
        }
        BeatmapObjectContainerCollection.RefreshAllPools();
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
        var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
        BeatmapBPMChange lastBPMChange = bpmChanges.FindLastBPM(atsc.CurrentBeat, true);
        copiedBPM = lastBPMChange?._BPM ?? atsc.song.beatsPerMinute;
    }

    /// <summary>
    /// Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste(bool triggersAction = true, bool overwriteSection = false)
    {
        DeselectAll();
        // Set up stuff that we need
        List<BeatmapObject> pasted = new List<BeatmapObject>();
        Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection> collections = new Dictionary<BeatmapObject.Type, BeatmapObjectContainerCollection>();
        // Grab the last BPM Change to warp distances between copied objects and maintain BPM.
        var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
        BeatmapBPMChange lastBPMChange = bpmChanges.FindLastBPM(atsc.CurrentBeat, true);
        // This first loop creates copy of the data to be pasted.
        foreach (BeatmapObject data in CopiedObjects)
        {
            if (data == null) continue;
            float bpmTime = data._time * (copiedBPM / (lastBPMChange?._BPM ?? copiedBPM));
            float newTime = bpmTime + atsc.CurrentBeat;
            BeatmapObject newData = BeatmapObject.GenerateCopy(data);
            newData._time = newTime;
            if (!collections.TryGetValue(newData.beatmapType, out BeatmapObjectContainerCollection collection))
            {
                collection = BeatmapObjectContainerCollection.GetCollectionForType(newData.beatmapType);
                collections.Add(newData.beatmapType, collection);
            }
            pasted.Add(newData);
        }
        List<BeatmapObject> totalRemoved = new List<BeatmapObject>();
        // We remove conflicting objects with our to-be-pasted objects.
        foreach (var kvp in collections)
        {
            kvp.Value.RemoveConflictingObjects(pasted.Where(x => x.beatmapType == kvp.Key), out var conflicting);
            totalRemoved.AddRange(conflicting);
        }
        // While we're at it, we will also overwrite the entire section if we have to.
        if (overwriteSection)
        {
            float start = pasted.First()._time;
            float end = pasted.First()._time;
            foreach (BeatmapObject beatmapObject in pasted)
            {
                if (start > beatmapObject._time)
                    start = beatmapObject._time;
                if (end < beatmapObject._time)
                    end = beatmapObject._time;
            }
            GetObjectTypes(pasted, out bool hasNoteOrObstacle, out bool hasEvent, out bool hasBpmChange);
            List<(BeatmapObjectContainerCollection, BeatmapObject)> toRemove = new List<(BeatmapObjectContainerCollection, BeatmapObject)>();
            ForEachObjectBetweenTimeByGroup(start, end, hasNoteOrObstacle, hasEvent, hasBpmChange, (collection, container) =>
            {
                if (pasted.Contains(container.objectData)) return;
                toRemove.Add((collection, container.objectData));
            });
            foreach((BeatmapObjectContainerCollection, BeatmapObject) pair in toRemove)
            {
                BeatmapObjectContainerCollection collection = pair.Item1;
                BeatmapObject beatmapObject = pair.Item2;
                collection.DeleteObject(beatmapObject, false);
                totalRemoved.Add(beatmapObject);
            }
        }
        // We then spawn our pasted objects into the map and select them.
        foreach (BeatmapObject data in pasted)
        {
            collections[data.beatmapType].SpawnObject(data, false, false);
            Select(data, true, false, false);
        }
        foreach (BeatmapObjectContainerCollection collection in collections.Values)
        {
            collection.RefreshPool();
        }
        if (CopiedObjects.Any(x => (x is MapEvent e) && e.IsRotationEvent))
        {
            tracksManager.RefreshTracks();
        }
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, totalRemoved));
        SelectionPastedEvent?.Invoke(pasted);
        RefreshSelectionMaterial(false);

        if (eventPlacement.objectContainerCollection.PropagationEditing)
            eventPlacement.objectContainerCollection.PropagationEditing = eventPlacement.objectContainerCollection.PropagationEditing;
        Debug.Log("Pasted!");
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        List<BeatmapAction> allActions = new List<BeatmapAction>();
        foreach (BeatmapObject data in SelectedObjects)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(data);
            data._time += beats;
            if (snapObjects)
                data._time = Mathf.Round(beats / (1f / atsc.gridMeasureSnapping)) * (1f / atsc.gridMeasureSnapping);
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.UpdateGridPosition();
            }
            allActions.Add(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(data), original));
        }
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, false, "Shifted a selection of objects."));
        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        List<BeatmapAction> allActions = new List<BeatmapAction>();
        foreach(BeatmapObject data in SelectedObjects)
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(data);
            if (data is BeatmapNote note)
            {
                if (note._customData is null || !note._customData.HasKey("_position"))
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
                else
                {
                    if (data._customData.HasKey("_position"))
                    {
                        data._customData["_position"][0] += (1f / atsc.gridMeasureSnapping) * leftRight;
                        data._customData["_position"][1] += (1f / atsc.gridMeasureSnapping) * upDown;
                    }
                }
            }
            else if (data is BeatmapObstacle obstacle)
            {
                if (!obstacle.IsNoodleExtensionsWall)
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
                else
                {
                    if (data._customData.HasKey("_position"))
                    {
                        data._customData["_position"][0] += (1f / atsc.gridMeasureSnapping) * leftRight;
                        data._customData["_position"][1] += (1f / atsc.gridMeasureSnapping) * upDown;
                    }
                }
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
                        if (data._customData is null || data._customData.Count == 0 || data._customData.Children.Count() == 0)
                        {
                            data._customData = new JSONObject();
                        }
                        data._customData["_propID"] = pos;
                    }
                }
                else
                {
                    var container = BeatmapObjectContainerCollection.GetCollectionForType<EventsContainer>(e.beatmapType);
                    if (e._customData != null && e._customData["_propID"] != null && container.PropagationEditing)
                    {
                        e._customData["_propID"] = e._customData["_propID"].AsInt + leftRight;
                        if (e._customData["_propID"].AsInt < 0)
                        {
                            e._customData.Remove("_propID");
                        }
                    }
                    else if (container.PropagationEditing && (e._customData == null | e._customData.Children.Count() == 0))
                    {
                        e._customData = new JSONObject();
                        e._customData.Add("_propID", 0);
                    }
                    else
                    {
                        int modified = BeatmapEventContainer.EventTypeToModifiedType(e._type);
                        modified += leftRight;
                        if (modified < 0) modified = 0;
                        if (modified > 15) modified = 15;
                        e._type = BeatmapEventContainer.ModifiedTypeToEventType(modified);
                    }
                }
            }
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.UpdateGridPosition();
            }
            allActions.Add(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(data), original));
            if (eventPlacement.objectContainerCollection.PropagationEditing) 
                eventPlacement.objectContainerCollection.PropagationEditing = eventPlacement.objectContainerCollection.PropagationEditing;
        }
        foreach (BeatmapObject unique in SelectedObjects.DistinctBy(x => x.beatmapType))
        {
            BeatmapObjectContainerCollection.GetCollectionForType(unique.beatmapType).RefreshPool(true);
        }
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, false, "Shifted a selection of objects."));
        tracksManager.RefreshTracks();
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
        if (context.performed) Paste(true, KeybindsController.ShiftHeld);
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
