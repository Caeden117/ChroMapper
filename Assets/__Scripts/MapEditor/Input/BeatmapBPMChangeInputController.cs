using UnityEngine.InputSystem;

public class BeatmapBPMChangeInputController : BeatmapInputController<BeatmapBPMChangeContainer>, CMInput.IBPMChangeObjectsActions
{
    public void OnReplaceBPM(InputAction.CallbackContext context)
    {
        if (context.performed && !PersistentUI.Instance.InputBox_IsEnabled)
        {
            RaycastFirstObject(out var containerToEdit);
            if (containerToEdit != null)
            {
                PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", s => ChangeBPM(containerToEdit, s),
                    "", containerToEdit.bpmData._BPM.ToString());
            }
        }
    }

    public void OnTweakBPMValue(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            RaycastFirstObject(out var containerToEdit);
            if (containerToEdit != null)
            {
                var original = BeatmapObject.GenerateCopy(containerToEdit.objectData);

                var modifier = context.ReadValue<float>() > 0 ? 1 : -1;

                containerToEdit.bpmData._BPM += modifier;
                containerToEdit.UpdateGridPosition();
                
                var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
                bpmChanges.RefreshGridShaders();

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.objectData, containerToEdit.objectData, original));
            }
        }
    }

    internal static void ChangeBPM(BeatmapBPMChangeContainer containerToEdit, string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj))
        {
            return;
        }
        if (float.TryParse(obj, out var bpm))
        {
            var original = BeatmapObject.GenerateCopy(containerToEdit.objectData);
            containerToEdit.bpmData._BPM = bpm;
            containerToEdit.UpdateGridPosition();
            var bpmChanges = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.Type.BPM_CHANGE);
            bpmChanges.RefreshGridShaders();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.objectData, containerToEdit.objectData, original));
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                s => ChangeBPM(containerToEdit, s), "", containerToEdit.bpmData._BPM.ToString());
        }
    }
}
