using System;
using System.Collections;
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

    internal PlatformDescriptor platformDescriptor;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.EVENT;

    public int EventTypeToPropagate = MapEvent.EVENT_TYPE_RING_LIGHTS;

    public bool PropagationEditing
    {
        get { return propagationEditing; }
        set
        {
            propagationEditing = value;
            int propagationLength = platformDescriptor.LightingManagers[EventTypeToPropagate]?.LightsGroupedByZ?.Length ?? 0;
            labels.UpdateLabels(value, EventTypeToPropagate, value ? propagationLength + 1 : 16);
            eventPlacement.SetGridSize(value ? propagationLength + 1 : 6 + platformDescriptor.LightingManagers.Count(s => s != null));
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

    public override void SortObjects()
    {
        UseChunkLoading = true;
    }

    protected override void OnObjectDelete(BeatmapObject obj)
    {
        if (obj is MapEvent e && e.IsRotationEvent) tracksManager.RefreshTracks();
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
                con.transform.localPosition = new Vector3(pos + 0.5f, 0.5f, con.transform.localPosition.z);
            }
            else
            {
                con.SafeSetActive(true);
                con.UpdateGridPosition();
            }
        }
        if (!propagationEditing) OnPlayToggle(AudioTimeSyncController.IsPlaying);
        SelectionController.RefreshMap();
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
            RecycleContainer(objectData);
        }
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
        return (a as MapEvent)._type == (b as MapEvent)._type;
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapEventContainer.SpawnEvent(this, null, ref eventPrefab, ref eventAppearanceSO);

    protected override void UpdateContainerData(BeatmapObjectContainer con, BeatmapObject obj)
    {
        eventAppearanceSO.SetEventAppearance(con as BeatmapEventContainer);
    }
}
