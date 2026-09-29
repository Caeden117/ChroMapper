using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using ZLinq;
using System.Reflection;
using System.Text.RegularExpressions;
using Beatmap.Base;
using Beatmap.Helper;
using SimpleJSON;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class NodeEditorController : MonoBehaviour, CMInput.INodeEditorActions
{
    public static bool IsActive;

    [SerializeField] private TMP_InputField nodeEditorInputField;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private Button closeButton;
    [SerializeField] private FlyoutPanelController flyoutPanelController;

    private readonly Type[] actionMapsEnabledWhenNodeEditing =
    {
        typeof(CMInput.ICameraActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INodeEditorActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.ITimelineActions)
    };

    private JSONNode editingNode;

    private List<BaseObject> editingObjects;
    private bool firstActive = true;

    private int height = 205;
    private bool isEditing;
    private bool queuedUpdate;

    // I can just apply this to the places that need them but im feeling lazy lmao
    private Type[] ActionMapsDisabled =>
        typeof(CMInput)
            .GetNestedTypes()
            .Where(x => x.IsInterface && !actionMapsEnabledWhenNodeEditing.Contains(x))
            .ToArray();

    // Use this for initialization
    private void Start() => SelectionController.OnSelectionChanged += ObjectWasSelected;

    private void Update()
    {
        if (UIMode.SelectedMode != UIModeType.Normal) return;
        if (!SelectionController.HasSelectedObjects() && IsActive)
        {
            if (!Settings.Instance.NodeEditor_UseKeybind)
            {
                StopAllCoroutines();
                Close();
            }

            labelTextMesh.text = "Nothing Selected";
            nodeEditorInputField.text = "Please select an object to use Node Editor.";
        }
    }

    private void OnDestroy()
    {
        IsActive = false;
        SelectionController.OnSelectionChanged -= ObjectWasSelected;
    }

    public void OnToggleNodeEditor(InputAction.CallbackContext context)
    {
        if (nodeEditorInputField.isFocused || UIMode.PreviewMode) return;
        if (Settings.Instance.NodeEditor_UseKeybind && context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
        {
            StopAllCoroutines();
            if (IsActive)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(
                    typeof(NodeEditorController),
                    new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), ActionMapsDisabled);
            #pragma warning disable CS0618 // 'NodeEditorTextChangedAction' is obsolete: 'Undo/Redo is disabled when node editor is open anyway'
                BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();
            #pragma warning restore CS0618 // 'NodeEditorTextChangedAction' is obsolete: 'Undo/Redo is disabled when node editor is open anyway'
            }
            else
            {
                closeButton.gameObject.SetActive(true);
                CMInputCallbackInstaller.DisableActionMaps(typeof(NodeEditorController), ActionMapsDisabled);
            }

            UpdateGroup(!IsActive);
        }
    }

    private void UpdateGroup(bool enabled)
    {
        IsActive = enabled;
        if (enabled)
        {
            if (queuedUpdate) ObjectWasSelected();

            height = Mathf.FloorToInt(Settings.Instance.NodeEditorSize * 20.5f);

            GetComponent<RectTransform>().sizeDelta = new Vector2(300, height);
            flyoutPanelController.UpdateEndingOffset(new Vector2(0, height));

            nodeEditorInputField.pointSize = Settings.Instance.NodeEditorTextSize;
        }

        if (IsActive)
        {
            flyoutPanelController.Open();
        }
        else
        {
            flyoutPanelController.Close();
        }
    }

    public void ObjectWasSelected()
    {
        queuedUpdate = !IsActive;
        if (queuedUpdate) return;

        if (!SelectionController.HasSelectedObjects())
        {
            isEditing = false;
            return;
        }

    #pragma warning disable CS0618 // 'NodeEditorTextChangedAction' is obsolete: 'Undo/Redo is disabled when node editor is open anyway'
        BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();
    #pragma warning restore CS0618 // 'NodeEditorTextChangedAction' is obsolete: 'Undo/Redo is disabled when node editor is open anyway'

        isEditing = true;
        if (!Settings.Instance.NodeEditor_UseKeybind)
        {
            StopAllCoroutines();
            closeButton.gameObject.SetActive(false);
            UpdateGroup(true);
            if (firstActive)
            {
                firstActive = false;
                PersistentUI.Instance.DisplayMessage("Mapper", "node.warning", PersistentUI.DisplayMessageType.Bottom);
            }
        }

        UpdateJson();
    }

    private void UpdateJson()
    {
        editingObjects = SelectionController.SelectedObjects.AsValueEnumerable().Select(it => it).ToList();

        // FloatFX groups serialize through the same node-editor JSON path as other GLS objects, so do not block them here.
        // Log the raw JSON before shared-value processing because GLS nodes and groups can have version-specific serializers.
        var rawJsonNodes = editingObjects
            .Select(obj =>
            {
                var json = obj.ToJson();
                // Debug.Log($"[NodeEditor JSON] Object={obj.GetType().Name} ObjectType={obj.ObjectType} MapVersion={Settings.Instance.MapVersion} JsonNull={json == null} Json={json}");
                return json?.Clone() ?? new JSONObject();
            })
            .ToList();
        editingNode = GetSharedJson(rawJsonNodes);
        // Debug.Log($"[NodeEditor JSON] Shared ObjectCount={editingObjects.Count} ResultNull={editingNode == null} ResultType={editingNode?.Tag} Result={editingNode}");

        var formattedJson = string.Join("", editingNode?.ToString(2).Split('\r') ?? Array.Empty<string>());
        nodeEditorInputField.SetTextWithoutNotify(formattedJson);
        // Debug.Log($"[NodeEditor JSON] InputFieldAssigned Length={formattedJson.Length} ActualLength={nodeEditorInputField.text.Length}");

        if (editingObjects.Count == 1)
        {
            var obj = editingObjects.First();

            var splitName = obj.ObjectType.ToString().Split('_');
            var processedNames = new List<string>(splitName.Length);
            foreach (var unprocessedName in splitName)
            {
                var processedName =
                    unprocessedName.Substring(0, 1); //Create a formatted string with the first character
                processedName += unprocessedName.ToLower().Substring(1); //capitalized, and the rest in lowercase.
                processedNames.Add(processedName);
            }

            var formattedName = string.Join(" ", processedNames);
            labelTextMesh.text = "Editing " + formattedName;
        }
        else
        {
            labelTextMesh.text = $"Editing ({editingObjects.Count}) objects";
        }
    }

    public void NodeEditor_StartEdit(string content)
    {
        if (IsActive)
        {
            if (!CMInputCallbackInstaller.IsActionMapDisabled(ActionMapsDisabled[0]))
            {
                CMInputCallbackInstaller.DisableActionMaps(
                    typeof(NodeEditorController),
                    new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.DisableActionMaps(typeof(NodeEditorController), ActionMapsDisabled);
            }
        }
    }

    public void NodeEditor_EndEdit(string nodeText)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(
            typeof(NodeEditorController),
            new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), ActionMapsDisabled);

        try
        {
            if (!isEditing || !IsActive) return;
            var visibleText = Regex.Replace(nodeText, @"\p{C}+", string.Empty);
            var newNode = JSON.Parse(visibleText); //Parse JSON, and do some basic checks.
            if (string.IsNullOrEmpty(newNode.ToString())) //Damn you Jackz
                throw new Exception("Node cannot be empty.");

            var objectJsonMap = editingObjects
                .ToDictionary(
                    obj => obj,
                    obj => obj.ToJson().Clone()); // the clone is needed because custom data is a reference in toJson

            ApplyJson(editingNode.AsObject, newNode.AsObject, objectJsonMap);

            // Inner GLS nodes are owned by their group, unlike every ordinary Node Editor object.
            if (editingObjects.All(obj => obj is BaseGLSEvent))
            {
                ApplyInnerGlsEdit(objectJsonMap);
            }
            else
            {
                var editedObjects = objectJsonMap
                    .Select(pair => CreateEditedObject(pair.Key, pair.Value))
                    .ToList();

                BeatmapActionContainer.AddAction(
                    new BeatmapObjectModifiedCollectionAction(
                        editedObjects,
                        editingObjects,
                        $"Edited ({editingObjects.Count}) objects with Node Editor."),
                    true);
            }
            UpdateJson();
        }
        catch (Exception e)
        {
            var message = e.Message;
            switch (e)
            {
                case JSONParseException jsonParse: // Error parsing input JSON; tell them what's wrong!
                    message = jsonParse.ToUIFriendlyString();
                    break;
                case TargetInvocationException invocationException
                    : // Error when converting JSON to an object; tell them what's wrong!
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

    private static BaseObject CreateEditedObject(BaseObject reference, JSONNode json)
    {
        var edited = BeatmapFactory.Clone(reference);
        edited.Apply(Activator.CreateInstance(reference.GetType(), new object[] { json }) as BaseObject);
        return edited;
    }

    private void ApplyInnerGlsEdit(IReadOnlyDictionary<BaseObject, JSONNode> objectJsonMap)
    {
        // The open inner GLS grid can select nodes from only its current parent group.
        var originalGroup = ((BaseGLSEvent)editingObjects[0]).EventBoxGroupData;
        var editedGroup = BeatmapFactory.Clone(originalGroup);
        var eventLookup = new GLSEventLookupIndex(originalGroup);
        var editedSelectedEvents = new List<BaseGLSEvent>();
        foreach (var selectedEvent in editingObjects.Cast<BaseGLSEvent>())
        {
            if (!eventLookup.TryGetCloneEvent(selectedEvent, editedGroup, out _, out var editedEvent))
            {
                continue;
            }

            editedEvent.Apply(CreateEditedObject(selectedEvent, objectJsonMap[selectedEvent]));
            editedSelectedEvents.Add(editedEvent);
        }

        // Parent replacement keeps child identities authoritative across undo and redo.
        BeatmapActionContainer.AddAction(
            new BeatmapGLSEventBoxModifiedAction(
                editedGroup,
                originalGroup,
                "Edited GLS events with Node Editor."),
            true);

        // Parent replacement clears selection, so retain the edited nodes without creating selection history.
        foreach (var editedEvent in editedSelectedEvents)
        {
            SelectionController.Select(editedEvent, true, false, false);
        }

        if (editedSelectedEvents.Count > 0)
        {
            SelectionController.OnSelectionChanged?.Invoke();
        }
    }

    public void Close()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(
            typeof(NodeEditorController),
            new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), ActionMapsDisabled);
        UpdateGroup(false);
    }

    #region JSON Utils

    private int TypeToInt(JSONNode node)
    {
        if (node.IsObject) return 0;
        if (node.IsArray) return 1;
        return 2;
    }

    private int? GetAllType(string key, IEnumerable<JSONNode> nodes)
    {
        var a = -1;
        var c = -1;
        foreach (var n in nodes)
        {
            if (!n.HasKey(key)) return null;

            var i = TypeToInt(n[key]);

            if ((i != a && a >= 0) || (a == 1 && n[key].AsArray.Count != c)) return null;

            if (n[key].IsArray && a == -1) c = n[key].AsArray.Count;

            a = i;
        }

        return a;
    }

    private JSONNode GetSharedJson(IEnumerable<JSONNode> nodes)
    {
        var first = nodes.First();
        var result = new JSONObject();

        foreach (var key in first.Keys)
        {
            var t = GetAllType(key, nodes);
            if (t == null) continue;

            if (t == 0)
                result[key] = GetSharedJson(nodes.Select(it => it[key]));
            else if (t == 2)
                result[key] = nodes.All(it => it[key].Value == first[key].Value) ? first[key] : new JSONDash();
            else if (t == 1)
                result[key] = GetSharedJson(nodes.Select(it => it[key].AsArray));
            else
                result[key] = new JSONDash();
        }

        return result;
    }

    private int? GetAllType(int idx, IEnumerable<JSONArray> nodes)
    {
        var a = -1;
        var c = -1;
        foreach (var n in nodes)
        {
            var i = TypeToInt(n[idx]);

            if ((i != a && a >= 0) || (a == 1 && n.AsArray.Count != c)) return null;

            if (n.IsArray && a == -1) c = n.AsArray.Count;

            a = i;
        }

        return a;
    }

    private JSONNode GetSharedJson(IEnumerable<JSONArray> nodes)
    {
        var first = nodes.First();
        var result = new JSONArray();

        for (var key = 0; key < first.Count; key++)
        {
            var t = GetAllType(key, nodes);
            if (t == null) continue;

            if (t == 0)
                result[key] = GetSharedJson(nodes.Select(it => it[key]));
            else if (t == 2)
                result[key] = nodes.All(it => it[key] == first[key]) ? first[key] : new JSONDash();
            else if (t == 1)
                result[key] = GetSharedJson(nodes.Select(it => it[key].AsArray));
            else
                result[key] = new JSONDash();
        }

        return result;
    }

    private void ApplyJson(JSONObject old, JSONObject updated, Dictionary<BaseObject, JSONNode> objects)
    {
        foreach (var key in old.Keys)
        {
            if (updated.HasKey(key)) continue;

            // User removed this key, blat it
            foreach (var o in objects) o.Value.Remove(key);
        }

        foreach (var key in updated.Keys)
        {
            if (updated[key] == "-") continue;

            if (updated[key].IsObject && old[key].IsObject)
            {
                ApplyJson(
                    old[key].AsObject,
                    updated[key].AsObject,
                    objects.ToDictionary(it => it.Key, it => it.Value[key]));
            }
            else if (updated[key].IsArray && old[key].IsArray)
            {
                ApplyJson(
                    old[key].AsArray,
                    updated[key].AsArray,
                    objects.ToDictionary(it => it.Key, it => it.Value[key].AsArray));
            }
            else
            {
                foreach (var o in objects) o.Value[key] = updated[key];
            }
        }
    }

    private void ApplyJson(JSONArray old, JSONArray updated, Dictionary<BaseObject, JSONArray> objects)
    {
        foreach (var o in objects)
        {
            for (var i = o.Value.Count - 1; i >= updated.Count; i--) o.Value.Remove(i);
        }

        for (var i = 0; i < updated.Count; i++)
        {
            if (updated[i] == "-") continue;

            if (updated[i].IsObject && old[i].IsObject)
            {
                ApplyJson(old[i].AsObject, updated[i].AsObject, objects.ToDictionary(it => it.Key, it => it.Value[i]));
            }
            else if (updated[i].IsArray && old[i].IsArray)
            {
                ApplyJson(
                    old[i].AsArray,
                    updated[i].AsArray,
                    objects.ToDictionary(it => it.Key, it => it.Value[i].AsArray));
            }
            else
            {
                foreach (var o in objects) o.Value[i] = updated[i];
            }
        }
    }

    #endregion
}
