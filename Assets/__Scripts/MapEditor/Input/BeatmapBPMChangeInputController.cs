﻿using Beatmap.Base;
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
                containerToEdit.UpdateGridPosition();

                var bpmChanges =
                    BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(ObjectType
                        .BpmChange);
                bpmChanges.RefreshModifiedBeat();

                BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                    containerToEdit.ObjectData, original, "Tweaked bpm"));

                // Update cursor position
                var atsc = bpmChanges.AudioTimeSyncController;
                if (containerToEdit.BpmData.SongBpmTime < atsc.CurrentBeat)
                {
                    var lastBpmChange = bpmChanges.FindLastBpm(atsc.CurrentBeat);
                    if (lastBpmChange == containerToEdit.BpmData)
                    {
                        var newTime = lastBpmChange.SongBpmTime + ((atsc.CurrentBeat - lastBpmChange.SongBpmTime) *
                            (lastBpmChange.Bpm - modifier) / lastBpmChange.Bpm);
                        atsc.MoveToTimeInBeats(newTime);
                    }
                }

                BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
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
            bpmChanges.RefreshModifiedBeat();
            BeatmapActionContainer.AddAction(new BeatmapObjectModifiedAction(containerToEdit.ObjectData,
                containerToEdit.ObjectData, original, "Modified bpm"));

            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                s => ChangeBpm(containerToEdit, s), "", containerToEdit.BpmData.Bpm.ToString());
        }
    }
}
