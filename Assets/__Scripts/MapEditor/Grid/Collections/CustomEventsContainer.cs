using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using System.Collections.ObjectModel;
using UnityEngine.InputSystem;
using System;

public class CustomEventsContainer : BeatmapObjectContainerCollection, CMInput.ICustomEventsContainerActions
{
    [SerializeField] private GameObject customEventPrefab;
    [SerializeField] private TextMeshProUGUI customEventLabelPrefab;
    [SerializeField] private Transform customEventLabelTransform;
    [SerializeField] private Transform[] customEventScalingOffsets;

    public override BeatmapObject.Type ContainerType => BeatmapObject.Type.CUSTOM_EVENT;

    public ReadOnlyCollection<string> CustomEventTypes => customEventTypes.AsReadOnly();
    private List<string> customEventTypes = new List<string>();

    private void Start()
    {
        RefreshTrack();
        if (!Settings.Instance.AdvancedShit)
        {
            Debug.LogWarning("Disabling some objects since an Advanced setting is not enabled...");
            foreach (Transform t in customEventScalingOffsets)
                t.gameObject.SetActive(false);
        }
    }

    public override IEnumerable<BeatmapObject> GrabSortedObjects()
    {
        return UnsortedObjects.OrderBy(x => x._time).ThenBy(x => (x as BeatmapCustomEvent)._type);
    }

    protected override void OnObjectSpawned(BeatmapObject obj)
    {
        BeatmapCustomEvent customEvent = obj as BeatmapCustomEvent;
        if (!customEventTypes.Contains(customEvent._type))
        {
            customEventTypes.Add(customEvent._type);
            RefreshTrack();
        }
    }

    private void RefreshTrack()
    {
        foreach (Transform t in customEventScalingOffsets)
        {
            Vector3 localScale = t.localScale;
            if (customEventTypes.Count == 0)
                t.gameObject.SetActive(false);
            else
            {
                t.gameObject.SetActive(true);
                t.localScale = new Vector3((customEventTypes.Count / 10f) + 0.01f, localScale.y, localScale.z);
            }
        }
        for (int i = 0; i < customEventLabelTransform.childCount; i++)
            Destroy(customEventLabelTransform.GetChild(i).gameObject);
        foreach(string str in customEventTypes)
        {
            TextMeshProUGUI newShit = Instantiate(customEventLabelPrefab.gameObject, customEventLabelTransform).GetComponent<TextMeshProUGUI>();
            newShit.rectTransform.localPosition = new Vector3(customEventTypes.IndexOf(str), 0.25f, 0);
            newShit.text = str;
        }
        foreach (BeatmapObjectContainer obj in LoadedContainers.Values) obj.UpdateGridPosition();
    }

    internal override void SubscribeToCallbacks()
    {
        LoadInitialMap.LevelLoadedEvent += SetInitialTracks;
    }

    private void SetInitialTracks()
    {
        foreach (BeatmapObject loadedObject in UnsortedObjects)
        {
            BeatmapCustomEvent customEvent = loadedObject as BeatmapCustomEvent;
            if (!customEventTypes.Contains(customEvent._type))
            {
                customEventTypes.Add(customEvent._type);
                RefreshTrack();
            }
        }
    }

    internal override void UnsubscribeToCallbacks()
    {
        LoadInitialMap.LevelLoadedEvent -= SetInitialTracks;
    }

    private void CreateNewType()
    {
        if (PersistentUI.Instance.InputBox_IsEnabled) return;
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

    public void OnAssignObjectstoTrack(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            PersistentUI.Instance.ShowInputBox("Assign the selected objects to a track ID.\n\n" +
            "If you dont know what you're doing, turn back now.", HandleTrackAssign);
        }
    }

    public void OnSetTrackFilter(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBox_IsEnabled) SetTrackFilter();
    }

    public void OnCreateNewEventType(InputAction.CallbackContext context)
    {
        if (Settings.Instance.AdvancedShit && context.performed && !PersistentUI.Instance.InputBox_IsEnabled) CreateNewType();
    }

    private void HandleTrackAssign(string res)
    {
        if (res is null) return;
        if (res == "")
        {
            foreach (BeatmapObject obj in SelectionController.SelectedObjects)
            {
                if (obj._customData == null) continue;
                obj._customData.Remove("_track");
            }
        }
        foreach (BeatmapObject obj in SelectionController.SelectedObjects)
        {
            obj.GetOrCreateCustomData()["_track"] = res;
        }
    }

    public override BeatmapObjectContainer CreateContainer() => BeatmapCustomEventContainer.SpawnCustomEvent(null, this, ref customEventPrefab);
}
