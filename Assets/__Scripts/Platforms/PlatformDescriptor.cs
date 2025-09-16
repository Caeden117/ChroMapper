using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class PlatformDescriptor : MonoBehaviour
{
    [Header("Rings")] [Tooltip("Leave null if you do not want small rings.")]
    public TrackLaneRingsManager SmallRingManager;

    [Tooltip("Leave null if you do not want big rings.")]
    public TrackLaneRingsManagerBase BigRingManager;

    [Tooltip("Leave null if you do not want gaga environment disks.")]
    public GagaDiskManager DiskManager;

    [Header("Lighting Groups")] [Tooltip("Manually map an Event ID (Index) to a group of lights (LightingManagers)")]
    public BasicLightManager[] LightingManagers = { };

    [Tooltip("If you want a thing to rotate around a 360 level with the track, place it here.")]
    public GridRotationController RotationController;

    [FormerlySerializedAs("Colors")] [FormerlySerializedAs("colors")] [HideInInspector]
    public PlatformColorScheme ColorScheme;

    [FormerlySerializedAs("DefaultColors")] [FormerlySerializedAs("defaultColors")]
    public PlatformColorScheme DefaultColorScheme = new PlatformColorScheme();

    [Tooltip(
        "-1 = No Sorting | 0 = Default Sorting | 1 = Collider Platform Special | 2 = New lanes 6/7 + 16/17 | 3 = Gaga Lanes")]
    public int SortMode;

    [Tooltip("Objects to disable through the L keybind, like lights and static objects in 360 environments.")]
    public GameObject[] DisablableObjects;

    [Tooltip("Change scale of normal map for shiny objects.")]
    public float NormalMapScale = 2f;

    private AudioTimeSyncController atsc;
    private ColorBoostManager colorBoostManager;

    private readonly Dictionary<int, List<BasicEventManager>> eventTypeManagerMap = new();
    private readonly List<BasicEventManager> sortedPriorityManagers = new();

    private RotationCallbackController rotationCallback;

    private static readonly int baseMap = Shader.PropertyToID("_BaseMap");

    public bool SoloAnEventType { get; private set; }
    public int SoloEventType { get; private set; }

    // loading happens too fast now
    private void Awake()
    {
        colorBoostManager = gameObject.AddComponent<ColorBoostManager>();
        BeatmapActionContainer.ActionCreatedEvent += HandleActionEventRedo;
        BeatmapActionContainer.ActionRedoEvent += HandleActionEventRedo;
        BeatmapActionContainer.ActionUndoEvent += HandleActionEventUndo;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") LoadInitialMap.LevelLoadedEvent += LevelLoaded;
    }

    private void Start() => UpdateShinyMaterialSettings();

    private void OnDestroy()
    {
        BeatmapActionContainer.ActionCreatedEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionRedoEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionUndoEvent -= HandleActionEventUndo;
        if (atsc != null) atsc.TimeChanged -= UpdateTime;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") LoadInitialMap.LevelLoadedEvent -= LevelLoaded;

        foreach (var manager in LightingManagers.Where(manager => manager != null))
            colorBoostManager.OnStateChange -= manager.ToggleBoost;
    }

    public void UpdateShinyMaterialSettings()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            if (renderer.sharedMaterial.name.Contains("Shiny Ass Black"))
            {
                var scale = renderer.gameObject.transform.lossyScale;
                var normalScale = new Vector2(scale.x, scale.z) / NormalMapScale;
                renderer.material.SetTextureScale(baseMap, normalScale);
                renderer.material.SetTextureOffset(baseMap, Vector2.zero);
            }
        }
    }

    private void LevelLoaded()
    {
        rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.Atsc;
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        atsc.TimeChanged += UpdateTime;
        RefreshLightingManagers();

        if (Settings.Instance.HideDisablableObjectsOnLoad) ToggleDisablableObjects();
    }

    public void RefreshLightingManagers() => StartCoroutine(PlatformLoadFromHell());

    // first off, what the fuck
    private IEnumerator PlatformLoadFromHell()
    {
        yield return new WaitForEndOfFrame(); // Actually wait for platform to fully load from Awake and Start

        BasicLightManager.FlashTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FlashTimeSecond);
        BasicLightManager.FadeTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FadeTimeSecond);

        sortedPriorityManagers.Clear();
        eventTypeManagerMap.Clear();

        for (var type = 0; type < LightingManagers.Length; type++)
        {
            var manager = LightingManagers[type];
            if (manager is null) continue;
            manager.SetColors(ColorScheme);
            colorBoostManager.OnStateChange += manager.ToggleBoost;
            MapEventManager(manager, type);
        }

        MapEventManager(colorBoostManager, 5);

        if (BigRingManager != null)
        {
            BigRingManager.RingFilter = RingFilter.Big;
            MapEventManager(BigRingManager, 8);
            MapEventManager(BigRingManager, 9);
        }

        if (SmallRingManager != null)
        {
            SmallRingManager.RingFilter = RingFilter.Small;
            MapEventManager(SmallRingManager, 8);
            MapEventManager(SmallRingManager, 9);
        }

        if (DiskManager != null)
        {
            MapEventManager(DiskManager, 12);
            MapEventManager(DiskManager, 13);
            MapEventManager(DiskManager, 16);
            MapEventManager(DiskManager, 17);
            MapEventManager(DiskManager, 18);
            MapEventManager(DiskManager, 19);
        }

        foreach (var handler in GetComponentsInChildren<PlatformEventHandler>())
        foreach (var type in handler.ListeningEventTypes)
            MapEventManager(handler, type);

        var leftEventTypes = new List<int>
        {
            (int)EventTypeValue.LeftLasers, (int)EventTypeValue.ExtraLeftLasers, (int)EventTypeValue.ExtraLeftLights
        };
        foreach (var l in leftEventTypes
            .Where(t => t <= LightingManagers.Length)
            .SelectMany(eventType => LightingManagers[eventType].RotatingLights))
            MapEventManager(l, 12);
        var rightEventTypes = new List<int>
        {
            (int)EventTypeValue.RightLasers,
            (int)EventTypeValue.ExtraRightLasers,
            (int)EventTypeValue.ExtraRightLights
        };
        foreach (var l in rightEventTypes
            .Where(t => t <= LightingManagers.Length)
            .SelectMany(eventType => LightingManagers[eventType].RotatingLights))
            MapEventManager(l, 13);

        foreach (var (type, managers) in eventTypeManagerMap)
        {
            var events = BeatSaberSongContainer.Instance.Map.Events.Where(e => e.Type == type);
            managers.ForEach(manager =>
            {
                manager.BuildFromEvents(events);
                manager.Atsc = atsc;
            });
        }

        foreach (var manager in eventTypeManagerMap
            .Values.SelectMany(manager => manager)
            .OrderBy(manager => manager.Priority))
            sortedPriorityManagers.Add(manager);

        foreach (var manager in sortedPriorityManagers)
        {
            manager.Reset();
            manager.UpdateTime(atsc.CurrentSongBpmTime);
        }
    }

    private void MapEventManager(BasicEventManager manager, int type)
    {
        if (!eventTypeManagerMap.ContainsKey(type)) eventTypeManagerMap.Add(type, new());
        eventTypeManagerMap[type].Add(manager);
    }

    public void UpdateTime()
    {
        // if (atsc.IsPlaying) return; // maybe lateupdate, maybe callback
        foreach (var manager in sortedPriorityManagers) manager.UpdateTime(atsc.CurrentSongBpmTime);
    }

    public void UpdateSoloEventType(bool solo, int soloTypeID)
    {
        SoloAnEventType = solo;
        SoloEventType = soloTypeID;
    }

    public void ToggleDisablableObjects()
    {
        foreach (var go in DisablableObjects) go.SetActive(!go.activeInHierarchy);
    }

    private bool AddEvents(IEnumerable<BaseEvent> events)
    {
        var mark = false;
        foreach (var baseEvent in events)
        {
            if (!eventTypeManagerMap.TryGetValue(baseEvent.Type, out var managers)) continue;
            managers.ForEach(manager => manager.InsertEvent(baseEvent));
            mark = true;
        }

        return mark;
    }

    private bool RemoveEvents(IEnumerable<BaseEvent> events)
    {
        var mark = false;
        foreach (var baseEvent in events)
        {
            if (!eventTypeManagerMap.TryGetValue(baseEvent.Type, out var managers)) continue;
            managers.ForEach(manager => manager.RemoveEvent(baseEvent));
            mark = true;
        }

        return mark;
    }

    private void HandleActionEventRedo(BeatmapAction action)
    {
        if (!HandleActionEventRedoNoNotify(action) || atsc.IsPlaying) return;
        foreach (var manager in sortedPriorityManagers) manager.Reset();
        UpdateTime();
    }

    private bool HandleActionEventRedoNoNotify(BeatmapAction action)
    {
        return action switch
        {
            ActionCollectionAction actionCollectionAction => actionCollectionAction
                .Actions.ToArray()
                .Select(HandleActionEventRedoNoNotify)
                .Any(),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionRedo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionRedo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionRedo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionRedo(
                    strobeGeneratorGenerationAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionRedo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionRedo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionRedo(beatmapObjectModifiedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionRedo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionRedo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveEvents(
            action
                .RemovedConflictObjects
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        b = AddEvents(
                action
                    .Data.Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
        return b;
    }

    private bool HandleSelectionDeletedActionRedo(SelectionDeletedAction action) =>
        RemoveEvents(
            action
                .Data.Where(d => d is BaseEvent)
                .Cast<BaseEvent>());

    private bool HandleSelectionPastedActionRedo(SelectionPastedAction action)
    {
        var b = RemoveEvents(
            action
                .Removed
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        b = AddEvents(
                action
                    .Data.Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
        return b;
    }

    private bool HandleStrobeGeneratorGenerationActionRedo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveEvents(
            action
                .ConflictingData
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .Data.Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleDeletionActionRedo(BeatmapObjectDeletionAction action) =>
        RemoveEvents(
            action
                .Data.Where(d => d is BaseEvent)
                .Cast<BaseEvent>());

    private bool HandleModifiedActionRedo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.OriginalObject }
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleModifiedCollectionActionRedo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveEvents(
            action
                .OriginalObjects
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .EditedObjects
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionRedo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.OriginalObject }
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        b = RemoveEvents(
                action
                    .ConflictingObjects
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
        return AddEvents(
                new List<BaseObject> { action.EditedObject }
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private void HandleActionEventUndo(BeatmapAction action)
    {
        if (!HandleActionEventUndoNoNotify(action) || atsc.IsPlaying) return;
        foreach (var manager in sortedPriorityManagers) manager.Reset();
        UpdateTime();
    }

    private bool HandleActionEventUndoNoNotify(BeatmapAction action)
    {
        return action switch
        {
            ActionCollectionAction actionCollectionAction => actionCollectionAction
                .Actions.ToArray()
                .Select(HandleActionEventUndoNoNotify)
                .Any(),
            BeatmapObjectPlacementAction beatmapObjectPlacementAction => HandlePlacementActionUndo(
                beatmapObjectPlacementAction),
            SelectionDeletedAction selectionDeletedAction => HandleSelectionDeletedActionUndo(selectionDeletedAction),
            SelectionPastedAction selectionPastedAction => HandleSelectionPastedActionUndo(selectionPastedAction),
            StrobeGeneratorGenerationAction strobeGeneratorGenerationAction =>
                HandleStrobeGeneratorGenerationActionUndo(
                    strobeGeneratorGenerationAction),
            BeatmapObjectDeletionAction beatmapObjectDeletionAction =>
                HandleDeletionActionUndo(beatmapObjectDeletionAction),
            BeatmapObjectModifiedWithConflictingAction beatmapObjectModifiedWithConflictingAction =>
                HandleModifiedWithConflictingActionUndo(beatmapObjectModifiedWithConflictingAction),
            BeatmapObjectModifiedAction beatmapObjectModifiedAction =>
                HandleModifiedActionUndo(beatmapObjectModifiedAction),
            BeatmapObjectModifiedCollectionAction beatmapObjectModifiedCollectionAction =>
                HandleModifiedCollectionActionUndo(beatmapObjectModifiedCollectionAction),
            _ => false
        };
    }

    private bool HandlePlacementActionUndo(BeatmapObjectPlacementAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .RemovedConflictObjects
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleSelectionDeletedActionUndo(SelectionDeletedAction action) =>
        AddEvents(
            action
                .Data
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());

    private bool HandleSelectionPastedActionUndo(SelectionPastedAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .Removed
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleStrobeGeneratorGenerationActionUndo(StrobeGeneratorGenerationAction action)
    {
        var b = RemoveEvents(
            action
                .Data
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .ConflictingData.Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleDeletionActionUndo(BeatmapObjectDeletionAction action) =>
        AddEvents(
            action
                .Data.Where(d => d is BaseEvent)
                .Cast<BaseEvent>());

    private bool HandleModifiedActionUndo(BeatmapObjectModifiedAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.EditedObject }
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleModifiedCollectionActionUndo(BeatmapObjectModifiedCollectionAction action)
    {
        var b = RemoveEvents(
            action
                .EditedObjects
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        return AddEvents(
                action
                    .OriginalObjects
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }

    private bool HandleModifiedWithConflictingActionUndo(BeatmapObjectModifiedWithConflictingAction action)
    {
        var b = RemoveEvents(
            new List<BaseObject> { action.EditedObject }
                .Where(d => d is BaseEvent)
                .Cast<BaseEvent>());
        b = AddEvents(
                new List<BaseObject> { action.OriginalObject }
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
        return AddEvents(
                action
                    .ConflictingObjects
                    .Where(d => d is BaseEvent)
                    .Cast<BaseEvent>())
            || b;
    }
}
