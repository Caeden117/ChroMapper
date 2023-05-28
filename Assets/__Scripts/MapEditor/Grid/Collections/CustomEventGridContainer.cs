using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
    private List<string> customEventTypes = new List<string>();

    public override ObjectType ContainerType => ObjectType.CustomEvent;

    public ReadOnlyCollection<string> CustomEventTypes => customEventTypes.AsReadOnly();

    public Dictionary<string, List<BaseCustomEvent>> EventsByTrack;

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
