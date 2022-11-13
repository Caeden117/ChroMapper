using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightColorEventPlacement : PlacementController<BeatmapLightColorEvent, BeatmapLightColorEventContainer, LightColorEventsContainer>, 
    CMInput.ILightV3PlacementActions
{
    internal PlatformDescriptorV3 platformDescriptor;
    [SerializeField] private EventAppearanceSO eventAppearanceSO;
    [SerializeField] private EventsContainer eventsContainer;
    [SerializeField] private LightV3GeneratorAppearance uiGenerator;
    private int objectGroup = -1;
    public override BeatmapAction GenerateAction(BeatmapObject spawned, IEnumerable<BeatmapObject> conflicting)
        => new BeatmapObjectPlacementAction(spawned, conflicting, "Placed a LightColorEvent.");
    public override BeatmapLightColorEvent GenerateOriginalData() => new BeatmapLightColorEvent();

    internal override void Start()
    {
        base.Start();
        UpdateAppearance();
        uiGenerator.OnToggleUIPanelSwitch += ChangeActivate;
    }


    protected void OnDestroy()
    {
        uiGenerator.OnToggleUIPanelSwitch -= ChangeActivate;
    }

    private void ChangeActivate(LightV3GeneratorAppearance.LightV3UIPanel currentState)
    {
        enabled = currentState == LightV3GeneratorAppearance.LightV3UIPanel.LightColorPanel;
        instantiatedContainer.SafeSetActive(enabled);
    }

    public override void OnPhysicsRaycast(Intersections.IntersectionHit hit, Vector3 transformedPoint)
    {
        instantiatedContainer.transform.localPosition = new Vector3(
            instantiatedContainer.transform.localPosition.x,
            0.5f,
            instantiatedContainer.transform.localPosition.z);
        objectGroup = platformDescriptor.LaneIndexToGroupId(Mathf.FloorToInt(instantiatedContainer.transform.localPosition.x));
    }
    public override void TransferQueuedToDraggedObject(ref BeatmapLightColorEvent dragged, BeatmapLightColorEvent queued) => throw new System.NotImplementedException();

    /// <summary>
    /// exactly same as <see cref="EventPlacement.SetGridSize(int)"/>
    /// </summary>
    /// <param name="gridSize"></param>
    public void SetGridSize(int gridSize = 16)
    {
        foreach (Transform eachChild in transform)
        {
            switch (eachChild.name)
            {
                case "Light Event Grid Front Scaling Offset":
                    var newFrontScale = eachChild.transform.localScale;
                    newFrontScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newFrontScale;
                    break;
                case "Light Event Interface Scaling Offset":
                    var newInterfaceScale = eachChild.transform.localScale;
                    newInterfaceScale.x = gridSize / 10f;
                    eachChild.transform.localScale = newInterfaceScale;
                    break;
            }
        }

        GridChild.Size = gridSize;
    }

    public void UpdateAppearance()
    {
        if (instantiatedContainer is null) RefreshVisuals();
        instantiatedContainer.ColorEventData = queuedData;
        eventAppearanceSO.SetLightColorEventAppearance(instantiatedContainer, false, 0, false);
    }

    public void UpdateData(BeatmapLightColorEvent e)
    {
        queuedData = e;
        UpdateAppearance();
    }

    internal override void ApplyToMap()
    {
        if (SelectionController.SelectedObjects.Count == 1 
            && SelectionController.SelectedObjects.First() is BeatmapLightColorEvent obj
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
                (con as BeatmapLightColorEventContainer).SpawnEventDatas(eventAppearanceSO, eventsContainer);
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

    public void OnSwapColorRotation(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        uiGenerator.OnToggleColorRotationSwitch();
    }
    public void OnToggleUI(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        uiGenerator.ToggleDropdown();
    }

    private bool SanityCheck()
    {
        if (!BeatmapLightEventFilter.SanityCheck(queuedData.EventBoxes[0].Filter)) return false;
        return true;
    }
}
