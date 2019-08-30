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
        (transform as RectTransform).anchoredPosition = Vector2.Lerp((transform as RectTransform).anchoredPosition,
            new Vector2(0, (IsActive && AdvancedSetting) ? 0 : -200), 0.1f);
        if (SelectionController.SelectedObjects.Count == 0 && IsActive) IsActive = false;
    }

    public void ObjectWasSelected(BeatmapObjectContainer container)
    {
        if (SelectionController.SelectedObjects.Count > 1 || !AdvancedSetting || SelectionController.HasCopiedObjects()) {
            IsActive = false;
            return;
        };
        IsActive = true;
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
            if (nodeText == "" || newNode == null || newNode["_time"] == null || newNode["_time"].Value == "")
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
        catch (System.Exception e) { PersistentUI.Instance.DisplayMessage(e.Message, PersistentUI.DisplayMessageType.BOTTOM); }
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
        AdvancedSetting = enabled;
    }

    public void Close()
    {
        IsActive = false;
    }
}
