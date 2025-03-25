using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.Helper;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using UnityEngine;

public class BPMChangePlacement : PlacementController<BaseBpmEvent, BpmEventContainer, BPMChangeGridContainer>
{
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Event at time {spawned.JsonTime}");

    public override BaseBpmEvent GenerateOriginalData() => new(0, 100);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        instantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BaseBpmEvent dragged, BaseBpmEvent queued)
    {
        dragged.SetTimes(queued.JsonTime);
        objectContainerCollection.RefreshModifiedBeat();
    }

    public override void ClickAndDragFinished() => objectContainerCollection.RefreshModifiedBeat();

    internal override void ApplyToMap()
    {
        CreateAndOpenBpmDialogue(isInitialPlacement: true);
    }

    private void AttemptPlaceBpmChange(string obj, bool willResetGrid)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            if (willResetGrid && (Mathf.Abs(queuedData.JsonTime - Mathf.Round(queuedData.JsonTime)) > BeatmapObjectContainerCollection.Epsilon))
            {
                // e.g. Placing a bpm event at beat 3.5 will create a bpm event at beat 3 and 4.
                //      The bpm on beat 3 will be such that the bpm event on beat 4 lines with where the cursor is.
                var prevBpm = (float)BeatSaberSongContainer.Instance.Map.BpmAtSongBpmTime(SongBpmTime);

                var prevBeat = Mathf.Floor(queuedData.JsonTime);
                var nextBeat = Mathf.Ceil(queuedData.JsonTime);
                
                // Place an offset bpm event on the previous beat to scale the grid so it "resets"
                var offsetBpm = prevBpm / (queuedData.JsonTime - prevBeat);
                var offsetEvent = new BaseBpmEvent(prevBeat, offsetBpm);
                objectContainerCollection.SpawnObject(offsetEvent, out var offsetConflicting);

                // Place the bpm event on the next beat
                var queuedEvent = new BaseBpmEvent(nextBeat, bpm);
                objectContainerCollection.SpawnObject(queuedEvent, out var queuedConflicting);

                BeatmapActionContainer.AddAction(new ActionCollectionAction(new List<BeatmapAction>{
                    GenerateAction(offsetEvent, offsetConflicting),
                    GenerateAction(queuedEvent, queuedConflicting)
                }));
            }
            else
            {
                queuedData.Bpm = bpm;
                base.ApplyToMap();
            }
        }
        else
        {
            CreateAndOpenBpmDialogue(isInitialPlacement: false);
        }
    }

    private void CreateAndOpenBpmDialogue(bool isInitialPlacement)
    {
        // TODO: Why aren't we caching this dialogue box? Two bugs:
        //    1) The footer buttons can trigger off the same click that opens this dialogue which causes an instant close
        //    2) Immediately reopening the dialogue box after closing it doesn't work

        var createBpmEventDialogueBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .WithTitle("Mapper", "bpm.dialog");

        if (!isInitialPlacement)
        {
            createBpmEventDialogueBox
                .AddComponent<TextComponent>()
                .WithInitialValue("Mapper", "bpm.dialogue.invalidnumber");
        }

        var lastBpm = (float)BeatSaberSongContainer.Instance.Map.BpmAtSongBpmTime(SongBpmTime);

        var bpmTextInput = createBpmEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Mapper", "bpm.dialogue.beatsperminute")
            .WithInitialValue(lastBpm.ToString());

        var resetBeatToggle = createBpmEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Mapper", "bpm.dialogue.resetbeat")
            .WithInitialValue(false);

        createBpmEventDialogueBox.OnQuickSubmit(() => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value));

        createBpmEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createBpmEventDialogueBox.AddFooterButton(() => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value), "PersistentUI", "ok");

        createBpmEventDialogueBox.Open();
    }
}
