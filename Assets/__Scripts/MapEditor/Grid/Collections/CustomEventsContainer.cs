using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using System.Collections.ObjectModel;

public class CustomEventsContainer : BeatmapObjectContainerCollection
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
            foreach (Transform t in customEventScalingOffsets)
                t.gameObject.SetActive(false);
        }
    }

    public override void SortObjects()
    {
        LoadedContainers = LoadedContainers.OrderBy(x => x.objectData._time).ThenBy(x => (x.objectData as BeatmapCustomEvent)?._type).ToList();
        customEventTypes = customEventTypes.OrderBy(x => x).ToList();
        RefreshTrack();
        UseChunkLoading = true;
    }

    public override BeatmapObjectContainer SpawnObject(BeatmapObject obj, out BeatmapObjectContainer conflicting, bool removeConflicting = true, bool refreshMap = true)
    {
        conflicting = null;
        if (!customEventTypes.Contains((obj as BeatmapCustomEvent)?._type))
        {
            customEventTypes.Add((obj as BeatmapCustomEvent)?._type);
            RefreshTrack();
        }
        BeatmapCustomEventContainer beatmapCustomEvent = BeatmapCustomEventContainer.SpawnCustomEvent(obj as BeatmapCustomEvent, this, ref customEventPrefab);
        beatmapCustomEvent.transform.SetParent(GridTransform);
        beatmapCustomEvent.UpdateGridPosition();
        LoadedContainers.Add(beatmapCustomEvent);
        if (refreshMap) SelectionController.RefreshMap();
        return beatmapCustomEvent;
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
        foreach (BeatmapObjectContainer obj in LoadedContainers) obj.UpdateGridPosition();
    }

    internal override void SubscribeToCallbacks() { }

    internal override void UnsubscribeToCallbacks() { }

    public void CreateNewType()
    {
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
        SortObjects();
    }
}
