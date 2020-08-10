using UnityEngine.InputSystem;
using System;

public class BeatmapBPMChangeInputController : BeatmapInputController<BeatmapBPMChangeContainer>, CMInput.IBPMChangeObjectsActions
{
    private BeatmapBPMChangeContainer containerToEdit = null;

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
    };
    
    public void OnReplaceBPM(InputAction.CallbackContext context)
    {
        if (context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            RaycastFirstObject(out containerToEdit);
            if (containerToEdit != null)
            {
                CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
                PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBPMChange,
                    "", containerToEdit.bpmData._BPM.ToString());
            }
        }
    }

    private void AttemptPlaceBPMChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj))
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
            containerToEdit = null;
            return;
        }
        if (float.TryParse(obj, out float bpm))
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(containerToEdit.objectData);
            CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
            containerToEdit.bpmData._BPM = bpm;
            containerToEdit.UpdateGridPosition();
            containerToEdit = null;
            var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
            bpmChanges.RefreshGridShaders();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(containerToEdit.objectData), original));
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBPMChange, "", containerToEdit.bpmData._BPM.ToString());
        }
    }
}
