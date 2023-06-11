using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Beatmap.Animations;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Enums;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class CustomEventGridContainer : BeatmapObjectContainerCollection, CMInput.ICustomEventsContainerActions
{
    [SerializeField] private GameObject customEventPrefab;
    [SerializeField] private TextMeshProUGUI customEventLabelPrefab;
    [SerializeField] private Transform customEventLabelTransform;
    [SerializeField] private Transform[] customEventScalingOffsets;
    [SerializeField] private TracksManager tracksManager;
    [SerializeField] private ObjectAnimator cameraAnimator;
    [SerializeField] private GameObject playerCamera;
    private List<string> customEventTypes = new List<string>();

    public override ObjectType ContainerType => ObjectType.CustomEvent;

    public ReadOnlyCollection<string> CustomEventTypes => customEventTypes.AsReadOnly();

    public Dictionary<string, List<BaseCustomEvent>> EventsByTrack;

    private List<float> playerTrackTimes;
    private List<TrackAnimator> playerTracks;
    private TrackAnimator currentTrack;

    private void Start()
    {
        RefreshTrack();
        if (!Settings.Instance.AdvancedShit)
        {
            Debug.LogWarning("Disabling some objects since an Advanced setting is not enabled...");
            foreach (var t in customEventScalingOffsets)
                t.gameObject.SetActive(false);
        }
    }

    // Maybe belongs in CameraController?
    private void Update()
    {
        if ((playerTrackTimes?.Count ?? 0) == 0) return;

        // 1 after last point, inverted (probably)
        var later = playerTrackTimes.BinarySearch(AudioTimeSyncController.CurrentJsonTime);

        var current = (later < 0)
            ? (~later) - 1
            : later;

        if (current < 0)
        {
            DisconnectPlayerTrack();
            return;
        }

        if (playerTracks[current] != currentTrack)
        {
            DisconnectPlayerTrack();
            currentTrack = playerTracks[current];
            cameraAnimator.LocalTarget = cameraAnimator.AnimationThis.transform;
            cameraAnimator.WorldTarget = cameraAnimator.transform;
            cameraAnimator.enabled = true;
            cameraAnimator.ResetData();

            currentTrack.children.Add(cameraAnimator);
            currentTrack.OnChildrenChanged();
        }
    }

    // During refresh? How to update when events are added
    public void LoadAnimationTracks()
    {
        playerTrackTimes = new List<float>();
        playerTracks = new List<TrackAnimator>();
        currentTrack = null;

        var events = LoadedObjects.Select(ev => ev as BaseCustomEvent);
        foreach (var ev in events)
        {
            var tracks = ev.CustomTrack switch {
                JSONArray arr => arr,
                JSONString s => JSONObject.Parse($"[{s.ToString()}]").AsArray,
                _ => null,
            };
            switch (ev.Type)
            {
            case "AssignTrackParent":
                var parent = tracksManager.CreateAnimationTrack(ev.DataParentTrack);
                tracks = ev.DataChildrenTracks switch {
                    JSONArray arr => arr,
                    JSONString s => JSONObject.Parse($"[{s.ToString()}]").AsArray,
                };
                foreach (var tr in tracks)
                {
                    var at = tracksManager.CreateAnimationTrack(tr.Value);
                    at.track.transform.parent = parent.track.ObjectParentTransform;
                    if (at.animator == null)
                    {
                        at.animator = at.gameObject.AddComponent<ObjectAnimator>();
                        at.animator.SetTrack(at.track, tr.Value);
                    }

                    if (!parent.children.Contains(at.animator))
                    {
                        parent.children.Add(at.animator);
                        at.parents.Add(parent);
                        at.OnChildrenChanged();
                    }
                }
                break;
            case "AssignPlayerToTrack":
                playerCamera.SetActive(true);
                var track = tracksManager.CreateAnimationTrack(ev.CustomTrack);
                playerTrackTimes.Add(ev.JsonTime);
                playerTracks.Add(track);
                break;
            }
        }
    }

    private void DisconnectPlayerTrack()
    {
        if (currentTrack == null) return;

        currentTrack.children.Remove(cameraAnimator);
        currentTrack.OnChildrenChanged();
        currentTrack = null;

        cameraAnimator.ResetData();
        cameraAnimator.enabled = false;
    }

    public void OnAssignObjectstoTrack(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
        {
            PersistentUI.Instance.ShowInputBox("Assign the selected objects to a track ID.\n\n" +
                                               "If you dont know what you're doing, turn back now.", HandleTrackAssign);
        }
    }

    public void OnSetTrackFilter(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            SetTrackFilter();
    }

    public void OnCreateNewEventType(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
            CreateNewType();
    }

    public override IEnumerable<BaseObject> GrabSortedObjects() =>
        UnsortedObjects.OrderBy(x => x.JsonTime).ThenBy(x => (x as BaseCustomEvent).Type);

    public void RefreshEventsByTrack()
    {
        EventsByTrack = new Dictionary<string, List<BaseCustomEvent>>();

        foreach (var loadedObject in UnsortedObjects)
        {
            var customEvent = loadedObject as BaseCustomEvent;
            List<string> tracks = customEvent.CustomTrack switch {
                JSONString s => new List<string> { s },
                JSONArray arr => new List<string>(arr.Children.Select(c => (string)c)),
                _ => new List<string>()
            };
            foreach (var track in tracks)
            {
                if (!EventsByTrack.ContainsKey(track))
                {
                    EventsByTrack[track] = new List<BaseCustomEvent>();
                }
                EventsByTrack[track].Add(customEvent);
            }
        }

        foreach (var track in EventsByTrack)
        {
            var at = tracksManager.CreateAnimationTrack(track.Key);
            at.SetEvents(track.Value.Where(ev => ev.Type == "AnimateTrack").ToList());
        }
    }

    protected override void OnObjectSpawned(BaseObject obj, bool inCollection = false)
    {
        var customEvent = obj as BaseCustomEvent;
        if (!customEventTypes.Contains(customEvent.Type))
        {
            customEventTypes.Add(customEvent.Type);
            RefreshTrack();
        }
    }

    private void RefreshTrack()
    {
        foreach (var t in customEventScalingOffsets)
        {
            var localScale = t.localScale;
            if (customEventTypes.Count == 0)
            {
                t.gameObject.SetActive(false);
            }
            else
            {
                t.gameObject.SetActive(true);
                t.localScale = new Vector3((customEventTypes.Count / 10f) + 0.01f, localScale.y, localScale.z);
            }
        }

        for (var i = 0; i < customEventLabelTransform.childCount; i++)
            Destroy(customEventLabelTransform.GetChild(i).gameObject);
        foreach (var str in customEventTypes)
        {
            var newShit = Instantiate(customEventLabelPrefab.gameObject, customEventLabelTransform)
                .GetComponent<TextMeshProUGUI>();
            newShit.rectTransform.localPosition = new Vector3(customEventTypes.IndexOf(str), 0.25f, 0);
            newShit.text = str;
        }

        foreach (var obj in LoadedContainers.Values) obj.UpdateGridPosition();
    }

    internal override void SubscribeToCallbacks() => LoadInitialMap.LevelLoadedEvent += SetInitialTracks;

    private void SetInitialTracks()
    {
        foreach (var loadedObject in UnsortedObjects)
        {
            var customEvent = loadedObject as BaseCustomEvent;
            if (!customEventTypes.Contains(customEvent.Type))
            {
                customEventTypes.Add(customEvent.Type);
                RefreshTrack();
            }
        }
    }

    internal override void UnsubscribeToCallbacks() => LoadInitialMap.LevelLoadedEvent -= SetInitialTracks;

    private void CreateNewType()
    {
        if (PersistentUI.Instance.InputBoxIsEnabled) return;
        PersistentUI.Instance.ShowInputBox("A new custom event type, I see?\n\n" +
                                           "Custom event types are for the advanced of advanced users. Node Editor and JSON knowledge are required for these babies.\n\n" +
                                           "If you dont know what these do, or don't have the documentation for them, turn back now.\n\n" +
                                           "But if you do, what would you like to name this new event type?",
            HandleNewTypeCreation, "NewCustomEventType");
    }

    private void HandleNewTypeCreation(string res)
    {
        if (string.IsNullOrEmpty(res) || string.IsNullOrWhiteSpace(res)) return;
        customEventTypes.Add(res);
        customEventTypes = customEventTypes.OrderBy(x => x).ToList();
        RefreshTrack();
    }

    private void HandleTrackAssign(string res)
    {
        if (res is null) return;
        if (res == "")
        {
            foreach (var obj in SelectionController.SelectedObjects)
            {
                if (obj.CustomData == null) continue;
                obj.CustomData.Remove("_track");
            }
        }

        // TODO: deal with track
        foreach (var obj in SelectionController.SelectedObjects) obj.CustomTrack = res;
    }

    public override ObjectContainer CreateContainer() =>
        CustomEventContainer.SpawnCustomEvent(null, this, ref customEventPrefab);
}
