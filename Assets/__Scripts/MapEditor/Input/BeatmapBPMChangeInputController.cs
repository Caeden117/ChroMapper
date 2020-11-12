using UnityEngine.InputSystem;
using System;

public class BeatmapBPMChangeInputController : BeatmapInputController<BeatmapBPMChangeContainer>, CMInput.IBPMChangeObjectsActions
{
    private BeatmapBPMChangeContainer containerToEdit = null;
    
    public void OnReplaceBPM(InputAction.CallbackContext context)
    {
        if (context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            RaycastFirstObject(out containerToEdit);
            if (containerToEdit != null)
            {
                PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBPMChange,
                    "", containerToEdit.bpmData._BPM.ToString());
            }
        }
    }

    private void AttemptPlaceBPMChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj))
        {
            containerToEdit = null;
            return;
        }
        if (float.TryParse(obj, out float bpm))
        {
            BeatmapObject original = BeatmapObject.GenerateCopy(containerToEdit.objectData);
            containerToEdit.bpmData._BPM = bpm;
            containerToEdit.UpdateGridPosition();
            var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
            bpmChanges.RefreshGridShaders();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(BeatmapObject.GenerateCopy(containerToEdit.objectData), original));
            containerToEdit = null;
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBPMChange, "", containerToEdit.bpmData._BPM.ToString());
        }
    }
}
