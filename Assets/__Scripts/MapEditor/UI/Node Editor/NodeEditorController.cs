using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;

public class NodeEditorController : MonoBehaviour {

    [SerializeField] private GameObject editableNodePrefab;
    [SerializeField] private Transform nodeContentTransform;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private float editableNodeStartY = 75f;
    [SerializeField] private float editableNodeX = -10f;
    [SerializeField] private float editableNodeSpacingY = 25f;

    private int nodeCount = 0;

    private List<NodeEditorNode> nodes = new List<NodeEditorNode>();
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

        for (int i = 0; i < nodeContentTransform.childCount; i++) //Clear previous child nodes.
            Destroy(nodeContentTransform.GetChild(i).gameObject);

        string formattedName = container.objectData.beatmapType.ToString().Substring(0, 1); //Create a formatted string with the first character
        formattedName += container.objectData.beatmapType.ToString().ToLower().Substring(1); //capitalized, and the rest in lowercase.

        labelTextMesh.text = "Editing " + formattedName;
        JSONNode.Enumerator nodeEnum = editingNode.GetEnumerator();
        while (nodeEnum.MoveNext())
        {
            string key = nodeEnum.Current.Key;
            JSONNode node = nodeEnum.Current.Value;
            GameObject instantiate = Instantiate(editableNodePrefab, nodeContentTransform);
            (instantiate.transform as RectTransform).anchoredPosition = new Vector2(editableNodeX, editableNodeStartY - (editableNodeSpacingY * nodeCount));
            nodeCount++;
            instantiate.GetComponent<NodeEditorNode>().SetValues(key, node);
        }
    }
}
