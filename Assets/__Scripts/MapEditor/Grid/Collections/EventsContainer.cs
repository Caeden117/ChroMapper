using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class EventsContainer : BeatmapObjectContainerCollection, CMInput.IEventGridActions
{
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private GameObject eventGridLabels;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CreateEventTypeLabels labels;
    [SerializeField] private BoxSelectionPlacementController boxSelectionPlacementController;
    [SerializeField] private LaserSpeedController laserSpeedController;
    [SerializeField] private CountersPlusController countersPlus;

    internal PlatformDescriptor platformDescriptor;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.EVENT;

    public int EventTypeToPropagate = MapEvent.EVENT_TYPE_RING_LIGHTS;
    public int EventTypePropagationSize = 0;
    private static int ExtraInterscopeLanes => BeatmapEventContainer.ModifyTypeMode == 2 ? 2 : 0;
    private int SpecialEventTypeCount => 7 + labels.NoRotationLaneOffset + ExtraInterscopeLanes;

    public List<MapEvent> AllRotationEvents = new List<MapEvent>();
    public List<MapEvent> AllBoostEvents = new List<MapEvent>();

    public enum PropMode
    {
        Off, Prop, Light
    }
    
    public static string GetKeyForProp(PropMode mode) {
        if (mode == PropMode.Light) return "_lightID";

        return mode == PropMode.Prop ? "_propID" : null;
    }

    public PropMode PropagationEditing
    {
        get => propagationEditing;
        set
        {
            propagationEditing = value;
            boxSelectionPlacementController.CancelPlacement();
            var propagationLength = (value == PropMode.Light ? platformDescriptor.LightingManagers[EventTypeToPropagate]?.LightIDPlacementMapReverse?.Count :
                platformDescriptor.LightingManagers[EventTypeToPropagate]?.LightsGroupedByZ?.Length) ?? 0;
            labels.UpdateLabels(value, EventTypeToPropagate, value == PropMode.Off ? 16 + ExtraInterscopeLanes : propagationLength + 1);
            eventPlacement.SetGridSize(value != PropMode.Off ? propagationLength + 1 : SpecialEventTypeCount + platformDescriptor.LightingManagers.Count(s => s != null));
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }
    private PropMode propagationEditing = PropMode.Off;

    private void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor;
        StartCoroutine(AfterPlatformLoaded());
    }

    private IEnumerator AfterPlatformLoaded()
    {
        yield return null;
        PropagationEditing = PropMode.Off;
    }

    void OnDestroy()
    {
        LoadInitialMap.PlatformLoadedEvent -= PlatformLoaded;
    }

    internal override void SubscribeToCallbacks()
    {
        SpawnCallbackController.EventPassedThreshold += SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished += RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold += DespawnCallback;
        AudioTimeSyncController.OnPlayToggle += OnPlayToggle;
    }

    internal override void UnsubscribeToCallbacks() {
        SpawnCallbackController.EventPassedThreshold -= SpawnCallback;
        SpawnCallbackController.RecursiveEventCheckFinished -= RecursiveCheckFinished;
        DespawnCallbackController.EventPassedThreshold -= DespawnCallback;
        AudioTimeSyncController.OnPlayToggle -= OnPlayToggle;
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
            else if (e._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS)
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
            {
                AllRotationEvents.Add(e);
            }
            else if (e._type == MapEvent.EVENT_TYPE_BOOST_LIGHTS)
            {
                AllBoostEvents.Add(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override IEnumerable<BeatmapObject> GrabSortedObjects()
    {
        List<BeatmapObject> sorted = new List<BeatmapObject>();
        var grouping = LoadedObjects.GroupBy(x => x._time);
        foreach (var group in grouping)
        {
            sorted.AddRange(group.OrderBy(x => (x._customData?.HasKey("_propID") ?? false) ? x._customData["_propID"].AsInt : -1)); // Sort non-light prop events before light prop events
        }
        return sorted;
    }

    private void UpdatePropagationMode()
    {
        foreach (BeatmapObjectContainer con in LoadedContainers.Values)
        {
            if (!(con is BeatmapEventContainer e)) continue;
            if (propagationEditing != PropMode.Off)
            {
                if (e.eventData._type != EventTypeToPropagate)
                {
                    con.SafeSetActive(false);
                }
                else
                {
                    con.SafeSetActive(true);
                }
            }
            else
            {
                con.SafeSetActive(true);
            }
            con.UpdateGridPosition();
        }
        if (propagationEditing == PropMode.Off) OnPlayToggle(AudioTimeSyncController.IsPlaying);
    }

    void SpawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData))
        {
            CreateContainerFromPool(objectData);
        }
    }

    //We don't need to check index as that's already done further up the chain
    void DespawnCallback(bool initial, int index, BeatmapObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            MapEvent e = objectData as MapEvent;
            if (e._lightGradient != null && Settings.Instance.VisualizeChromaGradients && isActiveAndEnabled)
            {
                StartCoroutine(nameof(WaitForGradientThenRecycle), e);
            }
            else
            {
                RecycleContainer(objectData);
            }
        }
    }

    private IEnumerator WaitForGradientThenRecycle(MapEvent @event)
    {
        float endTime = @event._time + @event._lightGradient.Duration;
        yield return new WaitUntil(() => endTime < (AudioTimeSyncController.CurrentBeat + DespawnCallbackController.offset));
        RecycleContainer(@event);
    }

    void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
            StopCoroutine(nameof(WaitForGradientThenRecycle));
            RefreshPool();
        }
    }

    void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        float epsilon = Mathf.Pow(10, -9);
        RefreshPool(AudioTimeSyncController.CurrentBeat + DespawnCallbackController.offset - epsilon,
            AudioTimeSyncController.CurrentBeat + SpawnCallbackController.offset + epsilon);
    }

    public void OnToggleLightPropagation(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PropagationEditing = PropagationEditing == PropMode.Prop ? PropMode.Off : PropMode.Prop;
        }
    }

    public void OnToggleLightIdMode(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PropagationEditing = PropagationEditing == PropMode.Light ? PropMode.Off : PropMode.Light;
        }
    }

    public void OnResetRings(InputAction.CallbackContext context)
    {
        if (!context.performed || laserSpeedController.Activated) return;

        if (platformDescriptor.BigRingManager is TrackLaneRingsManager manager)
            manager.rotationEffect.Reset();

        if (platformDescriptor.SmallRingManager != null)
            platformDescriptor.SmallRingManager.rotationEffect.Reset();
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        int nextID = EventTypeToPropagate + 1;
        if (nextID == platformDescriptor.LightingManagers.Length)
        {
            nextID = 0;
        }
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID++;
            if (nextID == platformDescriptor.LightingManagers.Length)
            {
                nextID = 0;
            }
        }
        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        if (!context.performed || PropagationEditing == PropMode.Off) return;
        int nextID = EventTypeToPropagate - 1;
        if (nextID == -1)
        {
            nextID = platformDescriptor.LightingManagers.Length - 1;
        }
        while (platformDescriptor.LightingManagers[nextID] == null)
        {
            nextID--;
            if (nextID == -1)
            {
                nextID = platformDescriptor.LightingManagers.Length - 1;
            }
        }
        EventTypeToPropagate = nextID;
        PropagationEditing = PropagationEditing;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapEventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSO, ref labels);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSO.SetEventAppearance(con as BeatmapEventContainer, true,
            AllBoostEvents.FindLast(x => x._time <= obj._time)?._value == 1);
        MapEvent e = obj as MapEvent;
        if (PropagationEditing != PropMode.Off && e._type != EventTypeToPropagate) con.SafeSetActive(false);
    }
}
