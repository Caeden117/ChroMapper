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

    public override BaseBpmEvent GenerateOriginalData() => BeatmapFactory.BpmEvent(0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        instantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BaseBpmEvent dragged, BaseBpmEvent queued)
    {
        dragged.SetTimes(queued.JsonTime, queued.SongBpmTime);
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
                var prevBpm = objectContainerCollection.FindLastBpm(SongBpmTime, false)?.Bpm ??
                          BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
                var oldTime = queuedData.JsonTime;
                var jsonTimeOffset = 1 - (oldTime % 1);

                // Place a very fast bpm event slighty behind the original event to account for drift
                var aVeryLargeBpm = 100000f;
                var offsetRequiredInBeats = jsonTimeOffset * prevBpm / (aVeryLargeBpm - prevBpm);
                var offsetEvent = BeatmapFactory.BpmEvent(oldTime - offsetRequiredInBeats, aVeryLargeBpm);
                objectContainerCollection.SpawnObject(offsetEvent, out var offsetConflicting);

                // Place the bpm event on the next beat
                var queuedEvent = BeatmapFactory.BpmEvent(Mathf.Ceil(oldTime), bpm);
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

        var lastBpm = objectContainerCollection.FindLastBpm(SongBpmTime, false)?.Bpm ??
                      BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
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
