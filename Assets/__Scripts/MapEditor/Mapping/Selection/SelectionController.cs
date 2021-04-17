using System.Collections.Generic;
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
    public static Action SelectionChangedEvent;
    public static Action<IEnumerable<BeatmapObject>> SelectionPastedEvent;

    private static SelectionController instance;

    [SerializeField] private AudioTimeSyncController atsc;
    [SerializeField] private Material selectionMaterial;
    [SerializeField] private Transform moveableGridTransform;
    [SerializeField] private Color selectedColor;
    [SerializeField] private Color copiedColor;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;

    [SerializeField] private CreateEventTypeLabels labels;

    private bool shiftInTime = false;
    private bool shiftInPlace = false;

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
    public static void ForEachObjectBetweenTimeByGroup(float start, float end, bool hasNoteOrObstacle, bool hasEvent, bool hasBpmChange, Action<BeatmapObjectContainerCollection, BeatmapObject> callback)
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

            foreach (BeatmapObject toCheck in collection.LoadedObjects.Where(x => x._time > start - epsilon && x._time < end + epsilon))
            {
                if (!hasEvent && toCheck is MapEvent mapEvent && !mapEvent.IsRotationEvent) //Includes only rotation events when neither of the two objects are events
                    continue;
                callback?.Invoke(collection, toCheck);
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
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.beatmapType);

        if (!collection.LoadedObjects.Contains(obj))
            return;

        SelectedObjects.Add(obj);
        if (collection.LoadedContainers.TryGetValue(obj, out BeatmapObjectContainer container))
        {
            container.SetOutlineColor(instance.selectedColor);
        }
        if (AddActionEvent)
        {
            ObjectWasSelectedEvent.Invoke(obj);
            SelectionChangedEvent?.Invoke();
        }
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
        ForEachObjectBetweenTimeByGroup(first._time, second._time, hasNoteOrObstacle, hasEvent, hasBpmChange, (collection, beatmapObject) =>
        {
            if (SelectedObjects.Contains(beatmapObject)) return;
            SelectedObjects.Add(beatmapObject);
            if (collection.LoadedContainers.TryGetValue(beatmapObject, out BeatmapObjectContainer container))
            {
                container.SetOutlineColor(instance.selectedColor);
            }
            if (AddActionEvent) ObjectWasSelectedEvent.Invoke(beatmapObject);
        });
        if (AddActionEvent)
            SelectionChangedEvent?.Invoke();
    }

    /// <summary>
    /// Deselects a container if it is currently selected
    /// </summary>
    /// <param name="obj">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObject obj, bool RemoveActionEvent = true)
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
        if (RemoveActionEvent) SelectionChangedEvent?.Invoke();
    }

    /// <summary>
    /// Deselect all selected objects.
    /// </summary>
    public static void DeselectAll(bool RemoveActionEvent = true)
    {
        foreach (BeatmapObject obj in SelectedObjects.ToArray())
        {
            Deselect(obj, false);
        }
        if (RemoveActionEvent) SelectionChangedEvent?.Invoke();
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
        IEnumerable<BeatmapObject> objects = SelectedObjects.ToArray();
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionDeletedAction(objects));
        DeselectAll();
        foreach (BeatmapObject con in objects)
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

        var lowerValue = new BeatmapBPMChange(420, atsc.CurrentBeat - 0.01f);
        var upperValue = new BeatmapBPMChange(69, atsc.CurrentBeat);

        var lastBPMChangeBeforePaste = bpmChanges.FindLastBPM(atsc.CurrentBeat, true);

        // This first loop creates copy of the data to be pasted.
        foreach (BeatmapObject data in CopiedObjects)
        {
            if (data == null) continue;

            upperValue._time = atsc.CurrentBeat + data._time;

            var bpmChangeView = bpmChanges.LoadedObjects.GetViewBetween(lowerValue, upperValue);

            float bpmTime = data._time * (copiedBPM / (lastBPMChangeBeforePaste?._BPM ?? copiedBPM));

            if (bpmChangeView.Any())
            {
                var firstBPMChange = bpmChangeView.First() as BeatmapBPMChange;

                bpmTime = firstBPMChange._time - atsc.CurrentBeat;

                for (var i = 0; i < bpmChangeView.Count - 1; i++)
                {
                    var leftBPM = bpmChangeView.ElementAt(i) as BeatmapBPMChange;
                    var rightBPM = bpmChangeView.ElementAt(i + 1) as BeatmapBPMChange;

                    bpmTime += (rightBPM._time - leftBPM._time) * (copiedBPM / leftBPM._BPM);
                }

                var lastBPMChange = bpmChangeView.Last() as BeatmapBPMChange;
                bpmTime += (atsc.CurrentBeat + data._time - lastBPMChange._time) * (copiedBPM / lastBPMChange._BPM);
            }

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
            ForEachObjectBetweenTimeByGroup(start, end, hasNoteOrObstacle, hasEvent, hasBpmChange, (collection, beatmapObject) =>
            {
                if (pasted.Contains(beatmapObject)) return;
                toRemove.Add((collection, beatmapObject));
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

            if (collection is BPMChangesContainer con)
            {
                con.RefreshGridShaders();
            }
        }
        if (CopiedObjects.Any(x => (x is MapEvent e) && e.IsRotationEvent))
        {
            tracksManager.RefreshTracks();
        }
        if (triggersAction)
        {
            BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, totalRemoved));
        }
        SelectionPastedEvent?.Invoke(pasted);
        SelectionChangedEvent?.Invoke();
        RefreshSelectionMaterial(false);

        if (eventPlacement.objectContainerCollection.PropagationEditing != EventsContainer.PropMode.Off)
            eventPlacement.objectContainerCollection.PropagationEditing = eventPlacement.objectContainerCollection.PropagationEditing;
        Debug.Log("Pasted!");
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        List<BeatmapAction> allActions = new List<BeatmapAction>();
        foreach (BeatmapObject data in SelectedObjects)
        {
            BeatmapObjectContainerCollection collection = BeatmapObjectContainerCollection.GetCollectionForType(data.beatmapType);
            BeatmapObject original = BeatmapObject.GenerateCopy(data);

            collection.LoadedObjects.Remove(data);
            data._time += beats;
            if (snapObjects)
                data._time = Mathf.Round(beats / (1f / atsc.gridMeasureSnapping)) * (1f / atsc.gridMeasureSnapping);
            collection.LoadedObjects.Add(data);

            if (collection.LoadedContainers.TryGetValue(data, out BeatmapObjectContainer con))
            {
                con.UpdateGridPosition();
            }

            if (collection is NotesContainer notesContainer)
            {
                notesContainer.RefreshSpecialAngles(original, false, false);
                notesContainer.RefreshSpecialAngles(data, false, false);
            }

            allActions.Add(new BeatmapObjectModifiedAction(data, data, original, "", true));
        }
        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true, "Shifted a selection of objects."));
        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        var allActions = SelectedObjects.AsParallel().Select(data => {
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
                var events = eventPlacement.objectContainerCollection;
                if (eventPlacement.objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Light)
                {
                    var max = events.platformDescriptor.LightingManagers[events.EventTypeToPropagate].ControllingLights.Select(x => x.lightID).Max();

                    var newId = Math.Min(e.LightId[0] + leftRight, max);
                    if (newId < 1)
                    {
                        data._customData?.Remove("_lightID");
                    }
                    else
                    {
                        data._customData["_lightID"] = newId;
                    }
                }
                else if (eventPlacement.objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Prop)
                {
                    var oldId = (e.IsLightIdEvent ? labels.LightIdsToPropId(events.EventTypeToPropagate, e.LightId) : null) ?? -1;
                    var max = events.platformDescriptor.LightingManagers[events.EventTypeToPropagate].LightsGroupedByZ.Length;
                    var newId = Math.Min(oldId + leftRight, max - 1);

                    if (newId < 0)
                    {
                        data._customData?.Remove("_lightID");
                    }
                    else
                    {
                        data.GetOrCreateCustomData()["_lightID"] = labels.PropIdToLightIdsJ(events.EventTypeToPropagate, newId);
                    }
                }
                else
                {
                    int oldType = e._type;

                    int modified = labels.EventTypeToLaneId(e._type);

                    modified += leftRight;

                    if (modified < 0) modified = 0;

                    int laneCount = labels.MaxLaneId();

                    if (modified > laneCount) modified = laneCount;

                    e._type = labels.LaneIdToEventType(modified);

                    if (e.IsLightIdEvent && !e._customData["_lightID"].IsArray)
                    {
                        var editorID = labels.LightIDToEditor(oldType, e.LightId[0]);
                        e._customData["_lightID"] = labels.EditorToLightID(e._type, editorID);
                    }
                    else if (e.IsLightIdEvent)
                    {
                        e._customData["_lightID"] = labels.PropIdToLightIdsJ(e._type, e.PropId);
                    }

                    if (e._customData != null) {
                        if (e._customData["_lightID"].Count == 0) {
                            e._customData.Remove("_lightID");
                        }

                        if (e._customData.Count == 0) {
                            e._customData = null;
                        }
                    }
                }
            }

            return new BeatmapObjectModifiedAction(data, data, original, "", true);
        }).ToList();

        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true, "Shifted a selection of objects."), true);
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
        if (context.performed) Paste(true, false);
    }

    public void OnOverwritePaste(InputAction.CallbackContext context)
    {
        if (context.performed) Paste(true, true);
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

    public void OnShiftingMovement(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        Vector2 movement = context.ReadValue<Vector2>();

        if (shiftInPlace)
        {
            ShiftSelection(Mathf.RoundToInt(movement.x), Mathf.RoundToInt(movement.y));
        }

        if (shiftInTime)
        {
            MoveSelection(movement.y * (1f / atsc.gridMeasureSnapping));
        }
    }

    public void OnActivateShiftinTime(InputAction.CallbackContext context)
    {
        shiftInTime = context.performed;
    }

    public void OnActivateShiftinPlace(InputAction.CallbackContext context)
    {
        shiftInPlace = context.performed;
    }
}
