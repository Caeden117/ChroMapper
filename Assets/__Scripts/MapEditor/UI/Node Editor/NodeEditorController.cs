using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;

public class NodeEditorController : MonoBehaviour {
    
    [SerializeField] private Transform nodeContentTransform;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private float editableNodeStartY = 75f;
    [SerializeField] private float editableNodeX = -10f;
    [SerializeField] private float editableNodeSpacingY = 25f;

    private int nodeCount = 0;
    
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

    private void ObjectWasSelected(BeatmapObjectContainer container)
    {
        if (SelectionController.SelectedObjects.Count > 1) return;
        editingContainer = container; //Set node to what we are editing.
        editingNode = container.objectData.ConvertToJSON();

        string formattedName = container.objectData.beatmapType.ToString().Substring(0, 1); //Create a formatted string with the first character
        formattedName += container.objectData.beatmapType.ToString().ToLower().Substring(1); //capitalized, and the rest in lowercase.

        labelTextMesh.text = "Editing " + formattedName;
        
    }
}
