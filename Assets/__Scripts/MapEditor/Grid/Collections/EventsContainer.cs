using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class EventsContainer : BeatmapObjectContainerCollection, CMInput.IEventGridActions
{
    public enum PropMode
    {
        Off, Prop, Light
    }

    [SerializeField] private GameObject eventPrefab;
    [FormerlySerializedAs("eventAppearanceSO")] [SerializeField] private EventAppearanceSO eventAppearanceSo;
    [SerializeField] private GameObject eventGridLabels;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private BoxSelectionPlacementController boxSelectionPlacementController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private CountersPlusController countersPlus;

    public int EventTypeToPropagate = MapEvent.EventTypeRingLights;
    public int EventTypePropagationSize;

    public List<MapEvent> AllRotationEvents = new List<MapEvent>();
    public List<MapEvent> AllBoostEvents = new List<MapEvent>();

    internal PlatformDescriptor platformDescriptor;
    private PropMode propagationEditing = PropMode.Off;

    public override BeatmapObject.ObjectType ContainerType => BeatmapObject.ObjectType.Event;
    private static int ExtraInterscopeLanes => BeatmapEventContainer.ModifyTypeMode == 2 ? 2 : 0;
    private int SpecialEventTypeCount => 7 + labels.NoRotationLaneOffset + ExtraInterscopeLanes;

    public PropMode PropagationEditing
    {
        get => propagationEditing;
        set
        {
            propagationEditing = value;
            boxSelectionPlacementController.CancelPlacement();

            var lightingManager = platformDescriptor.LightingManagers[EventTypeToPropagate];

            var propagationLength = lightingManager == null
                ? 0
                : (value == PropMode.Light
                    ? platformDescriptor.LightingManagers[EventTypeToPropagate].LightIDPlacementMapReverse?.Count
                    : platformDescriptor.LightingManagers[EventTypeToPropagate].LightsGroupedByZ?.Length) ?? 0;

            labels.UpdateLabels(value, EventTypeToPropagate,
                value == PropMode.Off ? 16 + ExtraInterscopeLanes : propagationLength + 1);
            eventPlacement.SetGridSize(value != PropMode.Off
                ? propagationLength + 1
                : SpecialEventTypeCount + platformDescriptor.LightingManagers.Count(s => s != null));
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }

    private void Start() => LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;

    private void OnDestroy() => LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;

    public void OnToggleLightPropagation(InputAction.CallbackContext context)
    {
        if (context.performed) PropagationEditing = PropagationEditing == PropMode.Prop ? PropMode.Off : PropMode.Prop;
    }

    public void OnToggleLightIdMode(InputAction.CallbackContext context)
    {
        if (context.performed)
            PropagationEditing = PropagationEditing == PropMode.Light ? PropMode.Off : PropMode.Light;
    }

    public void OnResetRings(InputAction.CallbackContext context)
    {
        if (!context.performed || laserSpeedController.Activated) return;

        if (platformDescriptor.BigRingManager is TrackLaneRingsManager manager)
            manager.RotationEffect.Reset();

        if (platformDescriptor.SmallRingManager != null && platformDescriptor.SmallRingManager.RotationEffect != null)
            platformDescriptor.SmallRingManager.RotationEffect.Reset();
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var nextID = EventTypeToPropagate + 1;
        if (nextID == platformDescriptor.LightingManagers.Length) nextID = 0;
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID++;
            if (nextID == platformDescriptor.LightingManagers.Length) nextID = 0;
        }

        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        var nextID = EventTypeToPropagate - 1;
        if (nextID == -1) nextID = platformDescriptor.LightingManagers.Length - 1;
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID--;
            if (nextID == -1) nextID = platformDescriptor.LightingManagers.Length - 1;
        }

        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public static string GetKeyForProp(PropMode mode)
    {
        if (mode == PropMode.Light) return "_lightID";

        return mode == PropMode.Prop ? "_propID" : null;
    }

    private void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor;
        StartCoroutine(AfterPlatformLoaded());
    }

    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        PropagationEditing = PropMode.Off;
    }

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold += DespawnCallback;
        AudioTimeSyncController.PlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished -= RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.PlayToggle -= OnPlayToggle;
    }

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        if (obj is MapEvent e)
        {
            if (e.IsRotationEvent)
            {
                AllRotationEvents.Remove(e);
                tracksManager.RefreshTracks();
            }
            else if (e.Type == MapEvent.EventTypeBoostLights)
            {
                AllBoostEvents.Remove(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    protected override void OnObjectSpawned(BeatmapObject obj)
    {
        if (obj is MapEvent e)
        {
            if (e.IsRotationEvent)
                AllRotationEvents.Add(e);
            else if (e.Type == MapEvent.EventTypeBoostLights) AllBoostEvents.Add(e);
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override IEnumerable<BeatmapObject> GrabSortedObjects()
    {
        var sorted = new List<BeatmapObject>();
        var grouping = LoadedObjects.GroupBy(x => x.Time);
        foreach (var group in grouping)
        {
            sorted.AddRange(@group.OrderBy(x =>
                x.CustomData?.HasKey("_propID") ?? false
                    ? x.CustomData["_propID"].AsInt
                    : -1)); // Sort non-light prop events before light prop events
        }

        return sorted;
    }

    private void UpdatePropagationMode()
    {
        foreach (var con in LoadedContainers.Values)
        {
            if (!(con is BeatmapEventContainer e)) continue;
            if (propagationEditing != PropMode.Off)
            {
                if (e.EventData.Type != EventTypeToPropagate)
                    con.SafeSetActive(false);
                else
                    con.SafeSetActive(true);
            }
            else
            {
                con.SafeSetActive(true);
            }

            con.UpdateGridPosition();
        }

        if (propagationEditing == PropMode.Off) OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

    private void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            var e = objectData as MapEvent;
            if (e.LightGradient != null && Settings.Instance.VisualizeChromaGradients && isActiveAndEnabled)
                StartCoroutine(nameof(WaitForGradientThenRecycle), e);
            else
                RecycleContainer(objectData);
        }
    }

    private IEnumerator WaitForGradientThenRecycle(MapEvent @event)
    {
        var endTime = @event.Time + @event.LightGradient.Duration;
        yield return new WaitUntil(() =>
            endTime < AudioTimeSyncController.CurrentBeat + DespawnCallbackController.Offset);
        RecycleContainer(@event);
    }

    private void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
            StopCoroutine(nameof(WaitForGradientThenRecycle));
            RefreshPool();
        }
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        var epsilon = Mathf.Pow(10, -9);
        RefreshPool(AudioTimeSyncController.CurrentBeat + DespawnCallbackController.Offset - epsilon,
            AudioTimeSyncController.CurrentBeat + SpawnCallbackController.Offset + epsilon);
    }

    public override BeatmapObjectContainer CreateContainer() =>
        BeatmapEventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSo, ref labels);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSo.SetEventAppearance(con as BeatmapEventContainer, true,
            AllBoostEvents.FindLast(x => x.Time <= obj.Time)?.Value == 1);
        var e = obj as MapEvent;
        if (PropagationEditing != PropMode.Off && e.Type != EventTypeToPropagate) con.SafeSetActive(false);
    }
}
