using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;

public class NodeEditorController : MonoBehaviour {
    
    [SerializeField] private TMP_InputField nodeEditorInputField;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;

    public static bool IsActive = false;
    public bool AdvancedSetting = false;
    private bool firstActive = true;

    private BeatmapObjectContainer editingContainer;
    private JSONNode editingNode;

	// Use this for initialization
	private void Start () {
        SelectionController.ObjectWasSelectedEvent += ObjectWasSelected;
	}

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectWasSelected;
    }

    private void Update()
    {
        if (SelectionController.SelectedObjects.Count == 0 && IsActive)
        {
            StopAllCoroutines();
            StartCoroutine(UpdateGroup(false, transform as RectTransform));
        }
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        float dest = enabled ? 5 : -200;
        float og = group.anchoredPosition.y;
        float t = 0;
        while (t < 1)
        {
            t += Time.deltaTime;
            group.anchoredPosition = new Vector2(group.anchoredPosition.x, Mathf.Lerp(og, dest, t));
            og = group.anchoredPosition.y;
            yield return new WaitForEndOfFrame();
        }
        group.anchoredPosition = new Vector2(group.anchoredPosition.x, dest);
        IsActive = enabled;
    }

    public void ObjectWasSelected(BeatmapObjectContainer container)
    {
        if (SelectionController.SelectedObjects.Count > 1 || !AdvancedSetting) {
            StopAllCoroutines();
            StartCoroutine(UpdateGroup(false, transform as RectTransform));
            return;
        };
        StopAllCoroutines();
        StartCoroutine(UpdateGroup(true, transform as RectTransform));
        if (firstActive)
        {
            firstActive = false;
            PersistentUI.Instance.DisplayMessage("Node Editor is very powerful - Be careful!", PersistentUI.DisplayMessageType.BOTTOM);
        }
        editingContainer = container; //Set node to what we are editing.
        editingNode = container.objectData.ConvertToJSON();

        string formattedName = container.objectData.beatmapType.ToString().Substring(0, 1); //Create a formatted string with the first character
        formattedName += container.objectData.beatmapType.ToString().ToLower().Substring(1); //capitalized, and the rest in lowercase.

        labelTextMesh.text = "Editing " + formattedName;
        nodeEditorInputField.text = editingNode.ToString(2);
    }

    public void NodeEditor_EndEdit(string nodeText)
    {
        try
        {
            if (!IsActive) return;
            JSONNode newNode = JSON.Parse(nodeText); //Parse JSON, and do some basic checks.
            if (string.IsNullOrEmpty(newNode)  || string.IsNullOrEmpty(newNode["_time"]))
                throw new System.Exception("Invalid JSON!");

            //From this point on, its the mappers fault for whatever shit happens from JSON.

            JSONNode original = editingContainer.objectData.ConvertToJSON();
            BeatmapActionContainer.AddAction(new NodeEditorUpdatedNodeAction(editingContainer, newNode, original));

            if (editingContainer is BeatmapNoteContainer note)
                note.mapNoteData = new BeatmapNote(newNode);
            else if (editingContainer is BeatmapEventContainer e)
                e.eventData = new MapEvent(newNode);
            else if (editingContainer is BeatmapObstacleContainer o)
                o.obstacleData = new BeatmapObstacle(newNode);
            else if (editingContainer is BeatmapBPMChangeContainer b)
                b.bpmData = new BeatmapBPMChange(newNode);
            UpdateAppearance(editingContainer);
        }
        catch (System.Exception e) { PersistentUI.Instance.ShowDialogBox(e.Message, null, PersistentUI.DialogBoxPresetType.Ok); }
    }

    public void UpdateAppearance(BeatmapObjectContainer obj)
    {
        if (obj is BeatmapNoteContainer note)
        {
            note.Directionalize(note.mapNoteData._cutDirection);
            noteAppearance.SetNoteAppearance(note);
        }
        else if (obj is BeatmapEventContainer e)
            eventAppearance.SetEventAppearance(e);
        else if (obj is BeatmapObstacleContainer o)
            obstacleAppearance.SetObstacleAppearance(o);
        obj.UpdateGridPosition();
        SelectionController.RefreshMap();
    }

    public void UpdateAdvancedSetting(bool enabled)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        AdvancedSetting = enabled;
    }

    public void Close()
    {
        StartCoroutine(UpdateGroup(false, transform as RectTransform));
    }
}
