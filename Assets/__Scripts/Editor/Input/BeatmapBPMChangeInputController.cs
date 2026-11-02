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
                PersistentUI.Instance.ShowInputBox(
                    "Mapper",
                    "bpm.dialog",
                    s => ChangeBpm(containerToEdit, s),
                    "",
                    containerToEdit.BpmData.Bpm.ToString());
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
                var modifier = context.GetScrollDirection();

                void Tweak(BaseBpmEvent data)
                {
                    data.Bpm = CMMath.RoundToDecimals(data.Bpm + modifier);
                    if (data.Bpm <= 0)
                        data.Bpm = 1f;
                }

                if (HoverSelectionMutation.TryApply(containerToEdit.BpmData, Tweak, out var edited))
                {
                    if (edited.Count > 0)
                    {
                        var firstEditedBeat = edited[0].JsonTime;
                        foreach (var evt in edited)
                            firstEditedBeat = System.Math.Min(firstEditedBeat, evt.JsonTime);

                        var collection = BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                            ObjectType.BpmChange);
                        RefreshCursorAfterTweak(collection, firstEditedBeat);
                    }

                    return;
                }

                var original = BeatmapFactory.Clone(containerToEdit.ObjectData);
                Tweak(containerToEdit.BpmData);
                containerToEdit.UpdateGridPosition();

                if (containerToEdit.BpmData.HasSameContent(original))
                    return;

                var bpmChanges =
                    BeatmapObjectContainerCollection.GetCollectionForType<BPMChangeGridContainer>(
                        ObjectType
                            .BpmChange);

                BeatmapActionContainer.AddAction(
                    new BeatmapObjectModifiedAction(
                        containerToEdit.ObjectData,
                        containerToEdit.ObjectData,
                        original,
                        "Tweaked bpm",
                        mergeType: ActionMergeType.BPMValueTweak,
                        preserveSelection: true));

                BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
                bpmChanges.RefreshModifiedBeat();

                RefreshCursorAfterTweak(bpmChanges, containerToEdit.BpmData.JsonTime);
            }
        }
    }

    private static void RefreshCursorAfterTweak(BPMChangeGridContainer collection, float firstEditedBeat)
    {
        var atsc = collection.BeatmapContext.Atsc;
        if (firstEditedBeat < atsc.CurrentJsonTime)
            atsc.MoveToJsonTime(atsc.CurrentJsonTime);
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
            BeatmapActionContainer.AddAction(
                new BeatmapObjectModifiedAction(
                    containerToEdit.ObjectData,
                    containerToEdit.ObjectData,
                    original,
                    "Modified bpm",
                    preserveSelection: true));

            BeatmapObjectContainerCollection.RefreshFutureObjectsPosition(containerToEdit.BpmData.JsonTime);
            bpmChanges.RefreshModifiedBeat();
        }
        else
        {
            PersistentUI.Instance.ShowInputBox(
                "Mapper",
                "bpm.dialog.invalid",
                s => ChangeBpm(containerToEdit, s),
                "",
                containerToEdit.BpmData.Bpm.ToString());
        }
    }
}
