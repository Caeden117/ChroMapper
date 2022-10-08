using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Appearances;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Base;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class EventGridContainer : BeatmapObjectContainerCollection, CMInput.IEventGridActions
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

    public int EventTypeToPropagate = (int)EventTypeValue.RingRotation;
    public int EventTypePropagationSize;

    public List<IEvent> AllRotationEvents = new List<IEvent>();
    public List<IEvent> AllBoostEvents = new List<IEvent>();

    // TODO: why was this MapEventV3?
    private Dictionary<int, List<IEvent>> allLightEvents = new Dictionary<int, List<IEvent>>();
    public Dictionary<int, List<IEvent>> AllLightEvents { get => allLightEvents;
        set {
            allLightEvents = value;
            foreach (var p in allLightEvents)
            {
                var lightList = p.Value;
                for (var i = 0; i < lightList.Count - 1; ++i)
                    lightList[i].Next = lightList[i + 1];
                lightList[lightList.Count - 1].Next = null;
            }
        } }

    internal PlatformDescriptor platformDescriptor;
    private PropMode propagationEditing = PropMode.Off;

    public override ObjectType ContainerType => ObjectType.Event;
    private static int ExtraInterscopeLanes => EventContainer.ModifyTypeMode == 2 ? 2 : 0;
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

    protected override void OnObjectDelete(IObject obj)
    {
        if (obj is IEvent e)
        {
            if (e.IsLaneRotationEvent())
            {
                AllRotationEvents.Remove(e);
                tracksManager.RefreshTracks();
            }
            else if (e.IsColorBoostEvent())
            {
                AllBoostEvents.Remove(e);
            }
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    protected override void OnObjectSpawned(IObject obj)
    {
        if (obj is IEvent e)
        {
            if (e.IsLaneRotationEvent())
                AllRotationEvents.Add(e);
            else if (e.IsColorBoostEvent()) AllBoostEvents.Add(e);
        }

        countersPlus.UpdateStatistic(CountersPlusStatistic.Events);
    }

    public override IEnumerable<IObject> GrabSortedObjects()
    {
        var sorted = new List<IObject>();
        var grouping = LoadedObjects.GroupBy(x => x.Time);
        foreach (var group in grouping)
        {
            sorted.AddRange(@group.OrderBy(x =>
                (x as IEvent)?.CustomPropID ?? -1)); // Sort non-light prop events before light prop events
        }

        return sorted;
    }

    private void UpdatePropagationMode()
    {
        foreach (var con in LoadedContainers.Values)
        {
            if (!(con is EventContainer e)) continue;
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

    private void SpawnCallback(bool initial, int index, IObject objectData)
    {
        if (!LoadedContainers.ContainsKey(objectData)) CreateContainerFromPool(objectData);
    }

    //We don't need to check index as that's already done further up the chain
    private void DespawnCallback(bool initial, int index, IObject objectData)
    {
        if (LoadedContainers.ContainsKey(objectData))
        {
            var e = objectData as IEvent;
            if (e.CustomLightGradient != null && Settings.Instance.VisualizeChromaGradients && isActiveAndEnabled)
                StartCoroutine(nameof(WaitForGradientThenRecycle), e);
            else
                RecycleContainer(objectData);
        }
    }

    private IEnumerator WaitForGradientThenRecycle(IEvent @event)
    {
        var endTime = @event.Time + @event.CustomLightGradient.Duration;
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
        else
        {
            LinkAllLightEvents();
        }
    }

    private void RecursiveCheckFinished(bool natural, int lastPassedIndex)
    {
        var epsilon = Mathf.Pow(10, -9);
        RefreshPool(AudioTimeSyncController.CurrentBeat + DespawnCallbackController.Offset - epsilon,
            AudioTimeSyncController.CurrentBeat + SpawnCallbackController.Offset + epsilon);
    }

    public override ObjectContainer CreateContainer() =>
        EventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSo, ref labels);

    protected override void UpdateContainerData(ObjectContainer con, IObject obj)
    {
        eventAppearanceSo.SetEventAppearance(con as EventContainer, true,
            AllBoostEvents.FindLast(x => x.Time <= obj.Time)?.Value == 1);
        var e = obj as IEvent;
        if (PropagationEditing != PropMode.Off && e.Type != EventTypeToPropagate) con.SafeSetActive(false);
    }

    public void LinkAllLightEvents()
    {
        AllLightEvents = LoadedObjects.OfType<IEvent>().
                    Where(x => x.IsLightEvent(EnvironmentInfoHelper.GetName())).
                    GroupBy(x => x.Type).
                    ToDictionary(g => g.Key, g => g.ToList());
    }
}
