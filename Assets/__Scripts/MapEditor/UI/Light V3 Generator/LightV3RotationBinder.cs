using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightV3RotationBinder : MetaLightV3Binder<BeatmapLightRotationEvent>
{
    public int DataIdx = 0;
    protected override void InitBindings()
    {
        ObjectData = new BeatmapLightRotationEvent();

        InputDumpFn.Add(x => (x.EventBoxes[0].Filter.FilterType == 1 ? x.EventBoxes[0].Filter.Section + 1 : x.EventBoxes[0].Filter.Section).ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Filter.Partition.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].Distribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].RotationDistribution.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].AdditionalLoop.ToString());
        InputDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].RotationValue.ToString());

        DropdownDumpFn.Add(x => x.EventBoxes[0].Filter.FilterType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].DistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].RotationDistributionType - 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].Axis);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].Transition);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].EaseType + 1);
        DropdownDumpFn.Add(x => x.EventBoxes[0].EventDatas[DataIdx].RotationDirection);

        ToggleDumpFn.Add(x => x.EventBoxes[0].Filter.Reverse == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].RotationAffectFirst == 1);
        ToggleDumpFn.Add(x => x.EventBoxes[0].ReverseRotation == 1);

        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Section = x.EventBoxes[0].Filter.FilterType == 1 ? int.Parse(s) - 1 : int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Filter.Partition = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].Distribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].RotationDistribution = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AddedBeat = float.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].AdditionalLoop = int.Parse(s));
        InputLoadFn.Add((x, s) => x.EventBoxes[0].EventDatas[DataIdx].RotationValue = float.Parse(s));

        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Filter.FilterType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].DistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].RotationDistributionType = i + 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].Axis = i);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].Transition = i);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].EaseType = i - 1);
        DropdownLoadFn.Add((x, i) => x.EventBoxes[0].EventDatas[DataIdx].RotationDirection = i);

        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].Filter.Reverse = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].RotationAffectFirst = b ? 1 : 0);
        ToggleLoadFn.Add((x, b) => x.EventBoxes[0].ReverseRotation = b ? 1 : 0);
    }
}
