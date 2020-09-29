using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;
using UnityEngine.InputSystem;
using System.Reflection;
using UnityEngine.UI;

public class NodeEditorController : MonoBehaviour, CMInput.INodeEditorActions
{
    
    [SerializeField] private TMP_InputField nodeEditorInputField;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private Button closeButton;

    public static bool IsActive;
    public bool AdvancedSetting => Settings.Instance.NodeEditor_Enabled;
    private bool firstActive = true;

    private string oldInputText;
    private int oldCaretPosition;

    private BeatmapObjectContainer editingContainer;
    private BeatmapObject editingObject;
    private BeatmapObject.Type editingObjectType;
    private JSONNode editingNode;
    private bool isEditing;

    private readonly Type[] actionMapsEnabledWhenNodeEditing = new Type[]
    {
        typeof(CMInput.ICameraActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INodeEditorActions),
        typeof(CMInput.ISavingActions),
    };

    // I can just apply this to the places that need them but im feeling lazy lmao
    private Type[] actionMapsDisabled => typeof(CMInput).GetNestedTypes()
        .Where(x => x.IsInterface && !actionMapsEnabledWhenNodeEditing.Contains(x)).ToArray();

    // Use this for initialization
    private void Start () {
        SelectionController.ObjectWasSelectedEvent += ObjectWasSelected;
        SelectionController.SelectionPastedEvent += SelectionPasted;
    }

    private void OnDestroy()
    {
        SelectionController.ObjectWasSelectedEvent -= ObjectWasSelected;
        SelectionController.SelectionPastedEvent -= SelectionPasted;
    }

    private void Update()
    {
        if (!AdvancedSetting || UIMode.SelectedMode != UIModeType.NORMAL) return;
        if (SelectionController.SelectedObjects.Count == 0 && IsActive)
        {
            if (!Settings.Instance.NodeEditor_UseKeybind)
            {
                StopAllCoroutines();
                Close();
            }
            labelTextMesh.text = "Nothing Selected";
            nodeEditorInputField.text = "Please select an object to use Node Editor.";
        } else if (SelectionController.SelectedObjects.Count == 1 && !isEditing)
            ObjectWasSelected(SelectionController.SelectedObjects.First());
    }

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        IsActive = enabled;
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
    }

    public void ObjectWasSelected(BeatmapObject container)
    {
        if (!SelectionController.HasSelectedObjects() || container is null) return;
        BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();
        if (SelectionController.SelectedObjects.Count > 1) {
            if (!Settings.Instance.NodeEditor_UseKeybind && !AdvancedSetting)
            {
                StopAllCoroutines();
                Close();
            }
            else
            {
                labelTextMesh.text = "Too Many Objects";
                nodeEditorInputField.text = "Please select one (1) object to use Node Editor.";
            }
            isEditing = false;
            return;
        }
        isEditing = true;
        if (!Settings.Instance.NodeEditor_UseKeybind)
        {
            StopAllCoroutines();
            closeButton.gameObject.SetActive(false);
            StartCoroutine(UpdateGroup(true, transform as RectTransform));
            if (firstActive)
            {
                firstActive = false;
                PersistentUI.Instance.DisplayMessage("Mapper", "node.warning", PersistentUI.DisplayMessageType.BOTTOM);
            }
        }

        var collection = BeatmapObjectContainerCollection.GetCollectionForType(container.beatmapType);
        if (collection.LoadedContainers.TryGetValue(container, out editingContainer))
        {
            editingObject = container;
            editingObjectType = container.beatmapType;
            editingNode = container.ConvertToJSON();

            string[] splitName = container.beatmapType.ToString().Split('_');
            List<string> processedNames = new List<string>(splitName.Length);
            foreach (string unprocessedName in splitName)
            {
                string processedName = unprocessedName.Substring(0, 1); //Create a formatted string with the first character
                processedName += unprocessedName.ToLower().Substring(1); //capitalized, and the rest in lowercase.
                processedNames.Add(processedName);
            }
            string formattedName = string.Join(" ", processedNames);
            labelTextMesh.text = "Editing " + formattedName;
            nodeEditorInputField.text = string.Join("", editingNode.ToString(2).Split('\r'));
        }
    }

    private void SelectionPasted(IEnumerable<BeatmapObject> obj)
    {
        editingContainer = null;
        ObjectWasSelected(obj.FirstOrDefault());
    }

    public void NodeEditor_StartEdit(string content)
    {
        if (IsActive)
        {
            CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
            CMInputCallbackInstaller.DisableActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
            if (!nodeEditorInputField.isFocused) return;
            BeatmapAction lastAction = BeatmapActionContainer.GetLastAction();
            if (lastAction != null && lastAction is NodeEditorTextChangedAction textChangedAction)
            {
                if (content != textChangedAction.CurrentText && content != textChangedAction.OldText)
                {
                    BeatmapActionContainer.AddAction(new NodeEditorTextChangedAction(content, nodeEditorInputField.caretPosition,
                        oldInputText, oldCaretPosition, nodeEditorInputField));
                }
            }
            else
            {
                BeatmapActionContainer.AddAction(new NodeEditorTextChangedAction(content, nodeEditorInputField.caretPosition,
                        content, nodeEditorInputField.caretPosition, nodeEditorInputField));
            }
        }
        oldInputText = content;
        oldCaretPosition = nodeEditorInputField.caretPosition;
    }

    public void NodeEditor_EndEdit(string nodeText)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
        try
        {
            if (!isEditing || !IsActive || SelectionController.SelectedObjects.Count != 1) return;
            JSONNode newNode = JSON.Parse(nodeText); //Parse JSON, and do some basic checks.
            if (string.IsNullOrEmpty(newNode.ToString())) //Damn you Jackz
                throw new Exception("Node cannot be empty.");

            BeatmapObject original = BeatmapObject.GenerateCopy(editingObject);

            //Let's create objects here so that if any exceptions happen, it will not disrupt the node editing process.
            BeatmapObject newObject = Activator.CreateInstance(editingObject.GetType(), new object[] { newNode }) as BeatmapObject;

            //From this point on, its the mappers fault for whatever shit happens from JSON.
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(editingObjectType);

            SelectionController.Deselect(editingContainer.objectData);
            collection.DeleteObject(editingContainer.objectData, false);

            collection.SpawnObject(newObject, true);
            SelectionController.Select(newObject, false, true, false);

            editingObject = newObject;
            editingContainer = collection.LoadedContainers[newObject];
            editingNode = newNode;

            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(newObject, original, $"Edited a {editingObject.beatmapType} with Node Editor."));
            //UpdateAppearance(editingContainer);
        }
        catch (Exception e)
        {
            string message = e.Message;
            switch (e)
            {
                case JSONParseException jsonParse: // Error parsing input JSON; tell them what's wrong!
                    message = jsonParse.ToUIFriendlyString();
                    break;
                case TargetInvocationException invocationException: // Error when converting JSON to an object; tell them what's wrong!
                    message = invocationException.InnerException.Message;
                    break;
                default:
                    //Log the full error to the console
                    Debug.LogError(e);
                    break;
            }
            PersistentUI.Instance.ShowDialogBox(message, null, PersistentUI.DialogBoxPresetType.Ok);
        }
    }

    public void Close()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
        StartCoroutine(UpdateGroup(false, transform as RectTransform));
        isEditing = false;
    }

    public void OnToggleNodeEditor(InputAction.CallbackContext context)
    {
        if (nodeEditorInputField.isFocused) return;
        if (Settings.Instance.NodeEditor_UseKeybind && AdvancedSetting && context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            StopAllCoroutines();
            if (IsActive)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
                BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();
            }
            else
            {
                closeButton.gameObject.SetActive(true);
                CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
            }
            StartCoroutine(UpdateGroup(!IsActive, transform as RectTransform));
        }
    }
}
