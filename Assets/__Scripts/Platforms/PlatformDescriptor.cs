using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Beatmap.Base;
using Beatmap.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

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

    private readonly Dictionary<int, List<PlatformEventHandler>> platformEventHandlers = new();

    private AudioTimeSyncController atsc;
    private ColorBoostManager colorBoostManager;

    private BeatmapObjectCallbackController callbackController;
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

    private void Start()
    {
        var eventHandlers = GetComponentsInChildren<PlatformEventHandler>();

        foreach (var handler in eventHandlers)
        {
            foreach (var type in handler.ListeningEventTypes)
            {
                if (!platformEventHandlers.TryGetValue(type, out var list))
                {
                    list = new List<PlatformEventHandler>();
                    platformEventHandlers.Add(type, list);
                }

                list.Add(handler);
            }
        }

        UpdateShinyMaterialSettings();
    }

    private void OnDestroy()
    {
        BeatmapActionContainer.ActionCreatedEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionRedoEvent -= HandleActionEventRedo;
        BeatmapActionContainer.ActionUndoEvent -= HandleActionEventUndo;
        if (callbackController != null) callbackController.EventPassedThreshold -= EventPassed;
        if (atsc != null) atsc.TimeChanged -= UpdateTime;
        if (SceneManager.GetActiveScene().name != "999_PrefabBuilding") LoadInitialMap.LevelLoadedEvent -= LevelLoaded;
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
        callbackController = GameObject.Find("Vertical Grid Callback").GetComponent<BeatmapObjectCallbackController>();

        rotationCallback = Resources.FindObjectsOfTypeAll<RotationCallbackController>().First();
        atsc = rotationCallback.Atsc;
        if (RotationController != null)
        {
            RotationController.RotationCallback = rotationCallback;
            RotationController.Init();
        }

        callbackController.EventPassedThreshold += EventPassed;
        atsc.TimeChanged += UpdateTime;
        RefreshLightingManagers();

        if (Settings.Instance.HideDisablableObjectsOnLoad) ToggleDisablableObjects();
    }

    public void RefreshLightingManagers()
    {
        colorBoostManager.Atsc = atsc;
        BuildBoostEvent();

        for (var i = 0; i < LightingManagers.Length; i++)
        {
            var manager = LightingManagers[i];
            if (manager is null) continue;
            manager.Atsc = atsc;
            manager.SetColors(ColorScheme);
            colorBoostManager.OnStateChange += manager.ToggleBoost;
            BasicLightManager.FlashTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FlashTimeSecond);
            BasicLightManager.FadeTimeBeat = atsc.GetBeatFromSeconds(BasicLightManager.FadeTimeSecond);
            StartCoroutine(BuildLightEvent(manager, i)); // where is my proper platform loaded
        }
    }

    private void BuildBoostEvent() =>
        colorBoostManager.BuildFromEvents(BeatSaberSongContainer.Instance.Map.Events.Where(e => e.Type == 5));

    private IEnumerator BuildLightEvent(BasicLightManager manager, int type)
    {
        yield return new WaitForSeconds(0.05f);
        manager.BuildFromEvents(BeatSaberSongContainer.Instance.Map.Events.Where(e => e.Type == type));
    }

    public void UpdateTime()
    {
        // if (atsc.IsPlaying) return; // maybe lateupdate, maybe callback
        var time = atsc.CurrentSongBpmTime;
        colorBoostManager.UpdateTime(time);
        foreach (var manager in LightingManagers)
        {
            if (manager == null) continue;
            manager.UpdateTime(time);
        }
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
            if (baseEvent.Type < LightingManagers.Length)
            {
                var manager = LightingManagers[baseEvent.Type];
                if (manager != null) manager.InsertEvent(baseEvent);
            }

            if (baseEvent.Type == 5) colorBoostManager.InsertEvent(baseEvent);
            mark = true;
        }

        return mark;
    }

    private bool RemoveEvents(IEnumerable<BaseEvent> events)
    {
        var mark = false;
        foreach (var baseEvent in events)
        {
            if (baseEvent.Type < LightingManagers.Length)
            {
                var manager = LightingManagers[baseEvent.Type];
                if (manager != null) manager.RemoveEvent(baseEvent);
            }

            if (baseEvent.Type == 5) colorBoostManager.RemoveEvent(baseEvent);
            mark = true;
        }

        return mark;
    }

    private void HandleActionEventRedo(BeatmapAction action)
    {
        if (!HandleActionEventRedoNoNotify(action) || atsc.IsPlaying) return;
        colorBoostManager.ResetState();
        foreach (var manager in LightingManagers.Where(manager => manager != null)) manager.ResetState();
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
        colorBoostManager.ResetState();
        foreach (var manager in LightingManagers.Where(manager => manager != null)) manager.ResetState();
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

    public void EventPassed(bool isPlaying, int index, BaseObject obj)
    {
        var e = obj as BaseEvent;

        // Two events at the same time should yield same results
        Random.InitState(Mathf.RoundToInt(obj.JsonTime * 100));

        // FUN PART BOIS
        switch (e.Type)
        {
            case 8:
                if (e.CustomNameFilter != null)
                {
                    string filter = e.CustomNameFilter;
                    if (filter.Contains("Big") || filter.Contains("Large"))
                    {
                        if (BigRingManager != null) BigRingManager.HandleRotationEvent(e);
                    }
                    else if (filter.Contains("Small") || filter.Contains("Panels") || filter.Contains("Triangle"))
                    {
                        if (SmallRingManager != null) SmallRingManager.HandleRotationEvent(e);
                    }
                    else
                    {
                        if (BigRingManager != null) BigRingManager.HandleRotationEvent(e);
                        if (SmallRingManager != null) SmallRingManager.HandleRotationEvent(e);
                    }
                }
                else
                {
                    if (BigRingManager != null) BigRingManager.HandleRotationEvent(e);
                    if (SmallRingManager != null) SmallRingManager.HandleRotationEvent(e);
                }

                break;
            case 9:
                if (BigRingManager != null) BigRingManager.HandlePositionEvent(e);
                if (SmallRingManager != null) SmallRingManager.HandlePositionEvent(e);
                break;
            case 12:
                if (HandleGagaHeightEvent(e)) return;

                var leftEventTypes = new List<int>()
                {
                    (int)EventTypeValue.LeftLasers,
                    (int)EventTypeValue.ExtraLeftLasers,
                    (int)EventTypeValue.ExtraLeftLights
                };

                foreach (var eventType in leftEventTypes.Where(eventType => LightingManagers.Length >= eventType))
                {
                    foreach (var l in LightingManagers[eventType].RotatingLights)
                    {
                        l.UpdateOffset(true, e);
                    }
                }

                break;
            case 13:
                if (HandleGagaHeightEvent(e)) return;

                var rightEventTypes = new List<int>()
                {
                    (int)EventTypeValue.RightLasers,
                    (int)EventTypeValue.ExtraRightLasers,
                    (int)EventTypeValue.ExtraRightLights
                };

                foreach (var eventType in rightEventTypes.Where(eventType => LightingManagers.Length >= eventType))
                {
                    foreach (var l in LightingManagers[eventType].RotatingLights)
                    {
                        l.UpdateOffset(true, e);
                    }
                }

                break;
            case 16:
            case 17:
            case 18:
            case 19:
                if (HandleGagaHeightEvent(e)) return;
                break;
        }

        if (atsc != null && atsc.IsPlaying && platformEventHandlers.TryGetValue(e.Type, out var eventHandlers))
        {
            foreach (var handler in eventHandlers) handler.OnEventTrigger(e.Type, e);
        }
    }

    // Handle Gaga Env Height events, and pass it to the case so it can ignore anything after if the manager is valid.
    private bool HandleGagaHeightEvent(BaseEvent evt)
    {
        if (DiskManager != null)
        {
            DiskManager.HandlePositionEvent(evt);
            return true;
        }
        else
            return false;
    }
}
