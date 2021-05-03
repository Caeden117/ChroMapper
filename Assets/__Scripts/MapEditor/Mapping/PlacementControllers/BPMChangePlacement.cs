using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMChangePlacement : PlacementController<BeatmapBPMChange, BeatmapBPMChangeContainer, BPMChangesContainer>
{
    private readonly Type[] actionMapsDisabled = new Type[]
    {
        typeof(CMInput.IPlacementControllersActions),
        typeof(CMInput.INotePlacementActions),
        typeof(CMInput.IEventPlacementActions),
        typeof(CMInput.ISavingActions),
        typeof(CMInput.IPlatformSoloLightGroupActions),
        typeof(CMInput.IPlaybackActions),
        typeof(CMInput.IPlatformDisableableObjectsActions),
        typeof(CMInput.INoteObjectsActions),
        typeof(CMInput.IEventObjectsActions),
        typeof(CMInput.IObstacleObjectsActions),
        typeof(CMInput.ICustomEventsContainerActions),
        typeof(CMInput.IBPMTapperActions),
        typeof(CMInput.IModifyingSelectionActions),
        typeof(CMInput.IWorkflowsActions),
    };

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned._time}");
    }

    public override BeatmapBPMChange GenerateOriginalData() => new BeatmapBPMChange(0, 0);

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
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
        PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog", AttemptPlaceBPMChange,
            "", BeatSaberSongContainer.Instance.song.beatsPerMinute.ToString());
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
            PersistentUI.Instance.ShowInputBox("Mapper", "bpm.dialog.invalid",
                AttemptPlaceBPMChange, "", BeatSaberSongContainer.Instance.song.beatsPerMinute.ToString());
        }
    }
}
