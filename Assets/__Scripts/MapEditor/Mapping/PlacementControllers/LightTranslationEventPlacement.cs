using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightTranslationEventPlacement : PlacementController<
    BeatmapLightTranslationEvent, BeatmapLightTranslationEventContainer, LightTranslationEventsContainer>
{
    internal PlatformDescriptorV3 platformDescriptor;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;
    private int objectGroup = -1;
    internal static readonly string[] axisName = { "X", "Y", "Z" };

    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a LightTranslationEvent.");
    public override BeatmapLightTranslationEvent GenerateOriginalData() => new BeatmapLightTranslationEvent();
    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.transform.localPosition = new Vector3(
            instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        objectGroup = platformDescriptor.LaneIndexToGroupId(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
    }

    internal override void Start()
    {
        base.Start();
        UpdateAppearance();
        uiGenerator.OnToggleUIPanelSwitch += ChangeActivate;
        ChangeActivate(LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel);
    }
    protected void OnDestroy()
    {
        uiGenerator.OnToggleUIPanelSwitch -= ChangeActivate;
    }
    private void ChangeActivate(LightV3GeneratorAppearance.LightV3UIPanel currentState)
    {
        enabled = currentState == LightV3GeneratorAppearance.LightV3UIPanel.LightTranslationPanel;
        instantiatedContainer.SafeSetActive(enabled);
    }

    public override void TransferQueuedToDraggedObject(ref BeatmapLightTranslationEvent dragged, BeatmapLightTranslationEvent queued) => throw new System.NotImplementedException();

    public void UpdateAppearance()
    {
        if (instantiatedContainer is null) RefreshVisuals();
        instantiatedContainer.LightEventData = queuedData;
        eventAppearanceSO.SetLightTranslationEventAppearance(instantiatedContainer, 0, false);
    }

    public void UpdateData(BeatmapLightTranslationEvent e)
    {
        queuedData = e;
        UpdateAppearance();
    }

    internal override void ApplyToMap()
    {
        if (SelectionController.SelectedObjects.Count == 1
            && SelectionController.SelectedObjects.First() is BeatmapLightTranslationEvent obj
            && obj.Group == objectGroup
            && obj.Time < RoundedTime)
        {
            // If we are placing subnotes, basically we will not use base.ApplyToMap()
            // because we need to manually insert the note
            var ebd = BeatmapObject.GenerateCopy(queuedData.EventBoxes[0].EventDatas[0]);
            ebd.AddedBeat = RoundedTime - obj.Time;
            var originData = BeatmapObject.GenerateCopy(obj);
            var newData = obj;
            var idx = newData.EventBoxes[0].EventDatas.FindLastIndex(x => x.AddedBeat < ebd.AddedBeat);
            newData.EventBoxes[0].EventDatas.Insert(idx + 1, ebd);
            if (objectContainerCollection.LoadedContainers.TryGetValue(newData, out var con))
            {
                (con as BeatmapLightTranslationEventContainer).SpawnEventDatas(eventAppearanceSO);
            }
            BeatmapActionContainer.AddAction(GenerateAction(newData, new[] { originData }));
        }
        else
        {
            if (!SanityCheck()) return;
            queuedData = BeatmapObject.GenerateCopy(queuedData); // before copy, queued data is referencing binder's data
            queuedData.Group = objectGroup;
            base.ApplyToMap();
        }
    }

    private bool SanityCheck()
    {
        var groupLight = platformDescriptor.LightsManagersV3[platformDescriptor.GroupIdToLaneIndex(objectGroup)];
        if (!groupLight.IsValidTranslationAxis(queuedData.EventBoxes[0].Axis))
        {
            PersistentUI.Instance.ShowDialogBox($"This lane cannot translate on {axisName[queuedData.EventBoxes[0].Axis]} axis", null, PersistentUI.DialogBoxPresetType.Ok);
            return false;
        }
        if (!BeatmapLightEventFilter.SanityCheck(queuedData.EventBoxes[0].Filter)) return false;
        return true;
    }
}
