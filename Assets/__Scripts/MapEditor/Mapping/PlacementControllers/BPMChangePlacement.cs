using System.Collections.Generic;
using Beatmap.Base;
using Beatmap.Base.Customs;
using Beatmap.Containers;
using Beatmap.V2.Customs;
using Beatmap.V3.Customs;
using UnityEngine;

public class BPMChangePlacement : PlacementController<BaseBpmEvent, BpmEventContainer, BPMChangeGridContainer>
{
    public override BeatmapAction GenerateAction(BaseObject spawned, IEnumerable<BaseObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned.Time}");

    public override BaseBpmEvent GenerateOriginalData() => Settings.Instance.Load_MapV3 ? (BaseBpmEvent)new V3BpmChange(0, 0) : new V2BpmChange(0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        instantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BaseBpmEvent dragged, BaseBpmEvent queued)
    {
        dragged.Time = queued.Time;
        objectContainerCollection.RefreshModifiedBeat();
    }

    public override void ClickAndDragFinished() => objectContainerCollection.RefreshModifiedBeat();

    internal override void ApplyToMap()
    {
        if (objectContainerCollection.LoadedObjects.Count >= BPMChangeGridContainer.MaxBpmChangesInShader)
        {
            if (!PersistentUI.Instance.DialogBoxIsEnabled)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "Mapper", "maxbpm",
                    null,
                    PersistentUI.DialogBoxPresetType.Ok, new object[] { BPMChangeGridContainer.MaxBpmChangesInShader - 1 });
            }

            return;
        }

        var lastBpm = objectContainerCollection.FindLastBpm(RoundedTime, false)?.Bpm ??
                      BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBpmChange,
            "", lastBpm.ToString());
    }

    private void AttemptPlaceBpmChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            queuedData.Time = RoundedTime;
            queuedData.Bpm = bpm;
            base.ApplyToMap();
            objectContainerCollection.RefreshModifiedBeat();
        }
        else
        {
            var lastBpm = objectContainerCollection.FindLastBpm(RoundedTime, false)?.Bpm ??
                          BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBpmChange, "", lastBpm.ToString());
        }
    }
}
