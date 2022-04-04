using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
///     Big boi master class for everything Selection.
/// </summary>
public class SelectionController : MonoBehaviour, CMInput.ISelectingActions, CMInput.IModifyingSelectionActions
{
    public static HashSet<BeatmapObject> SelectedObjects = new HashSet<BeatmapObject>();
    public static HashSet<BeatmapObject> CopiedObjects = new HashSet<BeatmapObject>();
    private static float copiedBpm = 100;

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
    private bool shiftInPlace;

    private bool shiftInTime;

    public static Color SelectedColor => instance.selectedColor;
    public static Color CopiedColor => instance.copiedColor;

    // Use this for initialization
    private void Start()
    {
        instance = this;
        SelectedObjects.Clear();
    }

    public void OnPaste(InputAction.CallbackContext context)
    {
        if (context.performed) Paste();
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
        var movement = context.ReadValue<Vector2>();

        if (shiftInPlace) ShiftSelection(Mathf.RoundToInt(movement.x), Mathf.RoundToInt(movement.y));

        if (shiftInTime) MoveSelection(movement.y * (1f / atsc.GridMeasureSnapping));
    }

    public void OnActivateShiftinTime(InputAction.CallbackContext context) => shiftInTime = context.performed;

    public void OnActivateShiftinPlace(InputAction.CallbackContext context) => shiftInPlace = context.performed;

    public void OnDeselectAll(InputAction.CallbackContext context)
    {
        if (context.performed) DeselectAll();
    }

    #region Utils

    /// <summary>
    ///     Does the user have any selected objects?
    /// </summary>
    public static bool HasSelectedObjects() => SelectedObjects.Count > 0;

    /// <summary>
    ///     Does the user have any copied objects?
    /// </summary>
    public static bool HasCopiedObjects() => CopiedObjects.Count > 0;

    /// <summary>
    ///     Returns true if the given container is selected, and false if it's not.
    /// </summary>
    /// <param name="container">Container to check.</param>
    public static bool IsObjectSelected(BeatmapObject container) => SelectedObjects.Contains(container);

    /// <summary>
    ///     Shows what types of object groups are in the passed in group of objects through output parameters.
    /// </summary>
    /// <param name="objects">Enumerable group of objects</param>
    /// <param name="hasNoteOrObstacle">Whether or not an object is in the note or obstacle group</param>
    /// <param name="hasEvent">Whether or not an object is in the event group</param>
    /// <param name="hasBpmChange">Whether or not an object is in the bpm change group</param>
    public static void GetObjectTypes(IEnumerable<BeatmapObject> objects, out bool hasNoteOrObstacle, out bool hasEvent,
        out bool hasBpmChange)
    {
        hasNoteOrObstacle = false;
        hasEvent = false;
        hasBpmChange = false;
        foreach (var beatmapObject in objects)
        {
            switch (beatmapObject.BeatmapType)
            {
                case BeatmapObject.ObjectType.Note:
                case BeatmapObject.ObjectType.Obstacle:
                case BeatmapObject.ObjectType.CustomNote:
                    hasNoteOrObstacle = true;
                    break;
                case BeatmapObject.ObjectType.Event:
                case BeatmapObject.ObjectType.CustomEvent:
                    hasEvent = true;
                    break;
                case BeatmapObject.ObjectType.BpmChange:
                    hasBpmChange = true;
                    break;
            }
        }
    }

    /// <summary>
    ///     Invokes a callback for all objects between a time by group
    /// </summary>
    /// <param name="start">Start time in beats</param>
    /// <param name="start">End time in beats</param>
    /// <param name="hasNoteOrObstacle">Whether or not to include the note or obstacle group</param>
    /// <param name="hasEvent">Whether or not to include the event group</param>
    /// <param name="hasBpmChange">Whether or not to include the bpm change group</param>
    /// <param name="callback">Callback with an object container and the collection it belongs to</param>
    public static void ForEachObjectBetweenTimeByGroup(float start, float end, bool hasNoteOrObstacle, bool hasEvent,
        bool hasBpmChange, Action<BeatmapObjectContainerCollection, BeatmapObject> callback)
    {
        var clearTypes = new List<BeatmapObject.ObjectType>();
        if (hasNoteOrObstacle)
        {
            clearTypes.AddRange(new[]
            {
                BeatmapObject.ObjectType.Note, BeatmapObject.ObjectType.Obstacle, BeatmapObject.ObjectType.CustomNote
            });
            if (Settings.Instance.Load_MapV3)
            {
                clearTypes.AddRange(new[] { BeatmapObject.ObjectType.Arc, BeatmapObject.ObjectType.Chain });
            }
        }

        if (hasNoteOrObstacle && !hasEvent)
            clearTypes.Add(BeatmapObject.ObjectType.Event); //for rotation events
        if (hasEvent)
        {
            clearTypes.AddRange(new[]
            {
                BeatmapObject.ObjectType.Event, BeatmapObject.ObjectType.CustomEvent, BeatmapObject.ObjectType.BpmChange
            });
        }

        var epsilon = 1f / Mathf.Pow(10, Settings.Instance.TimeValueDecimalPrecision);
        foreach (var type in clearTypes)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
            if (collection == null) continue;

            foreach (var toCheck in collection.LoadedObjects.Where(x =>
                x.Time > start - epsilon && x.Time < end + epsilon))
            {
                if (!hasEvent && toCheck is MapEvent mapEvent &&
                    !mapEvent
                        .IsRotationEvent) //Includes only rotation events when neither of the two objects are events
                {
                    continue;
                }

                callback?.Invoke(collection, toCheck);
            }
        }
    }

    #endregion

    #region Selection

    /// <summary>
    ///     Select an individual container.
    /// </summary>
    /// <param name="container">The container to select.</param>
    /// <param name="addsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="addActionEvent">If an action event to undo the selection should be made</param>
    public static void Select(BeatmapObject obj, bool addsToSelection = false, bool automaticallyRefreshes = true,
        bool addActionEvent = true)
    {
        if (!addsToSelection)
            DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        var collection = BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType);

        if (!collection.LoadedObjects.Contains(obj))
            return;

        SelectedObjects.Add(obj);
        if (collection.LoadedContainers.TryGetValue(obj, out var container))
            container.SetOutlineColor(instance.selectedColor);
        if (addActionEvent)
        {
            ObjectWasSelectedEvent.Invoke(obj);
            SelectionChangedEvent?.Invoke();
        }
    }

    /// <summary>
    ///     Selects objects between 2 objects, sorted by group.
    /// </summary>
    /// <param name="first">The beatmap object at the one end of the selection.</param>
    /// <param name="second">The beatmap object at the other end of the selection</param>
    /// <param name="addsToSelection">Whether or not previously selected objects will deselect before selecting this object.</param>
    /// <param name="addActionEvent">If an action event to undo the selection should be made</param>
    public static void SelectBetween(BeatmapObject first, BeatmapObject second, bool addsToSelection = false,
        bool addActionEvent = true)
    {
        if (!addsToSelection)
            DeselectAll(); //This SHOULD deselect every object unless you otherwise specify, but it aint working.
        if (first.Time > second.Time)
            (first, second) = (second, first);
        GetObjectTypes(new[] { first, second }, out var hasNoteOrObstacle, out var hasEvent, out var hasBpmChange);
        ForEachObjectBetweenTimeByGroup(first.Time, second.Time, hasNoteOrObstacle, hasEvent, hasBpmChange,
            (collection, beatmapObject) =>
            {
                if (SelectedObjects.Contains(beatmapObject)) return;
                SelectedObjects.Add(beatmapObject);
                if (collection.LoadedContainers.TryGetValue(beatmapObject, out var container))
                    container.SetOutlineColor(instance.selectedColor);
                if (addActionEvent) ObjectWasSelectedEvent.Invoke(beatmapObject);
            });
        if (addActionEvent)
            SelectionChangedEvent?.Invoke();
    }

    /// <summary>
    ///     Deselects a container if it is currently selected
    /// </summary>
    /// <param name="obj">The container to deselect, if it has been selected.</param>
    public static void Deselect(BeatmapObject obj, bool removeActionEvent = true)
    {
        SelectedObjects.Remove(obj);
        if (BeatmapObjectContainerCollection.GetCollectionForType(obj.BeatmapType).LoadedContainers.TryGetValue(obj, out var container)
            && container != null)
        {
            container.OutlineVisible = false;
        }
        if (removeActionEvent) SelectionChangedEvent?.Invoke();
    }

    /// <summary>
    ///     Deselect all selected objects.
    /// </summary>
    public static void DeselectAll(bool removeActionEvent = true)
    {
        foreach (var obj in SelectedObjects.ToArray()) Deselect(obj, false);
        if (removeActionEvent) SelectionChangedEvent?.Invoke();
    }

    /// <summary>
    ///     Can be very taxing. Use sparringly.
    /// </summary>
    internal static void RefreshSelectionMaterial(bool triggersAction = true)
    {
        foreach (var data in SelectedObjects)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out var con))
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
    ///     Deletes and clears the current selection.
    /// </summary>
    public void Delete(bool triggersAction = true)
    {
        IEnumerable<BeatmapObject> objects = SelectedObjects.ToArray();
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionDeletedAction(objects));
        DeselectAll();
        foreach (var con in objects)
            BeatmapObjectContainerCollection.GetCollectionForType(con.BeatmapType).DeleteObject(con, false, false);
    }

    /// <summary>
    ///     Copies the current selection for later Pasting.
    /// </summary>
    /// <param name="cut">Whether or not to delete the original selection after copying them.</param>
    public void Copy(bool cut = false)
    {
        if (!HasSelectedObjects()) return;
        CopiedObjects.Clear();
        var firstTime = SelectedObjects.OrderBy(x => x.Time).First().Time;
        foreach (var data in SelectedObjects)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType);
            if (collection.LoadedContainers.TryGetValue(data, out var con)) con.SetOutlineColor(instance.copiedColor);
            var copy = BeatmapObject.GenerateCopy(data);
            copy.Time -= firstTime;
            CopiedObjects.Add(copy);
        }

        if (cut) Delete();
        var bpmChanges =
            BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.ObjectType.BpmChange);
        var lastBpmChange = bpmChanges.FindLastBpm(atsc.CurrentBeat);
        copiedBpm = lastBpmChange?.Bpm ?? atsc.Song.BeatsPerMinute;
    }

    /// <summary>
    ///     Pastes any copied objects into the map, selecting them immediately.
    /// </summary>
    public void Paste(bool triggersAction = true, bool overwriteSection = false)
    {
        DeselectAll();

        // Set up stuff that we need
        var pasted = new List<BeatmapObject>();
        var collections = new Dictionary<BeatmapObject.ObjectType, BeatmapObjectContainerCollection>();

        // Grab the last BPM Change to warp distances between copied objects and maintain BPM.
        var bpmChanges =
            BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.ObjectType.BpmChange);

        var lowerValue = new BeatmapBPMChange(420, atsc.CurrentBeat - 0.01f);
        var upperValue = new BeatmapBPMChange(69, atsc.CurrentBeat);

        var lastBpmChangeBeforePaste = bpmChanges.FindLastBpm(atsc.CurrentBeat);

        // This first loop creates copy of the data to be pasted.
        foreach (var data in CopiedObjects)
        {
            if (data == null) continue;

            upperValue.Time = atsc.CurrentBeat + data.Time;

            var bpmChangeView = bpmChanges.LoadedObjects.GetViewBetween(lowerValue, upperValue);

            var bpmTime = data.Time * (copiedBpm / (lastBpmChangeBeforePaste?.Bpm ?? copiedBpm));

            if (bpmChangeView.Any())
            {
                var firstBpmChange = bpmChangeView.First() as BeatmapBPMChange;

                bpmTime = firstBpmChange.Time - atsc.CurrentBeat;

                for (var i = 0; i < bpmChangeView.Count - 1; i++)
                {
                    var leftBpm = bpmChangeView.ElementAt(i) as BeatmapBPMChange;
                    var rightBpm = bpmChangeView.ElementAt(i + 1) as BeatmapBPMChange;

                    bpmTime += (rightBpm.Time - leftBpm.Time) * (copiedBpm / leftBpm.Bpm);
                }

                var lastBpmChange = bpmChangeView.Last() as BeatmapBPMChange;
                bpmTime += (atsc.CurrentBeat + data.Time - lastBpmChange.Time) * (copiedBpm / lastBpmChange.Bpm);
            }

            var newTime = bpmTime + atsc.CurrentBeat;

            var newData = BeatmapObject.GenerateCopy(data);
            newData.Time = newTime;

            if (!collections.TryGetValue(newData.BeatmapType, out var collection))
            {
                collection = BeatmapObjectContainerCollection.GetCollectionForType(newData.BeatmapType);
                collections.Add(newData.BeatmapType, collection);
            }

            pasted.Add(newData);
        }

        var totalRemoved = new List<BeatmapObject>();

        // We remove conflicting objects with our to-be-pasted objects.
        foreach (var kvp in collections)
        {
            kvp.Value.RemoveConflictingObjects(pasted.Where(x => x.BeatmapType == kvp.Key), out var conflicting);
            totalRemoved.AddRange(conflicting);
        }

        // While we're at it, we will also overwrite the entire section if we have to.
        if (overwriteSection)
        {
            var start = pasted.First().Time;
            var end = pasted.First().Time;
            foreach (var beatmapObject in pasted)
            {
                if (start > beatmapObject.Time)
                    start = beatmapObject.Time;
                if (end < beatmapObject.Time)
                    end = beatmapObject.Time;
            }

            GetObjectTypes(pasted, out var hasNoteOrObstacle, out var hasEvent, out var hasBpmChange);
            var toRemove = new List<(BeatmapObjectContainerCollection, BeatmapObject)>();
            ForEachObjectBetweenTimeByGroup(start, end, hasNoteOrObstacle, hasEvent, hasBpmChange,
                (collection, beatmapObject) =>
                {
                    if (pasted.Contains(beatmapObject)) return;
                    toRemove.Add((collection, beatmapObject));
                });
            foreach (var pair in toRemove)
            {
                var collection = pair.Item1;
                var beatmapObject = pair.Item2;
                collection.DeleteObject(beatmapObject, false);
                totalRemoved.Add(beatmapObject);
            }
        }

        // We then spawn our pasted objects into the map and select them.
        foreach (var data in pasted)
        {
            collections[data.BeatmapType].SpawnObject(data, false, false);
            Select(data, true, false, false);
        }

        foreach (var collection in collections.Values)
        {
            collection.RefreshPool();

            if (collection is BPMChangesContainer con) con.RefreshModifiedBeat();
        }

        if (CopiedObjects.Any(x => x is MapEvent e && e.IsRotationEvent)) tracksManager.RefreshTracks();
        if (triggersAction) BeatmapActionContainer.AddAction(new SelectionPastedAction(pasted, totalRemoved));
        SelectionPastedEvent?.Invoke(pasted);
        SelectionChangedEvent?.Invoke();
        RefreshSelectionMaterial(false);

        if (eventPlacement.objectContainerCollection.PropagationEditing != EventsContainer.PropMode.Off)
        {
            eventPlacement.objectContainerCollection.PropagationEditing =
                eventPlacement.objectContainerCollection.PropagationEditing;
        }

        Debug.Log("Pasted!");
    }

    public void MoveSelection(float beats, bool snapObjects = false)
    {
        var allActions = new List<BeatmapAction>();
        foreach (var data in SelectedObjects)
        {
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(data.BeatmapType);
            var original = BeatmapObject.GenerateCopy(data);

            collection.LoadedObjects.Remove(data);
            data.Time += beats;
            if (snapObjects)
                data.Time = Mathf.Round(beats / (1f / atsc.GridMeasureSnapping)) * (1f / atsc.GridMeasureSnapping);
            collection.LoadedObjects.Add(data);

            if (collection.LoadedContainers.TryGetValue(data, out var con)) con.UpdateGridPosition();

            if (collection is NotesContainer notesContainer)
            {
                notesContainer.RefreshSpecialAngles(original, false, false);
                notesContainer.RefreshSpecialAngles(data, false, false);
            }

            allActions.Add(new BeatmapObjectModifiedAction(data, data, original, "", true));
        }

        BeatmapActionContainer.AddAction(new ActionCollectionAction(allActions, true, true,
            "Shifted a selection of objects."));
        BeatmapObjectContainerCollection.RefreshAllPools();
    }

    public void ShiftSelection(int leftRight, int upDown)
    {
        var allActions = SelectedObjects.AsParallel().Select(data =>
        {
            var original = BeatmapObject.GenerateCopy(data);
            if (data is BeatmapNote note)
            {
                if (note.CustomData is null || !note.CustomData.HasKey("_position"))
                {
                    if (note.LineIndex >= 1000)
                    {
                        note.LineIndex += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                        if (note.LineIndex < 1000) note.LineIndex = 1000;
                    }
                    else if (note.LineIndex <= -1000)
                    {
                        note.LineIndex += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                        if (note.LineIndex > -1000) note.LineIndex = -1000;
                    }
                    else
                    {
                        note.LineIndex += leftRight;
                    }

                    note.LineLayer += upDown;
                }
                else
                {
                    if (data.CustomData.HasKey("_position"))
                    {
                        data.CustomData["_position"][0] += 1f / atsc.GridMeasureSnapping * leftRight;
                        data.CustomData["_position"][1] += 1f / atsc.GridMeasureSnapping * upDown;
                    }
                }
            }
            else if (data is BeatmapObstacle obstacle)
            {
                if (!obstacle.IsNoodleExtensionsWall)
                {
                    if (obstacle.LineIndex >= 1000)
                    {
                        obstacle.LineIndex += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                        if (obstacle.LineIndex < 1000) obstacle.LineIndex = 1000;
                    }
                    else if (obstacle.LineIndex <= -1000)
                    {
                        obstacle.LineIndex += Mathf.RoundToInt(1f / atsc.GridMeasureSnapping * 1000 * leftRight);
                        if (obstacle.LineIndex > -1000) obstacle.LineIndex = -1000;
                    }
                    else
                    {
                        obstacle.LineIndex += leftRight;
                    }
                }
                else
                {
                    if (data.CustomData.HasKey("_position"))
                    {
                        data.CustomData["_position"][0] += 1f / atsc.GridMeasureSnapping * leftRight;
                        data.CustomData["_position"][1] += 1f / atsc.GridMeasureSnapping * upDown;
                    }
                }
            }
            else if (data is MapEvent e)
            {
                var events = eventPlacement.objectContainerCollection;
                if (eventPlacement.objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Light)
                {
                    var max = events.platformDescriptor.LightingManagers[events.EventTypeToPropagate].ControllingLights
                        .Select(x => x.LightID).Max();

                    var curId = e.IsLightIdEvent ? e.LightId[0] : 0;
                    var newId = Math.Min(curId + leftRight, max);
                    if (newId < 1)
                        data.CustomData?.Remove("_lightID");
                    else
                        data.GetOrCreateCustomData()["_lightID"] = newId;
                }
                else if (eventPlacement.objectContainerCollection.PropagationEditing == EventsContainer.PropMode.Prop)
                {
                    var oldId = (e.IsLightIdEvent
                        ? labels.LightIdsToPropId(events.EventTypeToPropagate, e.LightId)
                        : null) ?? -1;
                    var max = events.platformDescriptor.LightingManagers[events.EventTypeToPropagate].LightsGroupedByZ
                        .Length;
                    var newId = Math.Min(oldId + leftRight, max - 1);

                    if (newId < 0)
                    {
                        data.CustomData?.Remove("_lightID");
                    }
                    else
                    {
                        data.GetOrCreateCustomData()["_lightID"] =
                            labels.PropIdToLightIdsJ(events.EventTypeToPropagate, newId);
                    }
                }
                else
                {
                    var oldType = e.Type;

                    var modified = labels.EventTypeToLaneId(e.Type);

                    modified += leftRight;

                    if (modified < 0) modified = 0;

                    var laneCount = labels.MaxLaneId();

                    if (modified > laneCount) modified = laneCount;

                    e.Type = labels.LaneIdToEventType(modified);

                    if (e.IsLightIdEvent && !e.CustomData["_lightID"].IsArray)
                    {
                        var editorID = labels.LightIDToEditor(oldType, e.LightId[0]);
                        e.CustomData["_lightID"] = labels.EditorToLightID(e.Type, editorID);
                    }
                    else if (e.IsLightIdEvent)
                    {
                        e.CustomData["_lightID"] = labels.PropIdToLightIdsJ(e.Type, e.PropId);
                    }

                    if (e.CustomData != null && e.CustomData.HasKey("_lightID") &&
                        e.CustomData["_lightID"].IsArray &&
                        e.CustomData["_lightID"].Count == 0)
                    {
                        e.CustomData.Remove("_lightID");
                    }
                }

                if (data.CustomData?.Count <= 0) data.CustomData = null;
            }

            return new BeatmapObjectModifiedAction(data, data, original, "", true);
        }).ToList();

        BeatmapActionContainer.AddAction(
            new ActionCollectionAction(allActions, true, true, "Shifted a selection of objects."), true);
        tracksManager.RefreshTracks();
    }

    /// <summary>
    ///     Applies objects to the loaded <see cref="BeatSaberMap" />. Should be done before saving the map.
    /// </summary>
    public static void RefreshMap()
    {
        if (BeatSaberSongContainer.Instance.Map != null)
        {
            var newObjects = new Dictionary<BeatmapObject.ObjectType, IEnumerable<BeatmapObject>>();
            foreach (int num in Enum.GetValues(typeof(BeatmapObject.ObjectType)))
            {
                var type = (BeatmapObject.ObjectType)num;
                var collection = BeatmapObjectContainerCollection.GetCollectionForType(type);
                if (collection is null) continue;
                newObjects.Add(type, collection.GrabSortedObjects());
            }

            if (Settings.Instance.Load_Notes)
            {
                BeatSaberSongContainer.Instance.Map.Notes =
                    newObjects[BeatmapObject.ObjectType.Note].Cast<BeatmapNote>().ToList();
            }

            if (Settings.Instance.Load_Obstacles)
            {
                BeatSaberSongContainer.Instance.Map.Obstacles =
                    newObjects[BeatmapObject.ObjectType.Obstacle].Cast<BeatmapObstacle>().ToList();
            }

            if (Settings.Instance.Load_Events)
            {
                BeatSaberSongContainer.Instance.Map.Events =
                    newObjects[BeatmapObject.ObjectType.Event].Cast<MapEvent>().ToList();
            }

            if (Settings.Instance.Load_Others)
            {
                BeatSaberSongContainer.Instance.Map.BpmChanges =
                    newObjects[BeatmapObject.ObjectType.BpmChange].Cast<BeatmapBPMChange>().ToList();
                BeatSaberSongContainer.Instance.Map.CustomEvents = newObjects[BeatmapObject.ObjectType.CustomEvent]
                    .Cast<BeatmapCustomEvent>().ToList();
            }

            if (Settings.Instance.Load_MapV3)
            {
                (BeatSaberSongContainer.Instance.Map as BeatSaberMapV3).Arcs = 
                    newObjects[BeatmapObject.ObjectType.Arc].Cast<BeatmapArc>().ToList();
                (BeatSaberSongContainer.Instance.Map as BeatSaberMapV3).Chains =
                    newObjects[BeatmapObject.ObjectType.Chain].Cast<BeatmapChain>().ToList();
            }
        }
    }

    #endregion
}
