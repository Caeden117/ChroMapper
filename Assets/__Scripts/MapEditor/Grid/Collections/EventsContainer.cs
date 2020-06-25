using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class EventsContainer : BeatmapObjectContainerCollection, CMInput.IEventGridActions
{
    [SerializeField] private GameObject eventPrefab;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private GameObject eventGridLabels;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private EventPlacement eventPlacement;
    [SerializeField] private CreateEventTypeLabels labels;

    internal PlatformDescriptor platformDescriptor;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.EVENT;

    public int EventTypeToPropagate = MapEvent.EVENT_TYPE_RING_LIGHTS;
    public int EventTypePropagationSize = 0;

    public List<MapEvent> AllRotationEvents = new List<MapEvent>();

    public bool PropagationEditing
    {
        get { return propagationEditing; }
        set
        {
            propagationEditing = value;
            int propagationLength = platformDescriptor.LightingManagers[EventTypeToPropagate]?.LightsGroupedByZ?.Length ?? 0;
            labels.UpdateLabels(value, EventTypeToPropagate, value ? propagationLength + 1 : 16);
            eventPlacement.SetGridSize(value ? propagationLength + 1 : 6 + platformDescriptor.LightingManagers.Count(s => s != null));
            EventTypePropagationSize = propagationLength;
            UpdatePropagationMode();
        }
    }
    private bool propagationEditing = false;

    private void Start()
    {
        LoadInitialMap.PlatformLoadedEvent += PlatformLoaded;
    }

    void PlatformLoaded(PlatformDescriptor descriptor)
    {
        platformDescriptor = descriptor;
        labels.UpdateLabels(false, MapEvent.EVENT_TYPE_RING_LIGHTS, 16);
        eventPlacement.SetGridSize(6 + descriptor.LightingManagers.Count(s => s != null));
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
        if (obj is MapEvent e && e.IsRotationEvent)
        {
            AllRotationEvents.Remove(e);
            tracksManager.RefreshTracks();
        }
    }

    protected override void OnObjectSpawned(BeatmapObject obj)
    {
        if (obj is MapEvent e && e.IsRotationEvent)
        {
            AllRotationEvents.Add(e);
        }
    }

    private void UpdatePropagationMode()
    {
        foreach (BeatmapObjectContainer con in LoadedContainers.Values)
        {
            if (propagationEditing)
            {
                int pos = 0;
                if (con.objectData._customData != null && con.objectData._customData["_propID"].IsNumber)
                    pos = (con.objectData?._customData["_propID"]?.AsInt  ?? -1) + 1;
                if ((con is BeatmapEventContainer e) && e.eventData._type != EventTypeToPropagate)
                {
                    con.SafeSetActive(false);
                    pos = -1;
                }
                else
                {
                    con.SafeSetActive(true);
                }
                con.transform.localPosition = new Vector3(pos + 0.5f, 0.5f, con.transform.localPosition.z);
            }
            else
            {
                con.SafeSetActive(true);
                con.UpdateGridPosition();
            }
        }
        if (!propagationEditing) OnPlayToggle(AudioTimeSyncController.IsPlaying);
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
            if (e._lightGradient != null)
            {
                StartCoroutine(WaitForGradientThenRecycle(e));
            }
            else
            {
                RecycleContainer(objectData);
            }
        }
    }

    private IEnumerator WaitForGradientThenRecycle(MapEvent @event)
    {
        float duration = AudioTimeSyncController.GetSecondsFromBeat(@event._lightGradient.Duration);
        yield return new WaitForSeconds(duration);
        RecycleContainer(@event);
    }

    void OnPlayToggle(bool playing)
    {
        if (!playing)
        {
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
        if (context.performed) PropagationEditing = !PropagationEditing;
    }

    public void OnCycleLightPropagationUp(InputAction.CallbackContext context)
    {
        if (!context.performed || !PropagationEditing) return;
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
        PropagationEditing = true;
    }

    public void OnCycleLightPropagationDown(InputAction.CallbackContext context)
    {
        if (!context.performed || !PropagationEditing) return;
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
        PropagationEditing = true;
    }

    protected override bool AreObjectsAtSameTimeConflicting(BeatmapObject a, BeatmapObject b)
    {
        MapEvent eventA = a as MapEvent;
        MapEvent eventB = b as MapEvent;
        if (a._customData?.HasKey("_propID") ?? false && (b._customData?.HasKey("_propID") ?? false))
        {
            return eventA._type == eventB._type && a._customData["_propID"] == b._customData["_propID"];
        }
        return eventA._type == eventB._type;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapEventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSO);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSO.SetEventAppearance(con as BeatmapEventContainer);
        if (PropagationEditing && (obj as MapEvent)._type != EventTypeToPropagate) con.SafeSetActive(false);
    }
}
