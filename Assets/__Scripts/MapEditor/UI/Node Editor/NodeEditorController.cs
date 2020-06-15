using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using SimpleJSON;
using UnityEngine.InputSystem;

public class NodeEditorController : MonoBehaviour, CMInput.INodeEditorActions
{
    
    [SerializeField] private TMP_InputField nodeEditorInputField;
    [SerializeField] private TextMeshProUGUI labelTextMesh;
    [SerializeField] private NoteAppearanceSO noteAppearance;
    [SerializeField] private EventAppearanceSO eventAppearance;
    [SerializeField] private ObstacleAppearanceSO obstacleAppearance;
    [SerializeField] private TracksManager tracksManager;

    public static bool IsActive;
    public bool AdvancedSetting => Settings.Instance.NodeEditor_Enabled;
    private bool firstActive = true;

    private BeatmapObjectContainer editingContainer;
    private JSONNode editingNode;
    private bool isEditing;

    private readonly Type[] actionMapsDisabled = new Type[]
    {
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IPlatformDisableableObjectsActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IModifyingSelectionActions),
        typeof(CMInput.IWorkflowsActions),
        typeof(CMInput.IBookmarksActions),
        typeof(CMInput.ITimelineActions),
    };

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
                StartCoroutine(UpdateGroup(false, transform as RectTransform));
            }
            labelTextMesh.text = "Nothing Selected";
            nodeEditorInputField.text = "Please select an object to use Node Editor.";
        }else if (SelectionController.SelectedObjects.Count == 1 && !isEditing && AdvancedSetting)
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
        if (SelectionController.SelectedObjects.Count > 1) {
            if (!Settings.Instance.NodeEditor_UseKeybind && !AdvancedSetting)
            {
                StopAllCoroutines();
                StartCoroutine(UpdateGroup(false, transform as RectTransform));
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
            StartCoroutine(UpdateGroup(true, transform as RectTransform));
            if (firstActive)
            {
                firstActive = false;
                PersistentUI.Instance.DisplayMessage("Node Editor is very powerful - Be careful!", PersistentUI.DisplayMessageType.BOTTOM);
            }
        }

        var collection = BeatmapObjectContainerCollection.GetCollectionForType(container.beatmapType);
        editingContainer = collection.LoadedContainers[container];
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

    private void SelectionPasted(IEnumerable<BeatmapObject> obj)
    {
        editingContainer = null;
        ObjectWasSelected(obj.FirstOrDefault());
    }

    public void NodeEditor_StartEdit(string _)
    {
        if (IsActive)
        {
            CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
            CMInputCallbackInstaller.DisableActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
        }
    }

    public void NodeEditor_EndEdit(string nodeText)
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
        try
        {
            if (!isEditing || !IsActive || SelectionController.SelectedObjects.Count != 1) return;
            JSONNode newNode = JSON.Parse(nodeText); //Parse JSON, and do some basic checks.
            if (string.IsNullOrEmpty(newNode.ToString())) //Damn you Jackz
                throw new Exception("Invalid JSON!\n\nCheck to make sure the node is not empty.");
            if (string.IsNullOrEmpty(newNode["_time"]))
                throw new Exception("Invalid JSON!\n\nEvery object needs a \"_time\" value!");

            //From this point on, its the mappers fault for whatever shit happens from JSON.
            var collection = BeatmapObjectContainerCollection.GetCollectionForType(editingContainer.objectData.beatmapType);

            BeatmapObject original = editingContainer.objectData;
            collection.DeleteObject(original, false);
            SelectionController.Deselect(original);

            BeatmapObject newObject = Activator.CreateInstance(original.GetType(), new object[] { newNode }) as BeatmapObject;
            collection.SpawnObject(newObject, true);
            SelectionController.Select(newObject, false, true, false);

            BeatmapActionContainer.AddAction(new NodeEditorUpdatedNodeAction(newObject, original));
            //UpdateAppearance(editingContainer);
            isEditing = false;
        }
        catch (Exception e)
        {
            if (e.GetType() != typeof(Exception)) Debug.LogError(e); 
            PersistentUI.Instance.ShowDialogBox(e.Message, null, PersistentUI.DialogBoxPresetType.Ok);
        }
    }

    public void Close()
    {
        CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
        CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
        StartCoroutine(UpdateGroup(false, transform as RectTransform));
    }

    public void OnToggleNodeEditor(InputAction.CallbackContext context)
    {
        if (Settings.Instance.NodeEditor_UseKeybind && AdvancedSetting && context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            StopAllCoroutines();
            if (IsActive)
            {
                CMInputCallbackInstaller.ClearDisabledActionMaps(new[] { typeof(CMInput.INodeEditorActions) });
                CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
            }
            else
            {
                CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
            }
            StartCoroutine(UpdateGroup(!IsActive, transform as RectTransform));
        }
    }
}
