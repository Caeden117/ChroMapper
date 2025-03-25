using Beatmap.Base;
using Beatmap.Containers;
using Beatmap.Enums;
using Beatmap.Helper;
using UnityEngine.InputSystem;

public class BeatmapBPMChangeInputController : BeatmapInputController<BpmEventContainer>,
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
                var original = BeatmapFactory.Clone(containerToEdit.ObjectData);

                var modifier = context.ReadValue<float>() > 0 ? 1 : -1;

                containerToEdit.BpmData.Bpm += modifier;
                if (containerToEdit.BpmData.Bpm <= 0) containerToEdit.BpmData.Bpm = 1f;
                containerToEdit.UpdateGridPosition();

                var bpmChanges =
                    BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType
                        .BpmChange);

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                    containerToEdit.ObjectData, original, "Tweaked bpm"));

                // Update cursor position
                var atsc = bpmChanges.AudioTimeSyncController;
                if (containerToEdit.BpmData.JsonTime < atsc.CurrentJsonTime)
                {
                    atsc.MoveToJsonTime(atsc.CurrentJsonTime);
                }

                BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
                bpmChanges.RefreshModifiedBeat();
            }
        }
    }
    internal static void ChangeBpm(BpmEventContainer containerToEdit, string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            var original = BeatmapFactory.Clone(containerToEdit.ObjectData);
            containerToEdit.BpmData.Bpm = bpm;
            containerToEdit.UpdateGridPosition();
            var bpmChanges =
                BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                    ObjectType.BpmChange);
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                containerToEdit.ObjectData, original, "Modified bpm"));

            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
            bpmChanges.RefreshModifiedBeat();
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                s => ChangeBpm(containerToEdit, s), "", containerToEdit.BpmData.Bpm.ToString());
        }
    }
}
