using UnityEngine.InputSystem;

public class BeatmapBPMChangeInputController : BeatmapInputController<BeatmapBPMChangeContainer>,
    CMInput.IBPMChangeObjectsActions
{
    public void OnReplaceBPM(InputAction.CallbackContext context)
    {
        if (context.performed && !PersistentUI.Instance.InputBoxIsEnabled)
        {
            RaycastFirstObject(out var containerToEdit);
            if (containerToEdit != null)
            {
                PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", s => ChangeBpm(containerToEdit, s),
                    "", containerToEdit.BpmData.Bpm.ToString());
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
                var original = BeatmapObject.GenerateCopy(containerToEdit.ObjectData);

                var modifier = context.ReadValue<float>() > 0 ? 1 : -1;

                containerToEdit.BpmData.Bpm += modifier;
                containerToEdit.UpdateGridPosition();

                var bpmChanges =
                    BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(BeatmapObject.ObjectType
                        .BpmChange);
                bpmChanges.RefreshModifiedBeat();

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                    containerToEdit.ObjectData, original));

                // Update cursor position
                var atsc = bpmChanges.AudioTimeSyncController;
                var lastBpmChange = bpmChanges.FindLastBpm(atsc.CurrentBeat);
                if (lastBpmChange == containerToEdit.BpmData)
                {
                    var newTime = lastBpmChange.Time + ((atsc.CurrentBeat - lastBpmChange.Time) *
                        (lastBpmChange.Bpm - modifier) / lastBpmChange.Bpm);
                    atsc.MoveToTimeInBeats(newTime);
                }
            }
        }
    }

    internal static void ChangeBpm(BeatmapBPMChangeContainer containerToEdit, string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            var original = BeatmapObject.GenerateCopy(containerToEdit.ObjectData);
            containerToEdit.BpmData.Bpm = bpm;
            containerToEdit.UpdateGridPosition();
            var bpmChanges =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangesContainer>(
                    BeatmapObject.ObjectType.BpmChange);
            bpmChanges.RefreshModifiedBeat();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                containerToEdit.ObjectData, original));
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                s => ChangeBpm(containerToEdit, s), "", containerToEdit.BpmData.Bpm.ToString());
        }
    }
}
