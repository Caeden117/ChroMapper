using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMChangePlacement : PlacementController<BeatmapBPMChange, BeatmapBPMChangeContainer, BPMChangesContainer>
{
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned._time}");
    }

    public override BeatmapBPMChange GenerateOriginalData() => new BeatmapBPMChange(0, 0);

    public override void OnPhysicsRaycast(Intersections.IntersectionHit _, Vector3 __)
    {
        instantiatedContainer.transform.localPosition = new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapBPMChange dragged, BeatmapBPMChange queued)
    {
        dragged._time = queued._time;
        objectContainerCollection.RefreshGridShaders();
    }

    public override void ClickAndDragFinished()
    {
        objectContainerCollection.RefreshGridShaders();
    }

    internal override void ApplyToMap()
    {
        if (objectContainerCollection.LoadedObjects.Count >= BPMChangesContainer.ShaderArrayMaxSize)
        {
            if (!PersistentUI.Instance.DialogBox_IsEnabled)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "Mapper", "maxbpm",
                    null,
                    PersistentUI.DialogBoxPresetType.Ok, new object[] { BPMChangesContainer.ShaderArrayMaxSize - 1 });
            }
            return;
        }
        float lastBPM = objectContainerCollection.FindLastBPM(RoundedTime, false)?._BPM ?? BeatSaberSongContainer.Instance.song.beatsPerMinute;
        PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBPMChange,
            "", lastBPM.ToString());
    }

    private void AttemptPlaceBPMChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj))
        {
            return;
        }
        if (float.TryParse(obj, out float bpm))
        {
            queuedData._time = RoundedTime;
            queuedData._BPM = bpm;
            base.ApplyToMap();
            objectContainerCollection.RefreshGridShaders();
        }
        else
        {
            float lastBPM = objectContainerCollection.FindLastBPM(RoundedTime, false)?._BPM ?? BeatSaberSongContainer.Instance.song.beatsPerMinute;
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBPMChange, "", lastBPM.ToString());
        }
    }
}
