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

    public override BeatmapAction GenerateAction(BeatmapBPMChangeContainer spawned, BeatmapObjectContainer conflicting)
    {
        return new BeatmapObjectPlacementAction(spawned, conflicting, $"Placed a BPM Change at time {spawned.bpmData._time}");
    }

    public override BeatmapBPMChange GenerateOriginalData() => new BeatmapBPMChange(0, 0);

    public override void OnPhysicsRaycast(RaycastHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.transform.localPosition = new Vector3(0.5f, 0.5f, instantiatedContainer.transform.localPosition.z);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapBPMChange dragged, BeatmapBPMChange queued)
    {
        dragged._time = queued._time;
        objectContainerCollection.SortObjects();
    }

    public override void ClickAndDragFinished()
    {
        objectContainerCollection.SortObjects();
    }

    internal override void ApplyToMap()
    {
        if (objectContainerCollection.LoadedContainers.Count >= BPMChangesContainer.ShaderArrayMaxSize)
        {
            if (!PersistentUI.Instance.DialogBox_IsEnabled)
            {
                PersistentUI.Instance.ShowDialogBox(
                    "Due to Unity shader restrictions, the maximum amount of BPM Changes you can have is " +
                    (BPMChangesContainer.ShaderArrayMaxSize - 1) + ".",
                    null,
                    PersistentUI.DialogBoxPresetType.Ok);
            }
            return;
        }
        CMInputCallbackInstaller.DisableActionMaps(actionMapsDisabled);
        PersistentUI.Instance.ShowInputBox("Please enter the BPM for this new BPM change.", AttemptPlaceBPMChange,
            BeatSaberSongContainer.Instance.song.beatsPerMinute.ToString());
    }

    private void AttemptPlaceBPMChange(string obj)
    {
        if (string.IsNullOrEmpty(obj) || string.IsNullOrWhiteSpace(obj))
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
            return;
        }
        if (float.TryParse(obj, out float bpm))
        {
            CMInputCallbackInstaller.ClearDisabledActionMaps(actionMapsDisabled);
            queuedData._time = RoundedTime;
            queuedData._BPM = bpm;
            base.ApplyToMap();
        }
        else
        {
            PersistentUI.Instance.ShowInputBox("Invalid number.\n\nPlease enter the BPM for this new BPM change.",
                AttemptPlaceBPMChange, BeatSaberSongContainer.Instance.song.beatsPerMinute.ToString());
        }
    }
}
