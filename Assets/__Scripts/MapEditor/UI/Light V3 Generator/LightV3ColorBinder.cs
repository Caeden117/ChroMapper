using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class LightV3ColorBinder : MetaLightV3Binder<BeatmapLightColorEvent>, CMInput.IEventUIActions, CMInput.IWorkflowsActions
{
    public int DataIdx = 0;
    protected override void InitBindings()
    {
        ObjectData = new BeatmapLightColorEvent();

        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].BrightnessDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Color.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Brightness.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].BrightnessDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].TransitionType);

        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Section" : "Start");
        TextsDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType == 1 ? "Partition" : "Step");
        TextsDumpFn.Add(x => $"{DataIdx + 1}/{x.EventBoxes[0].EventDatas.Count}");

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].BrightnessDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Color = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].Brightness = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].FlickerFrequency = int.Parse(s));

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].BrightnessDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].TransitionType = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
    }

    protected override void Dump(BeatmapLightColorEvent obj)
    {
        var col = BeatmapObjectContainerCollection.GetCollectionForType<LightColorEventsContainer>(obj.BeatmapType);
        if (col.LoadedContainers.TryGetValue(obj, out var con))
        {
            var colorCon = con as BeatmapLightColorEventContainer;
            DataIdx = colorCon.GetRaycastedIdx();
        }
        base.Dump(obj);
    }

    public void OnTypeOn(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DropdownLoadFn[3](ObjectData, 0);
        InputLoadFn[6](ObjectData, "1");
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }
    public void OnTypeFlash(InputAction.CallbackContext context) { }
    public void OnTypeOff(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DropdownLoadFn[3](ObjectData, 0);
        InputLoadFn[6](ObjectData, "0");
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }
    public void OnTypeFade(InputAction.CallbackContext context) { }
    public void OnTogglePrecisionRotation(InputAction.CallbackContext context) { }
    public void OnSwapCursorInterval(InputAction.CallbackContext context) { }
    public void OnTypeTransition(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        DropdownLoadFn[3](ObjectData, 1);
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }


    public void OnToggleRightButtonPanel(InputAction.CallbackContext context) { }
    public void OnUpdateSwingArcVisualizer(InputAction.CallbackContext context) { }
    public void OnToggleNoteorEvent(InputAction.CallbackContext context) { }
    public void OnPlaceRedNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        InputLoadFn[5](ObjectData, "0");
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }
    public void OnPlaceBlueNoteorEvent(InputAction.CallbackContext context) 
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        InputLoadFn[5](ObjectData, "1");
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }
    public void OnPlaceBomb(InputAction.CallbackContext context)
    {
        if (!context.performed || !Settings.Instance.Load_MapV3) return;
        InputLoadFn[5](ObjectData, "2");
        if (!DisplayingSelectedObject) Dump(ObjectData);
    }
    public void OnPlaceObstacle(InputAction.CallbackContext context) { }
    public void OnToggleDeleteTool(InputAction.CallbackContext context) { }
    public void OnMirror(InputAction.CallbackContext context) { }
    public void OnMirrorinTime(InputAction.CallbackContext context) { }
    public void OnMirrorColoursOnly(InputAction.CallbackContext context) { }
}
