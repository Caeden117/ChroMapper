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
using System.Text;

public class NodeEditorController : MonoBehaviour, CMInput.INodeEditorActions
{
    
    [SerializeField] private TMP_InputField nodeEditorInputField;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private Button closeButton;

    public static bool IsActive;
    private bool firstActive = true;

    private string oldInputText;
    private int oldCaretPosition;

    private IEnumerable<BeatmapObject> editingObjects;
    private JSONNode editingNode;
    private bool isEditing;

    private int height = 205;
    private bool queuedUpdate;

    private readonly Type[] actionMapsEnabledWhenNodeEditing = new Type[]
    {
        typeof(CMInput.ICameraActions),
        typeof(CMInput.IBeatmapObjectsActions),
        typeof(CMInput.INodeEditorActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.ITimelineActions)
    };

    // I can just apply this to the places that need them but im feeling lazy lmao
    private Type[] actionMapsDisabled => typeof(CMInput).GetNestedTypes()
        .Where(x => x.IsInterface && !actionMapsEnabledWhenNodeEditing.Contains(x)).ToArray();

    // Use this for initialization
    private void Start () {
        SelectionController.SelectionChangedEvent += ObjectWasSelected;
    }

    private void OnDestroy()
    {
        SelectionController.SelectionChangedEvent -= ObjectWasSelected;
    }

    private void Update()
    {
        if (!Settings.Instance.NodeEditor_Enabled || UIMode.SelectedMode != UIModeType.NORMAL) return;
        if (SelectionController.SelectedObjects.Count == 0 && IsActive)
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

    private IEnumerator UpdateGroup(bool enabled, RectTransform group)
    {
        IsActive = enabled;
        if (enabled)
        {
            if (queuedUpdate)
                ObjectWasSelected();

            height = Mathf.FloorToInt(Settings.Instance.NodeEditorSize * 20.5f);
            GetComponent<RectTransform>().sizeDelta = new Vector2(300, height);
            nodeEditorInputField.pointSize = Settings.Instance.NodeEditorTextSize;
        }

        float dest = enabled ? -5 : -height;
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

    public void ObjectWasSelected()
    {
        queuedUpdate = !IsActive;
        if (queuedUpdate)
            return;

        if (!SelectionController.HasSelectedObjects())
        {
            isEditing = false;
            return;
        }
        BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();

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

        UpdateJSON();
    }

    private void UpdateJSON()
    {
        editingObjects = SelectionController.SelectedObjects.Select(it => it);
        editingNode = GetSharedJSON(editingObjects.Select(it => JSON.Parse(it.ConvertToJSON().ToString())));

        nodeEditorInputField.text = string.Join("", editingNode.ToString(2).Split('\r'));

        if (editingObjects.Count() == 1)
        {
            var obj = editingObjects.First();

            string[] splitName = obj.beatmapType.ToString().Split('_');
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
        else
        {
            labelTextMesh.text = $"Editing ({editingObjects.Count()}) objects";
        }
    }

    public void NodeEditor_StartEdit(string content)
    {
        if (IsActive)
        {
            if (!CMInputCallbackInstaller.IsActionMapDisabled(actionMapsDisabled[0]))
            {
                CMInputCallbackInstaller.DisableActionMaps(typeof(NodeEditorController), new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.DisableActionMaps(typeof(NodeEditorController), actionMapsDisabled);
            }
        }
        oldInputText = content;
        oldCaretPosition = nodeEditorInputField.caretPosition;
    }

    public void NodeEditor_EndEdit(string nodeText)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), actionMapsDisabled);

        try
        {
            if (!isEditing || !IsActive) return;
            JSONNode newNode = JSON.Parse(nodeText); //Parse JSON, and do some basic checks.
            if (string.IsNullOrEmpty(newNode.ToString())) //Damn you Jackz
                throw new Exception("Node cannot be empty.");

            // Super sneaky clone, maybe not needed
            var dict = editingObjects.ToDictionary(it => it, it => it.ConvertToJSON().Clone());

            ApplyJSON(editingNode.AsObject, newNode.AsObject, dict);

            var beatmapActions = dict.Select(entry =>
                new BeatmapObjectModifiedAction(
                    Activator.CreateInstance(entry.Key.GetType(), new object[] { entry.Value }) as BeatmapObject,
                    entry.Key, entry.Key, $"Edited a {entry.Key.beatmapType} with Node Editor.", true)
            ).ToList();

            BeatmapActionContainer.AddAction(new ActionCollectionAction(beatmapActions, true, true, $"Edited ({editingObjects.Count()}) objects with Node Editor."), true);
            UpdateJSON();
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
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), actionMapsDisabled);
        StartCoroutine(UpdateGroup(false, transform as RectTransform));
    }

    public void OnToggleNodeEditor(InputAction.CallbackContext context)
    {
        if (nodeEditorInputField.isFocused) return;
        if (Settings.Instance.NodeEditor_UseKeybind && context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            StopAllCoroutines();
            if (IsActive)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.ClearDisabledActionMaps(typeof(NodeEditorController), actionMapsDisabled);
                BeatmapActionContainer.RemoveAllActionsOfType<NodeEditorTextChangedAction>();
            }
            else
            {
                closeButton.gameObject.SetActive(true);
                CMInputCallbackInstaller.DisableActionMaps(typeof(NodeEditorController), actionMapsDisabled);
            }
            StartCoroutine(UpdateGroup(!IsActive, transform as RectTransform));
        }
    }

    #region JSON Utils

    private int TypeToInt(JSONNode node)
    {
        if (node.IsObject)
        {
            return 0;
        }
        else if (node.IsArray)
        {
            return 1;
        }
        return 2;
    }

    private int? GetAllType(string key, IEnumerable<JSONNode> nodes)
    {
        int a = -1;
        int c = -1;
        foreach (var n in nodes)
        {
            if (!n.HasKey(key))
                return null;

            int i = TypeToInt(n[key]);

            if ((i != a && a >= 0) || (a == 1 && n[key].AsArray.Count != c))
                return null;

            if (n[key].IsArray && a == -1)
                c = n[key].AsArray.Count;

            a = i;
        }

        return a;
    }

    private JSONNode GetSharedJSON(IEnumerable<JSONNode> nodes)
    {
        var first = nodes.First();
        var result = new JSONObject();

        foreach (var key in first.Keys)
        {
            var t = GetAllType(key, nodes);
            if (t == null)
                continue;

            if (t == 0)
            {
                result[key] = GetSharedJSON(nodes.Select(it => it[key]));
            }
            else if (t == 2)
            {
                result[key] = nodes.All(it => it[key].Value == first[key].Value) ? first[key] : new JSONDash();
            }
            else if (t == 1)
            {
                result[key] = GetSharedJSON(nodes.Select(it => it[key].AsArray));
            }
            else
            {
                result[key] = new JSONDash();
            }
        }

        return result;
    }

    private int? GetAllType(int idx, IEnumerable<JSONArray> nodes)
    {
        int a = -1;
        int c = -1;
        foreach (var n in nodes)
        {
            int i = TypeToInt(n[idx]);

            if ((i != a && a >= 0) || (a == 1 && n.AsArray.Count != c))
                return null;

            if (n.IsArray && a == -1)
                c = n.AsArray.Count;

            a = i;
        }

        return a;
    }

    private JSONNode GetSharedJSON(IEnumerable<JSONArray> nodes)
    {
        var first = nodes.First();
        var result = new JSONArray();

        for (int key = 0; key < first.Count; key++)
        {
            var t = GetAllType(key, nodes);
            if (t == null)
                continue;

            if (t == 0)
            {
                result[key] = GetSharedJSON(nodes.Select(it => it[key]));
            }
            else if (t == 2)
            {
                result[key] = nodes.All(it => it[key] == first[key]) ? first[key] : new JSONDash();
            }
            else if (t == 1)
            {
                result[key] = GetSharedJSON(nodes.Select(it => it[key].AsArray));
            }
            else
            {
                result[key] = new JSONDash();
            }
        }

        return result;
    }

    private void ApplyJSON(JSONObject old, JSONObject updated, Dictionary<BeatmapObject, JSONNode> objects)
    {
        foreach (var key in old.Keys)
        {
            if (updated.HasKey(key))
                continue;

            // User removed this key, blat it
            foreach (var o in objects)
            {
                o.Value.Remove(key);
            }
        }

        foreach (var key in updated.Keys)
        {
            if (updated[key] == "-")
                continue;

            if (updated[key].IsObject && old[key].IsObject)
            {
                ApplyJSON(old[key].AsObject, updated[key].AsObject, objects.ToDictionary(it => it.Key, it => it.Value[key]));
            }
            else if (updated[key].IsArray && old[key].IsArray)
            {
                ApplyJSON(old[key].AsArray, updated[key].AsArray, objects.ToDictionary(it => it.Key, it => it.Value[key].AsArray));
            }
            else
            {
                foreach (var o in objects)
                {
                    o.Value[key] = updated[key];
                }
                continue;
            }
        }
    }

    private void ApplyJSON(JSONArray old, JSONArray updated, Dictionary<BeatmapObject, JSONArray> objects)
    {
        foreach (var o in objects)
        {
            for (int i = o.Value.Count - 1; i >= updated.Count; i--)
            {
                o.Value.Remove(i);
            }
        }

        for (int i = 0; i < updated.Count; i++)
        {
            if (updated[i] == "-")
                continue;

            if (updated[i].IsObject && old[i].IsObject)
            {
                ApplyJSON(old[i].AsObject, updated[i].AsObject, objects.ToDictionary(it => it.Key, it => it.Value[i]));
            }
            else if (updated[i].IsArray && old[i].IsArray)
            {
                ApplyJSON(old[i].AsArray, updated[i].AsArray, objects.ToDictionary(it => it.Key, it => it.Value[i].AsArray));
            }
            else
            {
                foreach (var o in objects)
                {
                    o.Value[i] = updated[i];
                }
            }


        }
    }

    #endregion
}
