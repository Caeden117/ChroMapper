using System.Collections.Generic;
using UnityEngine;

public class BpmChangePlacement : PlacementController<BeatmapBPMChange, BeatmapBPMChangeContainer, BPMChangesContainer>
{
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned.Time}");

    public override BeatmapBPMChange GenerateOriginalData() => new BeatmapBPMChange(0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        instantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BeatmapBPMChange dragged, BeatmapBPMChange queued)
    {
        dragged.Time = queued.Time;
        objectContainerCollection.RefreshModifiedBeat();
    }

    public override void ClickAndDragFinished() => objectContainerCollection.RefreshModifiedBeat();

    internal override void ApplyToMap()
    {
        if (objectContainerCollection.LoadedObjects.Count >= BPMChangesContainer.MaxBpmChangesInShader)
        {
            if (!PersistentUI.Instance.DialogBoxIsEnabled)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "Mapper", "maxbpm",
                    null,
                    PersistentUI.DialogBoxPresetType.Ok, new object[] {BPMChangesContainer.MaxBpmChangesInShader - 1});
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
