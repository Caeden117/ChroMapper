using System.Collections.Generic;
using UnityEngine;

public class BPMChangePlacement : PlacementController<BeatmapBPMChange, BeatmapBPMChangeContainer, BPMChangesContainer>
{
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting) =>
        new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned.Time}");

    public override BeatmapBPMChange GenerateOriginalData() => new BeatmapBPMChange(0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __) =>
        InstantiatedContainer.transform.localPosition =
            new Vector3(0.5f, 0.5f, InstantiatedContainer.transform.localPosition.z);

    public override void TransferQueuedToDraggedObject(ref BeatmapBPMChange dragged, BeatmapBPMChange queued)
    {
        dragged.Time = queued.Time;
        ObjectContainerCollection.RefreshModifiedBeat();
    }

    public override void ClickAndDragFinished() => ObjectContainerCollection.RefreshModifiedBeat();

    internal override void ApplyToMap()
    {
        if (ObjectContainerCollection.LoadedObjects.Count >= BPMChangesContainer.MaxBpmChangesInShader)
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

        var lastBpm = ObjectContainerCollection.FindLastBpm(RoundedTime, false)?.Bpm ??
                      BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
        PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBpmChange,
            "", lastBpm.ToString());
    }

    private void AttemptPlaceBpmChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj)) return;
        if (float.TryParse(obj, out var bpm))
        {
            QueuedData.Time = RoundedTime;
            QueuedData.Bpm = bpm;
            base.ApplyToMap();
            ObjectContainerCollection.RefreshModifiedBeat();
        }
        else
        {
            var lastBpm = ObjectContainerCollection.FindLastBpm(RoundedTime, false)?.Bpm ??
                          BeatSaberSongContainer.Instance.Song.BeatsPerMinute;
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBpmChange, "", lastBpm.ToString());
        }
    }
}
