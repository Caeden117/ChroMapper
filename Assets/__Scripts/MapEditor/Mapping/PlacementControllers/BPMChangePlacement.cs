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
    private DialogBox createBpmEventDialogueBox;
    private TextBoxComponent bpmTextInput;
    private ToggleComponent resetBeatToggle;

    internal override void Start()
    {
        base.Start();

        // Create and cache dialog box for later use
        createBpmEventDialogueBox = PersistentUI.Instance
            .CreateNewDialogBox()
            .DontDestroyOnClose();

        // TODO: Localise these & and initialise with last bpm value
        bpmTextInput = createBpmEventDialogueBox
            .AddComponent<TextBoxComponent>()
            .WithLabel("Bpm");

        resetBeatToggle = createBpmEventDialogueBox
            .AddComponent<ToggleComponent>()
            .WithLabel("Reset Beat")
            .WithInitialValue(false);

        createBpmEventDialogueBox.OnQuickSubmit(() => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value));

        createBpmEventDialogueBox.AddFooterButton(null, "PersistentUI", "cancel");
        createBpmEventDialogueBox.AddFooterButton(() => AttemptPlaceBpmChange(bpmTextInput.Value, resetBeatToggle.Value), "PersistentUI", "ok");
    }

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
        createBpmEventDialogueBox
            .WithTitle("Mapper", "bpm.dialog")
            .Open();
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
                var jsonTimeOffset = 1 - oldTime % 1;

                // Place a very fast bpm event slighty behind the original event to account for drift
                var aVeryLargeBpm = 100000f;
                var offsetRequiredInBeats = jsonTimeOffset * prevBpm / (aVeryLargeBpm - prevBpm);
                var offsetEvent = BeatmapFactory.BpmEvent(oldTime - offsetRequiredInBeats, aVeryLargeBpm);
                objectContainerCollection.SpawnObject(offsetEvent, out var fatConflicting);

                // Place the bpm event on the next beat
                var queuedEvent = BeatmapFactory.BpmEvent(Mathf.Ceil(oldTime), bpm);
                objectContainerCollection.SpawnObject(queuedEvent, out var queuedConflicting);

                BeatmapActionContainer.AddAction(new ActionCollectionAction(new List<BeatmapAction>{
                    GenerateAction(offsetEvent, fatConflicting),
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
            // TODO: This path doesn't work
            createBpmEventDialogueBox
                .WithTitle("Mapper", "bpm.dialog.invalid")
                .Open();
        }
    }
}
